using System.Windows;
using System.IO;
using Pricing.Core.Models;
using Pricing.Core.Pricing;
using Pricing.Core.Analysis;
using Pricing.Core.Strategies;
using Pricing.CLI.Market;
using Pricing.Core.Ui;

namespace Pricing.WPF;

/// Contrôleur du workflow WPF : gère toute la logique de l'interface pas-à-pas.
/// Mode Avisé (expert) vs Non-Avisé (guidé par questionnaire).
/// C'est le cerveau de l'application - coordonne les étapes, valide les inputs, appelle l'API.
public class WorkflowController
{
    // Référence à la fenêtre principale pour mettre à jour l'UI
    private readonly MainWindow _window;
    
    // Mode choisi par l'utilisateur : true = Avisé (expert), false = Non-Avisé (débutant)
    private readonly bool _isAdvanced;
    
    // État actuel du workflow (Capital, Secteurs, Horizon, etc.)
    private WorkflowState _state;
    
    // Données accumulées au fil du workflow (capital, secteurs, options, etc.)
    private WorkflowData _data;
    
    // Buffer temporaire pour les poids en attente de confirmation (fractions normalisées qui somment à 1)
    private double[]? _pendingWeights;

    /// Constructeur : reçoit la fenêtre principale et le mode choisi.
    public WorkflowController(MainWindow window, bool isAdvanced)
    {
        _window = window;
        _isAdvanced = isAdvanced;
        _data = new WorkflowData();
        _state = WorkflowState.Capital; // Toujours commencer par demander le capital
    }

    /// Démarre le workflow : affiche le mode choisi et pose la première question (capital).
    public void Start()
    {
        _window.UpdateStatus($"Mode : {(_isAdvanced ? "AVISÉ (Expert)" : "NON-AVISÉ (Guidé)")}");
        _window.AddHistory($"Mode sélectionné : {(_isAdvanced ? "AVISÉ" : "NON-AVISÉ")}");
        
        AskCapital();
    }

