namespace Pricing.CLI.Market
{
    public sealed class BasketSnapshot
    {
        /// Snapshot d'un panier multi-actifs avec métriques agrégées.
        /// Utile pour pricer des options sur un ETF fictif.
        /// Contient : symboles, poids normalisés, spot du panier, dividend yield, vol historique.
        public string[] Symbols { get; init; } = Array.Empty<string>();

        /// Poids normalisés : somme = 1 (pas de valeurs négatives=pas de short autorisé au sein de l'ETF).
        public double[] Weights { get; init; } = Array.Empty<double>(); 
        public double Spot { get; init; }            
        public double DividendYield { get; init; }   
        public double HistVol { get; init; }          
        public double ExpectedReturn { get; init; }  
    }

    public static class BasketBuilder
    {
        /// Permet de construire un panier pondéré à partir des symboles et poids fournis.
        /// - Spot du panier : S0 = Σ w_i × S0_i =moyenne pondérée des prix
        /// - Dividend yield effectif : q = Σ w_i × q_i
        /// - Volatilité historique : std(Σ w_i × r_{i,t}) × √252
        /// - Rendement attendu : moyenne des log-returns quotidiens × 252
        /// Remarque : Max 4 symboles (limite métier pour éviter trop de requêtes API), pas de short autorisé
        /// les séries sont alignées sur les dates communes disponibles
        public static async Task<BasketSnapshot> BuildWeightedAsync(string[] symbols, double[] weights)
        {
            if (symbols is null || symbols.Length == 0) throw new ArgumentException("Aucun symbole fourni.");
            if (weights is null || weights.Length != symbols.Length) throw new ArgumentException("Taille des poids invalide.");
            if (symbols.Length > 4) throw new ArgumentException("Max 4 symboles autorisés.");

            // Normalisation des poids : On met les négatifs à zéro puis on normalise pour que somme fasse 1
            var positive = weights.Select(x => Math.Max(0.0, x)).ToArray();
            double sumPositive = positive.Sum();
            if (sumPositive <= 0) throw new ArgumentException("Somme des poids positifs doit être > 0.");
            var w = positive.Select(x => x / sumPositive).ToArray();

            // Récupération des snapshots individuels + séries temporelles
            var snaps = new List<MarketSnapshot>(symbols.Length);
            var series = new List<(DateTime[] Dates, double[] Closes)>(symbols.Length);

            for (int i = 0; i < symbols.Length; i++)
            {
                var snap = await MarketDataProvider.GetSnapshotAsync(symbols[i]);
                snaps.Add(snap);

                var ts = await MarketDataProvider.GetDailyClosesAsync(symbols[i], maxDays: 400);
                series.Add(ts);
            }

            // Intersection des dates communes a tous les actifs certains marches peuvent avoir des jours fériés différents)
            var commonDates = series
                .Select(s => s.Dates)
                .Aggregate((acc, next) => acc.Intersect(next).ToArray())
                .OrderBy(d => d)
                .ToArray();

            if (commonDates.Length < 2)
                throw new InvalidOperationException("Pas assez de dates communes pour calculer la volatilité du panier.");

            // aligner
            var aligned = new List<double[]>();
            for (int k = 0; k < symbols.Length; k++)
            {
                var dict = new Dictionary<DateTime, double>(series[k].Dates.Length);
                for (int i = 0; i < series[k].Dates.Length; i++)
                    dict[series[k].Dates[i]] = series[k].Closes[i];
                aligned.Add(commonDates.Select(d => dict[d]).ToArray());
            }

            // Validation des prix (s assurer que toutes les clotures alignees sont strictement positives)
            for (int k = 0; k < symbols.Length; k++)
            {
                for (int i = 0; i < aligned[k].Length; i++)
                {
                    if (!(aligned[k][i] > 0.0))
                        throw new InvalidOperationException($"Prix non valide pour {symbols[k]} à {commonDates[i]:yyyy-MM-dd}: {aligned[k][i]}");
                }
            }

            // return du panier
            int T = commonDates.Length;
            var basketReturns = new double[T - 1];
            for (int t = 1; t < T; t++)
            {
                double ret = 0.0;
                for (int k = 0; k < symbols.Length; k++)
                {
                    double rk = Math.Log(aligned[k][t] / aligned[k][t - 1]);
                    ret += w[k] * rk;
                }
                basketReturns[t - 1] = ret;
            }

            double mean = basketReturns.Average();
            double var = basketReturns.Select(r => (r - mean) * (r - mean)).Average();
            double sigma = Math.Sqrt(Math.Max(var, 0.0)) * Math.Sqrt(252.0);
            double mu = mean * 252.0;

            double spot = 0.0;
            double q = 0.0;
            for (int k = 0; k < symbols.Length; k++)
            {
                spot += w[k] * snaps[k].Spot;
                q    += w[k] * snaps[k].DividendYield;
            }

            return new BasketSnapshot
            {
                Symbols = symbols,
                Weights = w,
                Spot = spot,
                DividendYield = Math.Max(q, 0.0),
                HistVol = Math.Max(sigma, 0.0),
                ExpectedReturn = mu
            };
        }
    }
}
