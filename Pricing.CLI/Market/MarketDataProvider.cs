using System.Globalization;
using System.Text.Json;

namespace Pricing.CLI.Market
{
    public sealed class MarketSnapshot
    {
        /// Ticker de l actif
        public string Symbol { get; init; } = "";

        /// Prix spot 
        public double Spot { get; init; }            

        /// Rendement de dividende estimé = Impossible de l'extraire de l API donc valeurs rentrées à la main
        public double DividendYield { get; init; }    

        /// Volatilité historique annualisée (σ) calculée sur 1 an 
        public double HistVol { get; init; }          
    }

    /// Fournit les données de marché via l'API Twelve Data
    /// Méthdo des calculs :
    /// 1. période historique: 365 jours ( avec 252 business days environ)
    ///    - Suffisant pour capturer les cycles saisonniers et les tendances
    ///    - Standard utilisé en finance quantitative
    /// 
    /// 2. Spot S0:
    ///    - Dernier cours de clôture disponible depuis l'API
    /// 
    /// 3. vol historique σ :
    ///    - Calcul : std(log_returns) × √252
    ///    - log_returns = ln(P_t / P_{t-1}) pour chaque jour
    ///    - Annualisée avec √252 
    ///    - bornée entre 5% et 200% pour éviter les valeurs aberrantes

    /// 
    /// 4. dividende q :
    ///    - Estimations sectorielles basée sur 2024-2025
    /// 
    /// 5. vol multi-actif (panier) :
    ///    - Calcul : std(Σ w_i × r_{i,t}) × √252
    ///    - Prend en compte les correl entre actifs (effet diversification)
    /// 
    /// API : Twelve Data (https://twelvedata.com/)
    public static class MarketDataProvider
    {
        private static readonly HttpClient _http = new HttpClient();
        
        // Rendement dividende estimé par secteur (valeurs 2024-2025)
        private static readonly Dictionary<string, double> DividendMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["AAPL"] = 0.005,  // Tech 
                ["XOM"]  = 0.035,  // Énergie 
                ["JPM"]  = 0.028,  // Financières 
                ["JNJ"]  = 0.029,  // Santé 
                ["AMZN"] = 0.000,  // Conso discrétionnaire 
                ["PG"]   = 0.024,  // Conso de base 
                ["CAT"]  = 0.020,  // Industrielles 
                ["LIN"]  = 0.015,  // Matériaux 
                ["GOOG"] = 0.000,  // Services communication 
                ["NEE"]  = 0.028,  // Services publics 
                ["SPG"]  = 0.065   // Immobilier 
            };

        private const int HISTORICAL_DAYS = 365; 

        /// Renvoie S0 et σ (vol 1 an) calculés depuis l'API Twelve Data.
        public static async Task<MarketSnapshot> GetSnapshotAsync(string symbol)
        {
            // Validation : le symbole doit être fourni
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol non fourni.", nameof(symbol));

            string? apiKey = Environment.GetEnvironmentVariable("TWELVEDATA_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Variable d'environnement TWELVEDATA_API_KEY non définie.\n" +
                    "Configurez-la avec : $env:TWELVEDATA_API_KEY = \"VOTRE_CLE\"\n" +
                    "Obtenez une clé gratuite sur : https://twelvedata.com/ (800 req/jour)"
                );
            }

            try
            {
                // Requête Twelve Data : on demande l'historique journalier sur 1 an
                string url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval=1day&outputsize=365&apikey={apiKey}";
                
                var response = await _http.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Erreur HTTP {response.StatusCode} pour {symbol}");
                }

                string json = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // L'API peut retourner un code d'erreur 
                if (root.TryGetProperty("code", out var code))
                {
                    string message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "Erreur inconnue" : "Erreur inconnue";
                    throw new Exception($"Erreur Twelve Data ({code.GetInt32()}) : {message}");
                }
                
                if (!root.TryGetProperty("values", out var values))
                {
                    throw new Exception($"Réponse invalide pour {symbol} : pas de 'values' dans la réponse JSON");
                }

                var closes = new List<double>();
                
                // Parse les prix de clôture journaliers
                foreach (var val in values.EnumerateArray())
                {
                    if (val.TryGetProperty("close", out var closeStr))
                    {
                        if (double.TryParse(closeStr.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double close))
                        {
                            closes.Add(close);
                        }
                    }
                }
                
                // Minimum 50 jours pour un calcul de vol fiable
                if (closes.Count < 50)
                {
                    throw new Exception($"Données insuffisantes pour {symbol} : seulement {closes.Count} jours (minimum 50 requis)");
                }

                // S0 = dernier cours de clôture
                // Remarque importante: Twelve Data retourne les données du plus récent au plus ancien (donc index 0 = aujourd'hui)
                double spot = closes[0];
                