    /// Point d'entrée pour traiter toutes les réponses de l'utilisateur.
    /// Appelle le handler approprié selon l'état actuel du workflow.
    public void ProcessInput(string input)
    {
        // Input vide → on redemande
        if (string.IsNullOrWhiteSpace(input))
        {
            _window.UpdateStatus("Veuillez entrer une valeur.");
            return;
        }

        try
        {
            // Machine à états : chaque étape du workflow a son handler
            switch (_state)
            {
                case WorkflowState.Capital:
                    HandleCapital(input);
                    break;
                case WorkflowState.Sectors:
                    HandleSectors(input);
                    break;
                    case WorkflowState.Weights:
                        HandleWeights(input);
                        break;
                    case WorkflowState.WeightsConfirm:
                        HandleWeightsConfirmation(input);
                        break;
                case WorkflowState.Horizon:
                    HandleHorizon(input);
                    break;
                case WorkflowState.Objectives:
                    HandleObjectives(input);
                    break;
                case WorkflowState.AdvancedChoice:
                    HandleAdvancedChoice(input);
                    break;
                case WorkflowState.SingleOptionParams:
                    HandleSingleOptionParams(input);
                    break;
                case WorkflowState.PortfolioSize:
                    HandlePortfolioSize(input);
                    break;
                case WorkflowState.OptionParams:
                    HandleOptionParams(input);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Affiche les erreurs de manière visible dans l'UI si disponible
            if (_window != null)
            {
                _window.ShowError($" Erreur : {ex.Message}");
            }
            else
            {
                // Solution de repli si pas de fenêtre
                Console.Error.WriteLine(ex.Message);
            }
        }
    }

    private void AskCapital()
    {
        _state = WorkflowState.Capital;
        _window.UpdateQuestion(" Quel est votre capital disponible (en €) ?");
        _window.AddHistory(" Étape : Capital");
        // S'assure que l'aide courte est masquée à l'étape du capital
        try { _window.HideShortHelp(); } catch { }
    }

    private void HandleCapital(string input)
    {
        if (!double.TryParse(input, out double capital) || capital <= 0)
        {
            _window.UpdateStatus("Montant invalide. Entrez un nombre positif.");
            return;
        }

        const double MinimumCapital = 50000.0;
        if (capital < MinimumCapital)
        {
            // Empêche de continuer si le capital est en-dessous du minimum requis.
            // Affiche une erreur et reste sur l'étape capital pour forcer une nouvelle saisie.
            _window.ShowError($"Le capital doit être au moins {MinimumCapital:N0} €. Veuillez saisir un montant supérieur.");
            AskCapital();
            return;
        }

        _data.Capital = capital;
        _data.CapitalInitial = capital;
        _data.CapitalUtilise = 0.0;
        _window.AddHistory($"Capital initial : {capital:F2} €");

        AskSectors();
    }

    private void AskSectors()
    {
        _state = WorkflowState.Sectors;
        _window.UpdateQuestion(
            "Sélectionnez 1 à 4 secteurs (séparés par virgules) :\n\n" +
            "1 = Technologie (AAPL)\n" +
            "2 = Énergie (XOM)\n" +
            "3 = Finance (JPM)\n" +
            "4 = Santé (JNJ)\n" +
            "5 = Consommation (AMZN)\n" +
            "6 = Biens de consommation (PG)\n" +
            "7 = Industriel (CAT)\n" +
            "8 = Matériaux (LIN)\n" +
            "9 = Communication (GOOG)\n" +
            "10 = Utilities (NEE)\n" +
            "11 = Immobilier (SPG)\n\n" +
            "Exemple : 1,2,3");
        _window.AddHistory("Étape : Secteurs");
    }

    private void HandleSectors(string input)
    {
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var indices = new List<int>();

        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int idx) && idx >= 1 && idx <= 11)
            {
                indices.Add(idx);
            }
        }

        if (indices.Count == 0 || indices.Count > 4)
        {
            _window.UpdateStatus("Choisissez entre 1 et 4 secteurs valides.");
            return;
        }

        _data.SectorIndices = indices.Distinct().Take(4).ToList();
        
        var sectorNames = new[] { "Tech", "Énergie", "Finance", "Santé", "Conso", 
                                  "Biens conso", "Industriel", "Matériaux", "Com", "Utilities", "Immo" };
        var selectedNames = _data.SectorIndices.Select(i => sectorNames[i - 1]);
        _window.AddHistory($"Secteurs : {string.Join(", ", selectedNames)}");

        // Après la sélection des secteurs, permet à l'utilisateur de spécifier optionnellement des poids par symbole
        AskWeights();
    }

    private void AskWeights()
    {
        _state = WorkflowState.Weights;
        var symbols = GetSymbolsFromSectors(_data.SectorIndices);
        // Demande : permet d'entrer des pourcentages séparés par virgules pour chaque symbole ou AUTO pour poids égaux
        string prompt = "Optionnel) Définissez des pondérations pour chaque sous-jacent (en %) séparées par des virgules) :\n\n" +
                        $"Sous-jacents : {string.Join(", ", symbols)}\n\n" +
                        "Format : 50,30,20  ou  50%,30%,20%  \n" +
                        "Tapez AUTO pour utiliser des poids égaux.";
        _window.UpdateQuestion(prompt);
        _window.AddHistory("Étape : Pondérations par sous-jacent (optionnel)");
    }

    private void HandleWeights(string input)
    {
        var symbols = GetSymbolsFromSectors(_data.SectorIndices);

        if (string.IsNullOrWhiteSpace(input) || input.Trim().ToUpper() == "AUTO" || input.Trim().ToUpper() == "EGAUX")
        {
            // Utilise des poids égaux (laisse _data.Weights à null → valeur par défaut plus tard)
            _data.Weights = null;
            _window.AddHistory(" Pondérations : poids égaux (par défaut)");
            AskHorizon();
            return;
        }

        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
        if (parts.Length != symbols.Length)
        {
            _window.UpdateStatus($" Nombre de valeurs attendu : {symbols.Length}. Entrez {symbols.Length} valeurs séparées par des virgules ou AUTO.");
            return;
        }

        var vals = new double[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            var s = parts[i];
            if (s.EndsWith("%")) s = s.TrimEnd('%');
            if (!double.TryParse(s, out double v) || v < 0)
            {
                _window.UpdateStatus($"Valeur invalide pour {symbols[i]} : '{parts[i]}'");
                return;
            }
            vals[i] = v; // garde comme pourcentages ou poids; BasketBuilder normalisera les valeurs positives
        }

        double sum = vals.Sum();
        if (sum <= 0)
        {
            _window.UpdateStatus("Somme des pondérations doit être > 0. Utilisation de poids égaux.");
            _data.Weights = null;
            AskHorizon();
            return;
        }

        // Normalise en fractions qui somment à 1 et demande confirmation à l'utilisateur de la composition normalisée
        var normalized = vals.Select(v => v / sum).ToArray();
        _pendingWeights = normalized;
        AskWeightsConfirmation(symbols, normalized);
    }

    private void AskWeightsConfirmation(string[] symbols, double[] normalized)
    {
        _state = WorkflowState.WeightsConfirm;
        var lines = symbols.Select((s, i) => $"{s}: {normalized[i] * 100.0:F1}%");
        string prompt = " Pondérations normalisées :\n\n" + string.Join("\n", lines) +
                        "\n\nConfirmez-vous ces pondérations ? (O/N)";
        _window.UpdateQuestion(prompt);
        _window.AddHistory("Étape : Confirmation des pondérations");
    }

    private void HandleWeightsConfirmation(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            _window.UpdateStatus("Répondez O (oui) ou N (non).");
            return;
        }

        var t = input.Trim().ToUpper();
        if (t == "O" || t == "OUI" || t == "Y" || t == "YES" || t == "1")
        {
            if (_pendingWeights == null)
            {
                _window.UpdateStatus("Aucune pondération en attente.");
                AskWeights();
                return;
            }

            _data.Weights = _pendingWeights;
            _window.AddHistory($"Pondérations confirmées : {string.Join(", ", _data.Weights.Select(w => (w * 100.0).ToString("F1") + "%"))}");
            _pendingWeights = null;
            AskHorizon();
            return;
        }

        if (t == "N" || t == "NON" || t == "0")
        {
            _pendingWeights = null;
            _window.AddHistory(" Saisie des pondérations annulée. Veuillez ressaisir.");
            AskWeights();
            return;
        }

        _window.UpdateStatus("Répondez O (oui) ou N (non).");
    }

    private void AskHorizon()
    {
        _state = WorkflowState.Horizon;
        _window.UpdateQuestion(
            "Quel est votre horizon d'investissement ?\n\n" +
            "1 = 6 mois\n" +
            "2 = 1 an\n" +
            "3 = 2 ans\n" +
            "4 = 5 ans");
        _window.AddHistory(" Étape : Horizon");
    }

    private void HandleHorizon(string input)
    {
        if (!int.TryParse(input, out int choice) || choice < 1 || choice > 4)
        {
            _window.UpdateStatus("Choix invalide. Entrez 1, 2, 3 ou 4.");
            return;
        }

        _data.HorizonYears = choice switch
        {
            1 => 0.5,
            2 => 1.0,
            3 => 2.0,
            4 => 5.0,
            _ => 1.0
        };

        _window.AddHistory($" Horizon : {_data.HorizonYears} an(s)");

        if (_isAdvanced)
        {
            AskAdvancedChoice();
        }
        else
        {
            AskObjectives();
        }
    }

    private void AskObjectives()
    {
        _state = WorkflowState.Objectives;
        _window.UpdateQuestion(ObjectiveDescriptions.GetWpfObjectivesPrompt());
        _window.AddHistory(" Étape : Objectifs");
        // Prépare les explications détaillées mais garde le panneau réduit par défaut;
        // l'utilisateur peut cliquer sur "En savoir plus" pour l'étendre. Affiche aussi l'aide courte d'une ligne.
        _window.SetObjectiveHelpText(ObjectiveDescriptions.GetDetailedHelp());
        try { _window.ShowShortHelp("Aide : tapez le(s) numéro(s) de l'objectif (ex : 1,4). "); } catch { }
    }

    private async void HandleObjectives(string input)
    {
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var objectives = new List<int>();

        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int obj) && obj >= 1 && obj <= 7)
            {
                objectives.Add(obj);
            }
        }

        if (objectives.Count == 0 || objectives.Count > 3)
        {
            _window.UpdateStatus("Choisissez entre 1 et 3 objectifs valides.");
            return;
        }

        // Valide les contradictions entre les objectifs sélectionnés
        var distinct = objectives.Distinct().Take(3).ToList();
        var conflictMessage = ValidateObjectiveCompatibility(distinct);
        if (!string.IsNullOrEmpty(conflictMessage))
        {
            // Affiche une erreur et demande à l'utilisateur de resaisir ses objectifs (ne continue pas)
            _window.ShowError(conflictMessage);
            return;
        }

        _data.Objectives = distinct;
        
        var objNames = new[] { "Hausse", "Baisse", "Stabilité", "Forte volatilité", 
                               "Volatilité modérée", "Long terme", "Range" };
    var selectedObj = _data.Objectives.Select(o => objNames[o - 1]);
    _window.AddHistory($"Objectifs : {string.Join(", ", selectedObj)}");

        await BuildNonAdvancedPortfolio();
    }

    private void AskAdvancedChoice()
    {
        _state = WorkflowState.AdvancedChoice;
        _window.UpdateQuestion(
            "Que souhaitez-vous analyser ?\n\n" +
            "1 = Une seule option (avec toutes les grecques)\n" +
            "2 = Un portefeuille d'options (jusqu'à 4 options)");
        _window.AddHistory("Étape : Choix d'analyse");
    }

    /// Valide que les objectifs sélectionnés sont mutuellement compatibles.
    /// Retourne null ou chaîne vide si OK, sinon retourne un message convivial décrivant les conflits.
    private string ValidateObjectiveCompatibility(List<int> objectives)
    {
        // Catégories :
        // 1 = Bull (hausse), 2 = Bear (baisse), 3 = Stabilité, 4 = Forte volatilité, 5 = Volatilité modérée (strangle),
        // 6 = Calendar, 7 = Range
        var conflicts = new List<string>();

        bool hasBull = objectives.Contains(1);
        bool hasBear = objectives.Contains(2);
        bool hasStable = objectives.Contains(3);
        bool hasStrongVol = objectives.Contains(4);
        bool hasModVol = objectives.Contains(5);
        bool hasRange = objectives.Contains(7);

        if (hasBull && hasBear)
            conflicts.Add("Hausse (1) et Baisse (2) — objectif contradictoire : hausse vs baisse");

        if (hasStable && (hasStrongVol || hasModVol))
            conflicts.Add("Stabilité (3) vs Volatilité (4/5) — ne choisissez pas 'stable' et 'volatilité' ensemble");

        if (hasRange && (hasStrongVol || hasModVol))
            conflicts.Add("Range (7) vs Volatilité (4/5) — range suppose peu de mouvement, incompatible avec pari volatilité");

        if (conflicts.Any())
        {
            var message = "Stratégies contradictoires sélectionnées :\n" + string.Join("; ", conflicts) + "\nVeuillez retaper des objectifs compatibles.";
            return message;
        }

        return string.Empty;
    }

    private void HandleAdvancedChoice(string input)
    {
        if (!int.TryParse(input, out int choice) || (choice != 1 && choice != 2))
        {
            _window.UpdateStatus("Choix invalide. Entrez 1 ou 2.");
            return;
        }

        _data.AdvancedChoice = choice;
        _window.AddHistory($"Analyse : {(choice == 1 ? "Option unique" : "Portefeuille")}");

        if (choice == 1)
        {
            AskSingleOption();
        }
        else
        {
            AskPortfolioSize();
        }
    }

    private async void AskSingleOption()
    {
        _state = WorkflowState.SingleOptionParams;
        _window.UpdateQuestion(
            "Paramètres de l'option (format : Type,Strike,Position)\n\n" +
            "Type : C (Call) ou P (Put)\n" +
            "Strike : Prix d'exercice\n" +
            "Position : L (Long/Achat) ou S (Short/Vente)\n\n" +
            "Exemple : C,100,L");
        _window.AddHistory(" Étape : Paramètres option");

        // Pour les utilisateurs avancés, récupère un instantané du marché et affiche un petit aperçu pour aider à choisir K
        try
        {
            var symbols = GetSymbolsFromSectors(_data.SectorIndices);
            double[] weights = (_data.Weights != null && _data.Weights.Length == symbols.Length)
                ? _data.Weights
                : Enumerable.Repeat(1.0 / symbols.Length, symbols.Length).ToArray();

            var basket = await BasketBuilder.BuildWeightedAsync(symbols, weights);
            // Sauvegarde l'instantané du marché récupéré pour que les étapes suivantes (calculs de capital/marge)
            // puissent accéder aux paramètres de marché sans refaire l'appel. Affiche aussi un petit aperçu.
            _data.MarketData = basket;
            var preview = $"Marché : S0 = {basket.Spot:F2} € | σ = {basket.HistVol * 100:F2}% | q = {basket.DividendYield * 100:F2}% | Horizon = {_data.HorizonYears} an(s)";
            _window.ShowMarketPreview(preview);
        }
        catch { /* ignore preview on failure */ }
    }

    private async void HandleSingleOptionParams(string input)
    {
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim().ToUpper())
                         .ToArray();

        if (parts.Length != 3)
        {
            _window.UpdateStatus("Format invalide. Utilisez : Type,Strike,Position");
            return;
        }

        if (parts[0] != "C" && parts[0] != "P")
        {
            _window.UpdateStatus(" Type doit être C ou P.");
            return;
        }

        if (!double.TryParse(parts[1], out double strike) || strike <= 0)
        {
            _window.UpdateStatus("Strike invalide.");
            return;
        }

        if (parts[2] != "L" && parts[2] != "S")
        {
            _window.UpdateStatus("Position doit être L ou S.");
            return;
        }

        _data.SingleOption = (parts[0], strike, parts[2]);
        _window.AddHistory($" Option : {parts[0]}, K={strike}, {(parts[2] == "L" ? "Long" : "Short")}");

        await BuildSingleOption();
    }

    private void AskPortfolioSize()
    {
        _state = WorkflowState.PortfolioSize;
        _window.UpdateQuestion(" Combien d'options voulez-vous dans le portefeuille ? (1 à 4)");
        _window.AddHistory(" Étape : Taille portefeuille");
    }

    private void HandlePortfolioSize(string input)
    {
        if (!int.TryParse(input, out int size) || size < 1 || size > 4)
        {
            _window.UpdateStatus(" Nombre invalide. Entre 1 et 4.");
            return;
        }

        _data.PortfolioSize = size;
        _data.CurrentOptionIndex = 0;
        _data.PortfolioOptions = new List<(string type, double strike, string position, double qty)>();
        
        _window.AddHistory($" Nombre d'options : {size}");

        AskOptionParams();
    }

    private async void AskOptionParams()
    {
        _state = WorkflowState.OptionParams;
        int current = _data.CurrentOptionIndex + 1;
        _window.UpdateQuestion(
            $" Option {current}/{_data.PortfolioSize} (format : Type,Strike,Position,Quantité)\n\n" +
            "Type : C (Call) ou P (Put)\n" +
            "Strike : Prix d'exercice\n" +
            "Position : L (Long/Achat) ou S (Short/Vente)\n" +
            "Quantité : nombre de contrats  OU Allocation : pourcentage du capital initial (ex: 10%)\n\n" +
            "Exemple : C,100,L,2   ou   C,100,L,10% (allouer 10% du capital initial)");
        _window.AddHistory($" Étape : Option {current}/{_data.PortfolioSize}");

        try
        {
            var symbols = GetSymbolsFromSectors(_data.SectorIndices);
            double[] weights = (_data.Weights != null && _data.Weights.Length == symbols.Length)
                ? _data.Weights
                : Enumerable.Repeat(1.0 / symbols.Length, symbols.Length).ToArray();

            var basket = await BasketBuilder.BuildWeightedAsync(symbols, weights);
            _data.MarketData = basket;
            var preview = $"Marché : S0 = {basket.Spot:F2} € | σ = {basket.HistVol * 100:F2}% | q = {basket.DividendYield * 100:F2}% | Horizon = {_data.HorizonYears} an(s)";
            _window.ShowMarketPreview(preview);
        }
        catch { }
    }

    private async void HandleOptionParams(string input)
    {
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries)
                         .Select(p => p.Trim().ToUpper())
                         .ToArray();

        if (parts.Length != 4)
        {
            _window.UpdateStatus(" Format invalide. Utilisez : Type,Strike,Position,Quantité");
            return;
        }

        if (parts[0] != "C" && parts[0] != "P")
        {
            _window.UpdateStatus(" Type doit être C ou P.");
            return;
        }

        if (!double.TryParse(parts[1], out double strike) || strike <= 0)
        {
            _window.UpdateStatus(" Strike invalide.");
            return;
        }

        if (parts[2] != "L" && parts[2] != "S")
        {
            _window.UpdateStatus(" Position doit être L ou S.");
            return;
        }

        // Supporte soit une quantité explicite (ex : 2) soit un pourcentage d'allocation (ex : 10%)
        double qty;
        var qtyRaw = parts[3];
        bool usedAllocationPercent = false;
        if (qtyRaw.EndsWith("%"))
        {
            // analyse le pourcentage
            var pctStr = qtyRaw.TrimEnd('%');
            if (!double.TryParse(pctStr, out double pct) || pct <= 0 || pct > 100)
            {
                _window.UpdateStatus("Allocation en pourcentage invalide (0-100%).");
                return;
            }
            usedAllocationPercent = true;
            // qty sera calculée après le pricing ci-dessous
            qty = -1; // marque-place
        }
        else
        {
            if (!double.TryParse(qtyRaw, out qty) || qty <= 0)
            {
                _window.UpdateStatus("Quantité invalide.");
                return;
            }
        }

        bool isCall = parts[0] == "C";
        bool isLong = parts[2] == "L";

        // DISCLAIMER POUR POSITIONS SHORT
        if (!isLong && _data.MarketData != null)
        {
            string riskType = isCall ? "ILLIMITÉ (pertes théoriquement infinies)" : "TRÈS ÉLEVÉ (pertes jusqu'à K × quantité)";
            var result = MessageBox.Show(
                $" AVERTISSEMENT : POSITION SHORT À RISQUE {(isCall ? "ILLIMITÉ" : "TRÈS ÉLEVÉ")}\n\n" +
                $"Vous allez vendre un {(isCall ? "CALL" : "PUT")} SHORT.\n\n" +
                $" RISQUE : {riskType}\n\n" +
                $" MARGE REQUISE : ~100% du notionnel\n" +
                $"   (environ {_data.MarketData.Spot * qty * 100:F2} € à immobiliser)\n\n" +
                $" AVANTAGE : Vous recevez la prime immédiatement\n\n" +
                " Cette position est réservée aux traders expérimentés.\n\n" +
                "Confirmez-vous que vous comprenez les risques ?",
                "Confirmation Position Short",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.No)
            {
                _window.AddHistory($"  Position Short {(isCall ? "Call" : "Put")} K={strike} refusée");
                _window.UpdateStatus("Position annulée. Réessayez avec une autre option.");
                return;
            }

            _window.AddHistory($"  Position Short {(isCall ? "Call" : "Put")} K={strike} confirmée (risques acceptés)");
        }

        // CALCUL DU CAPITAL REQUIS
        if (_data.MarketData != null)
        {
            // Calculer le prix de l'option
            double S0 = _data.MarketData.Spot;
            double sigma = _data.MarketData.HistVol;
            double q = _data.MarketData.DividendYield;
            double r = 0.03;
            double T = _data.HorizonYears;

            Option tempOpt = isCall 
                ? new CallOption(S0, strike, r, q, T, sigma)
                : new PutOption(S0, strike, r, q, T, sigma);

            var pricer = new BlackScholesPricer();
            double prixUnitaire = pricer.Price(tempOpt);

            double capitalRequis;
            string typeTransaction;

            if (usedAllocationPercent)
            {
                // calcule la quantité depuis le pourcentage d'allocation fourni par l'utilisateur
                var pctStr = parts[3].TrimEnd('%');
                double pct = double.Parse(pctStr);
                double allocationAmount = pct / 100.0 * _data.CapitalInitial;

                if (isLong)
                {
                    // nombre de contrats accessibles avec l'allocation
                    qty = Math.Floor(allocationAmount / (prixUnitaire * 100));
                    typeTransaction = "Paiement de la prime (via allocation %)";
                    capitalRequis = prixUnitaire * qty * 100;
                }
                else
                {
                    // pour le short, l'allocation se réfère à la marge que l'utilisateur est prêt à allouer
                    qty = Math.Floor(allocationAmount / (S0 * 100));
                    double primeRecue = prixUnitaire * qty * 100;
                    double margeRequise = S0 * qty * 100; // notionnel complet
                    capitalRequis = margeRequise;
                    typeTransaction = $"Marge requise (allocation %) ({primeRecue:F2} € de prime reçue)";
                }

                if (qty <= 0)
                {
                    _window.UpdateStatus("Allocation trop faible pour acheter au moins 1 contrat avec ces paramètres.");
                    return;
                }
            }
            else
            {
                if (isLong)
                {
                    // Long = payer la prime
                    capitalRequis = prixUnitaire * qty * 100; // 100 actions par contrat
                    typeTransaction = "Paiement de la prime";
                }
                else
                {
                    // Short = recevoir la prime mais déposer la marge
                    double primeRecue = prixUnitaire * qty * 100;
                    double margeRequise = S0 * qty * 100; // notionnel complet
                    capitalRequis = margeRequise;
                    typeTransaction = $"Marge requise ({primeRecue:F2} € de prime reçue)";
                }
            }

            // VALIDATION DU CAPITAL
            if (capitalRequis > _data.Capital)
            {
                int qtyMax = (int)Math.Floor(_data.Capital / (capitalRequis / qty));
                
                MessageBox.Show(
                    $" CAPITAL INSUFFISANT !\n\n" +
                    $"Capital disponible : {_data.Capital:F2} €\n" +
                    $"Capital requis : {capitalRequis:F2} €\n" +
                    $"Manque : {capitalRequis - _data.Capital:F2} €\n\n" +
                    $"Solutions :\n" +
                    $"  • Réduire la quantité à {qtyMax} contrat(s) maximum\n" +
                    $"  • Choisir un strike moins cher (plus OTM)\n" +
                    $"  • Annuler cette position",
                    "Capital Insuffisant",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                _window.AddHistory($"   Capital insuffisant : besoin de {capitalRequis:F2} €, disponible : {_data.Capital:F2} €");
                _window.UpdateStatus($" Capital insuffisant. Ajustez vos paramètres.");
                return;
            }

            // CALCUL DU BREAK-EVEN
            double breakeven = isCall ? strike + prixUnitaire : strike - prixUnitaire;

            // MISE À JOUR DU CAPITAL
            _data.Capital -= capitalRequis;
            _data.CapitalUtilise += capitalRequis;

            _window.AddHistory($"   Option {_data.CurrentOptionIndex + 1} : {(isCall ? "Call" : "Put")} K={strike}, {(isLong ? "Long" : "Short")}, Qty={qty}");
            _window.AddHistory($"     Prix unitaire (BS) : {prixUnitaire:F6} €");
            _window.AddHistory($"     {typeTransaction} : {capitalRequis:F2} €");
            _window.AddHistory($"     Break-even : {breakeven:F2} € (profit NET si S_T {(isCall ? ">" : "<")} {breakeven:F2})");
            _window.AddHistory($"     Capital restant : {_data.Capital:F2} €");
        }

        _data.PortfolioOptions!.Add((parts[0], strike, parts[2], qty));

        _data.CurrentOptionIndex++;

        if (_data.CurrentOptionIndex < _data.PortfolioSize)
        {
            AskOptionParams();
        }
        else
        {
            await BuildAdvancedPortfolio();
        }
    }

    private async Task BuildNonAdvancedPortfolio()
    {
        _window.UpdateStatus(" Téléchargement des données de marché en cours...");
        await Task.Delay(100); // Permet à l'UI de se mettre à jour

        try
        {
            // Build basket
            var symbols = GetSymbolsFromSectors(_data.SectorIndices);
            double[] weights;
            if (_data.Weights != null && _data.Weights.Length == symbols.Length)
                weights = _data.Weights;
            else
                weights = Enumerable.Repeat(1.0 / symbols.Length, symbols.Length).ToArray();

            _window.AddHistory($" Téléchargement : {symbols.Length * 2} requêtes API...");

            var basket = await BasketBuilder.BuildWeightedAsync(symbols, weights);
            
            _window.AddHistory($" Données récupérées : S0={basket.Spot:F2} €, σ={basket.HistVol * 100:F2}%, q={basket.DividendYield * 100:F2}%");

            // Create strategies
            var portfolio = new Core.Portfolio.Portfolio();
            double capitalPerObjective = _data.Capital / _data.Objectives.Count;

            foreach (var objIdx in _data.Objectives)
            {
                var strategy = CreateStrategyFromObjective(objIdx, basket.Spot, basket.HistVol, _data.HorizonYears, basket.DividendYield);
                
                if (strategy != null)
                {
                    foreach (var leg in strategy.GetLegs())
                    {
                        portfolio.AddOption(leg.Opt, leg.Qty);
                    }
                    _window.AddHistory($"  Stratégie ajoutée : {strategy.GetType().Name}");
                }
            }

            await ComputeAndDisplayResults(basket, portfolio);
        }
        catch (Exception ex)
        {
            _window.UpdateStatus($" Erreur : {ex.Message}");
            _window.AddHistory($" {ex.Message}");
        }
    }

    private async Task BuildSingleOption()
    {
        _window.UpdateStatus(" Téléchargement des données de marché...");
        await Task.Delay(100);

        try
        {
            var symbols = GetSymbolsFromSectors(_data.SectorIndices);
            double[] weights;
            if (_data.Weights != null && _data.Weights.Length == symbols.Length)
                weights = _data.Weights;
            else
                weights = Enumerable.Repeat(1.0 / symbols.Length, symbols.Length).ToArray();

            var basket = await BasketBuilder.BuildWeightedAsync(symbols, weights);
            _window.AddHistory($" S0={basket.Spot:F2} €, σ={basket.HistVol * 100:F2}%, q={basket.DividendYield * 100:F2}%");

            var (type, strike, position) = _data.SingleOption;
            bool isCall = type == "C";
            bool isLong = position == "L";
            int qty = isLong ? 1 : -1;

            Option option = isCall 
                ? new CallOption(basket.Spot, strike, 0.03, basket.DividendYield, _data.HorizonYears, basket.HistVol)
                : new PutOption(basket.Spot, strike, 0.03, basket.DividendYield, _data.HorizonYears, basket.HistVol);
            
            var portfolio = new Core.Portfolio.Portfolio();
            portfolio.AddOption(option, qty);

            await ComputeAndDisplayResults(basket, portfolio);
        }
        catch (Exception ex)
        {
            _window.UpdateStatus($" Erreur : {ex.Message}");
        }
    }

    private async Task BuildAdvancedPortfolio()
    {
        _window.UpdateStatus("⏳ Téléchargement des données de marché...");
        await Task.Delay(100);

        try
        {
            var symbols = GetSymbolsFromSectors(_data.SectorIndices);
            double[] weights;
            if (_data.Weights != null && _data.Weights.Length == symbols.Length)
                weights = _data.Weights;
            else
                weights = Enumerable.Repeat(1.0 / symbols.Length, symbols.Length).ToArray();

            var basket = await BasketBuilder.BuildWeightedAsync(symbols, weights);
            _window.AddHistory($" S0={basket.Spot:F2} €, σ={basket.HistVol * 100:F2}%, q={basket.DividendYield * 100:F2}%");

            var portfolio = new Core.Portfolio.Portfolio();

            foreach (var (type, strike, position, qtyValue) in _data.PortfolioOptions!)
            {
                bool isCall = type == "C";
                bool isLong = position == "L";
                int finalQty = (int)(isLong ? qtyValue : -qtyValue);

                Option option = isCall 
                    ? new CallOption(basket.Spot, strike, 0.03, basket.DividendYield, _data.HorizonYears, basket.HistVol)
                    : new PutOption(basket.Spot, strike, 0.03, basket.DividendYield, _data.HorizonYears, basket.HistVol);
                
                portfolio.AddOption(option, finalQty);
            }

            await ComputeAndDisplayResults(basket, portfolio);
        }
        catch (Exception ex)
        {
            _window.UpdateStatus($" Erreur : {ex.Message}");
        }
    }

    private async Task ComputeAndDisplayResults(BasketSnapshot basket, Core.Portfolio.Portfolio portfolio)
    {
        _data.MarketData = basket; // Sauvegarder pour validation capital
        // Garde le dernier portefeuille calculé disponible pour les requêtes Monte-Carlo depuis l'UI
        _data.LastPortfolio = portfolio;
        
        await Task.Run(() =>
        {
            // Prix de toutes les options
            var pricer = new BlackScholesPricer();
            double totalValue = 0;
            double totalDelta = 0;
            double totalGamma = 0;
            double totalVega = 0;
            double totalTheta = 0;
            double totalRho = 0;

            // Collecte les détails de pricing par option pour afficher dans l'UI
            var perOptionLines = new System.Text.StringBuilder();
            perOptionLines.AppendLine("Détails des options (prix Black‑Scholes par contrat) :");

            foreach (var (opt, qty) in portfolio.OptionLines)
            {
                var price = pricer.Price(opt);
                totalValue += price * qty;

                var delta = pricer.Delta(opt);
                var gamma = pricer.Gamma(opt);
                var vega = pricer.Vega(opt);
                var theta = pricer.Theta(opt);
                var rho = pricer.Rho(opt);
                
                totalDelta += delta * qty;
                totalGamma += gamma * qty;
                totalVega += vega * qty;
                totalTheta += theta * qty;
                totalRho += rho * qty;

                // Formate la description de l'option
                string optType = opt.IsCall ? "C" : "P";
                string pos = qty > 0 ? "Long" : "Short";
                double lineTotal = price * qty * 100.0; // impact cash (prix par contrat × qty × taille contrat)
                perOptionLines.AppendFormat("- {0}, K={1:F2}, {2}, qty={3} => Prix BS (par contrat) = {4:F6} €, Total (notionnel*qty) = {5:F2} €\n",
                                         optType, opt.K, pos, qty, price, lineTotal);
            }

            // Calcule le capital requis en utilisant la fonction de bibliothèque
            double capitalRequired = PortfolioAnalyzer.CalculateCapitalRequired(portfolio.OptionLines, pricer, 0.20);

            // Mettre à jour le capital utilisé / restant dans les données du workflow
            _data.CapitalUtilise = Math.Round(capitalRequired, 2);
            _data.Capital = Math.Max(0.0, _data.CapitalInitial - _data.CapitalUtilise);

            // Calcul du payoff en utilisant la fonction de bibliothèque
            var payoffData = new List<(double spot, double payoff)>();
            double[] spotGrid = PayoffEngine.GenerateGrid(basket.Spot, 0.5, 1.5, 100);
            
            foreach (double spot in spotGrid)
            {
                double payoff = PayoffEngine.TotalPayoff(portfolio.OptionLines, spot);
                payoffData.Add((spot, payoff));
            }

            // Formate les résultats avec les interprétations économiques
            string marketData = $"Prix Spot (S0) : {basket.Spot:F2} €\n" +
                              $"Volatilité (σ) : {basket.HistVol * 100:F2} %\n" +
                              $"Dividende (q) : {basket.DividendYield * 100:F2} %\n" +
                              $"Horizon : {_data.HorizonYears} an(s)";

            // Interprétation de la position nette en utilisant la fonction de bibliothèque
            string netPositionInterpretation = PortfolioAnalyzer.InterpretNetPosition(totalValue);

            string portfolioMetrics = $"Valeur Nette (BS) : {totalValue:F6} €{netPositionInterpretation}\n\n" +
                                    $"Capital initial : {_data.CapitalInitial:F2} €\n" +
                                    $"Capital utilisé : {_data.CapitalUtilise:F2} € ({(_data.CapitalUtilise / _data.CapitalInitial * 100):F1}%)\n" +
                                    $"Capital restant : {_data.Capital:F2} €\n" +
                                    $"Nombre d'options : {portfolio.OptionLines.Count}\n\n" +
                                    perOptionLines.ToString();

            // Interprétations économiques des grecques en utilisant les fonctions de bibliothèque
            string deltaInterpretation = PortfolioAnalyzer.InterpretDelta(totalDelta);

            string gammaInterpretation = PortfolioAnalyzer.InterpretGamma(totalGamma);

            string vegaInterpretation = PortfolioAnalyzer.InterpretVega(totalVega);

            string thetaInterpretation = PortfolioAnalyzer.InterpretTheta(totalTheta);

            string greeksText = $"Delta (Δ) : {totalDelta:F4}\n{deltaInterpretation}\n\n" +
                           $"Gamma (Γ) : {totalGamma:F4}\n{gammaInterpretation}\n\n" +
                           $"Vega (ν) : {totalVega:F4}\n{vegaInterpretation}\n\n" +
                           $"Theta (θ) : {totalTheta:F4} € / jour\n{thetaInterpretation}\n\n" +
                           $"Rho (ρ) : {totalRho:F4}";

            // DÉTECTION DE PRODUITS STRUCTURÉS (mode Avisé uniquement)
            string structuredProductsText = "";
            if (_isAdvanced && portfolio.OptionLines.Count > 1)
            {
                var legs = portfolio.OptionLines.Select(line => 
                    (Opt: line.Opt, Qty: line.Qty)).ToList();
                
                var findings = StructureDetector.Detect(legs);
                
                if (findings.Count > 0)
                {
                    structuredProductsText = "\n\nPRODUITS STRUCTURÉS DÉTECTÉS :\n";
                    foreach (var finding in findings)
                    {
                        structuredProductsText += $"\n• {finding.Name}\n";
                        structuredProductsText += $"  Objectif : {finding.Objective}\n";
                        
                        // Analyse coût/bénéfice
                        if (finding.Name.Contains("Bull Call") || finding.Name.Contains("Bear Put"))
                        {
                            structuredProductsText += "  + Coût réduit vs option simple\n";
                            structuredProductsText += "  - Gain plafonné\n";
                            structuredProductsText += "  → Stratégie DÉFENSIVE\n";
                        }
                        else if (finding.Name.Contains("Butterfly"))
                        {
                            structuredProductsText += "  + Coût très faible (quasi zéro-cost)\n";
                            structuredProductsText += "  - Perte si mouvement important\n";
                            structuredProductsText += "  → Stratégie de STABILITÉ\n";
                        }
                        else if (finding.Name.Contains("Straddle") || finding.Name.Contains("Strangle"))
                        {
                            structuredProductsText += "  - Coût élevé (2 options Long)\n";
                            structuredProductsText += "  + Gain illimité si grand mouvement\n";
                            structuredProductsText += "  → Stratégie de VOLATILITÉ\n";
                        }
                        else if (finding.Name.Contains("Iron Condor"))
                        {
                            structuredProductsText += "  + Crédit net (prime reçue)\n";
                            structuredProductsText += "  - Perte si hors range\n";
                            structuredProductsText += "  → Stratégie de RANGE\n";
                        }
                    }
                }
                else
                {
                    structuredProductsText = "\n\nAucun produit structuré standard détecté.\n";
                    structuredProductsText += "Votre stratégie est personnalisée.";
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _data.PayoffData = payoffData;
                _window.ShowResults(marketData, portfolioMetrics + structuredProductsText, greeksText, payoffData);
            });
        });
    }

    /// Lance un pricing Monte-Carlo du dernier portefeuille calculé de manière asynchrone.
    /// Appelé par l'UI quand l'utilisateur demande Monte-Carlo.
    /// <param name="simulations">Nombre de simulations Monte-Carlo à exécuter.</param>
    public async void RunMonteCarlo(int simulations)
    {
        if (_data.LastPortfolio == null)
        {
            _window.ShowError("Aucun portefeuille calculé. Lancez d'abord une simulation.");
            return;
        }

        _window.UpdateStatus($" Monte-Carlo : lancement ({simulations:N0} simulations)...");

        try
        {
            var mc = new MonteCarloPricer(simulations);
            double mcPrice = await Task.Run(() => _data.LastPortfolio.PriceMonteCarlo(mc));

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Affiche le prix MC dans l'UI et ajoute aussi à l'historique pour traçabilité
                try { _window.ShowMonteCarloResult($"Prix Monte-Carlo ({simulations:N0} sims) : {mcPrice:F6} €"); } catch { }
                _window.AddHistory($" Prix Monte-Carlo ({simulations:N0} sims) : {mcPrice:F6} €");
                _window.UpdateStatus($" Monte-Carlo terminé ({simulations:N0} sims)");
            });
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _window.ShowError($"Erreur Monte-Carlo : {ex.Message}");
            });
        }
    }

    private string[] GetSymbolsFromSectors(List<int> indices)
    {
        var map = new Dictionary<int, string>
        {
            {1, "AAPL"}, {2, "XOM"}, {3, "JPM"}, {4, "JNJ"}, {5, "AMZN"},
            {6, "PG"}, {7, "CAT"}, {8, "LIN"}, {9, "GOOG"}, {10, "NEE"}, {11, "SPG"}
        };

        return indices.Select(i => map[i]).ToArray();
    }

    private Strategy? CreateStrategyFromObjective(int objective, double S0, double sigma, double T, double q)
    {
        double r = 0.03;
        double K1 = S0 * 0.95;
        double K2 = S0 * 1.05;
        double dK = S0 * 0.05;

        return objective switch
        {
            1 => new BullCallSpread(S0, r, q, T, sigma, K1, K2),
            2 => new BearPutSpread(S0, r, q, T, sigma, K1, K2),
            3 => new Butterfly(S0, r, q, T, sigma, S0, dK),
            4 => new Straddle(S0, r, q, T, sigma, S0),
            5 => new Strangle(S0, r, q, T, sigma, K1, K2),
            6 => new CalendarSpread(S0, r, q, T, sigma, S0, T * 1.5),
            7 => new IronCondor(S0, r, q, T, sigma, K1, S0 * 0.975, S0 * 1.025, K2),
            _ => null
        };
    }

    public void ExportPayoffData()
    {
        if (_data.PayoffData == null || _data.PayoffData.Count == 0)
        {
            MessageBox.Show("Aucune donnée à exporter.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filename = Path.Combine(desktop, $"payoff_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("Spot,Payoff");
                foreach (var (spot, payoff) in _data.PayoffData)
                {
                    writer.WriteLine($"{spot:F2},{payoff:F2}");
                }
            }

            MessageBox.Show($"Données exportées :\n{filename}", "Export Réussi", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
            _window.AddHistory($" Export CSV : {Path.GetFileName(filename)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($" Erreur d'export :\n{ex.Message}", "Erreur", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    }

public enum WorkflowState
{
    Capital,
    Sectors,
    Weights,
    WeightsConfirm,
    Horizon,
    Objectives,
    AdvancedChoice,
    SingleOptionParams,
    PortfolioSize,
    OptionParams
}

public class WorkflowData
{
    public double Capital { get; set; }
    public double CapitalInitial { get; set; }
    public double CapitalUtilise { get; set; }
    public List<int> SectorIndices { get; set; } = new();
    // Poids optionnels par symbole fournis par l'utilisateur (même forme que le tableau de symboles passé à BasketBuilder)
    // Les valeurs sont acceptées comme pourcentages (ex : 50, 30, 20) ou poids bruts; BuildWeightedAsync normalisera les valeurs positives.
    public double[]? Weights { get; set; }
    public double HorizonYears { get; set; }
    public List<int> Objectives { get; set; } = new();
    public int AdvancedChoice { get; set; }
    public (string type, double strike, string position) SingleOption { get; set; }
    public int PortfolioSize { get; set; }
    public int CurrentOptionIndex { get; set; }
    public List<(string type, double strike, string position, double qty)>? PortfolioOptions { get; set; }
    public List<(double spot, double payoff)>? PayoffData { get; set; }
    public BasketSnapshot? MarketData { get; set; }
    // Dernier portefeuille calculé (gardé pour supporter le pricing Monte-Carlo à la demande depuis l'UI)
    public Core.Portfolio.Portfolio? LastPortfolio { get; set; }
}