                // Calcul de la vol historique annualisée
                var logReturns = new List<double>();
                for (int i = 0; i < closes.Count - 1; i++)
                {
                    double r = Math.Log(closes[i] / closes[i + 1]);
                    // On filtre les valeurs aberrantes (NaN, Inf)
                    if (!double.IsNaN(r) && !double.IsInfinity(r))
                    {
                        logReturns.Add(r);
                    }
                }
                
                if (logReturns.Count == 0)
                {
                    throw new Exception($"Impossible de calculer la volatilité pour {symbol} - log-returns tous invalides");
                }

                // Écart-type des rendements quotidiens
                double meanReturn = logReturns.Average();
                double variance = logReturns.Select(r => Math.Pow(r - meanReturn, 2)).Average();
                double dailyVol = Math.Sqrt(variance);
                
                // Annualisation
                double sigma = dailyVol * Math.Sqrt(252.0);
                
                // Dividende estimé selon le secteur (def + haut)
                // Si le ticker n'est pas dans la map, on met 0% 
                DividendMap.TryGetValue(symbol, out double dividendYield);

                return new MarketSnapshot
                {
                    Symbol = symbol,
                    Spot = spot,
                    DividendYield = Math.Max(0.0, Math.Min(0.15, dividendYield)),  //  0%, 15%
                    HistVol = Math.Max(0.05, Math.Min(2.00, sigma))  //  5%, 200% 
                };
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Erreur de connexion pour {symbol}. Vérifiez votre Internet. Détails : {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Erreur de parsing JSON pour {symbol}. L'API a peut-être retourné un format inattendu. Détails : {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Erreur lors du téléchargement des données pour {symbol} depuis Twelve Data : {ex.Message}", ex);
            }
        }

        /// Renvoie l'historique des clôtures quotidiennes sur 1 an pour construire un panier.
        /// Remarque: Même période que GetSnapshotAsync pour cohérence.
        public static async Task<(DateTime[] Dates, double[] Closes)> GetDailyClosesAsync(string symbol, int maxDays = 365)
        {
            if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol non fourni.", nameof(symbol));
            if (maxDays <= 0) throw new ArgumentException("maxDays doit être > 0", nameof(maxDays));

            string? apiKey = Environment.GetEnvironmentVariable("TWELVEDATA_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Variable d'environnement TWELVEDATA_API_KEY non définie.\n" +
                    "   Configurez-la avec : $env:TWELVEDATA_API_KEY = \"VOTRE_CLE\"\n" +
                    "   Obtenez une clé gratuite sur : https://twelvedata.com/"
                );
            }

            try
            {
                string url = $"https://api.twelvedata.com/time_series?symbol={symbol}&interval=1day&outputsize={maxDays}&apikey={apiKey}";
                
                var response = await _http.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Erreur HTTP {response.StatusCode} pour {symbol}");
                }

                string json = await response.Content.ReadAsStringAsync();
                
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                
                // Vérifier les erreurs
                if (root.TryGetProperty("code", out var code))
                {
                    string message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? "Erreur inconnue" : "Erreur inconnue";
                    throw new Exception($"Erreur Twelve Data ({code.GetInt32()}) : {message}");
                }
                
                if (!root.TryGetProperty("values", out var values))
                {
                    throw new Exception($"Réponse invalide pour {symbol} : pas de 'values'");
                }

                var records = new List<(DateTime Date, double Close)>();
                
                foreach (var val in values.EnumerateArray())
                {
                    if (val.TryGetProperty("datetime", out var dtStr) &&
                        val.TryGetProperty("close", out var closeStr))
                    {
                        // Convertir la datetime retournée par l'API en Date (ignore l'heure)
                        if (DateTime.TryParse(dtStr.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) &&
                            double.TryParse(closeStr.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double close))
                        {
                            records.Add((date.Date, close));
                        }
                    }
                }
                
                if (records.Count < 50)
                {
                    throw new Exception($"Données insuffisantes pour {symbol} : {records.Count} jours (minimum 50 requis)");
                }

                // Rq: Comme Twelve Data retourne les données du plus récent au plus ancien, on les ordonnons par date croissante
                records = records.OrderBy(r => r.Date).ToList();

                var dates = records.Select(r => r.Date).ToArray();
                var closes = records.Select(r => r.Close).ToArray();

                return (dates, closes);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(
                    $"Erreur de connexion pour {symbol}. Vérifiez votre Internet. Détails : {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Erreur de parsing JSON pour {symbol}. L'API a peut-être retourné un format inattendu. Détails : {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Erreur lors du téléchargement de l'historique pour {symbol} depuis Twelve Data : {ex.Message}", ex);
            }
        }
    }
}
