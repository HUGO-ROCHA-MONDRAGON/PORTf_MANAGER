using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Pricing.WPF;

/// Fenêtre principale de l'application de pricing d'options.
/// Gère la navigation entre les différents panneaux et l'interaction avec le WorkflowController.
public partial class MainWindow : Window
{
    // Contrôleur qui gère toute la logique métier (sélection secteurs, options, calculs)
    private WorkflowController? _controller;
    
    // Mémorise si l'utilisateur a choisi le mode "Avisé" (expert) ou "Non-Avisé" (débutant)
    private bool _isAdvanced;

    public MainWindow()
    {
        InitializeComponent();
        LoadApiKey(); // Charge la clé API depuis les variables d'environnement si elle existe
    }

    // ÉTAPE 1 : Configuration de la clé API
    /// Charge la clé API depuis les variables d'environnement au démarrage.
    /// Si elle existe déjà, on pré-remplit le champ et on active le bouton "Continuer".
    private void LoadApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("TWELVEDATA_API_KEY");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            ApiKeyInput.Text = apiKey;
            ApiKeyStatus.Text = " Clé API chargée";
            ApiKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60)); // Vert
            BtnContinueToProfile.IsEnabled = true;
        }
        else
        {
            ApiKeyStatus.Text = " Entrez votre clé API";
            ApiKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0)); // Orange
            BtnContinueToProfile.IsEnabled = false;
        }
    }

    /// Validation en temps réel pendant que l'utilisateur tape sa clé API.
    /// Active/désactive le bouton "Continuer" selon la validité.
    private void ApiKeyInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        var apiKey = ApiKeyInput.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ApiKeyStatus.Text = " Entrez votre clé API";
            ApiKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0)); // Orange
            BtnContinueToProfile.IsEnabled = false;
        }
        else if (apiKey.Length < 20) // Les clés Twelve Data font normalement 30+ caractères
        {
            ApiKeyStatus.Text = " Clé API trop courte";
            ApiKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Rouge
            BtnContinueToProfile.IsEnabled = false;
        }
        else
        {
            // Stocke la clé dans les variables d'environnement pour cette session
            Environment.SetEnvironmentVariable("TWELVEDATA_API_KEY", apiKey);
            ApiKeyStatus.Text = $" Clé API validée";
            ApiKeyStatus.Foreground = new SolidColorBrush(Color.FromRgb(56, 142, 60)); // Vert
            BtnContinueToProfile.IsEnabled = true;
        }
    }

    /// Passe de l'écran "Configuration API" à l'écran "Choix du profil".
    private void BtnContinueToProfile_Click(object sender, RoutedEventArgs e)
    {
        ApiKeyPanel.Visibility = Visibility.Collapsed;
        ProfilePanel.Visibility = Visibility.Visible;
    }

    /// Retour en arrière : de "Choix du profil" vers "Configuration API".
    private void BtnBackToApi_Click(object sender, RoutedEventArgs e)
    {
        ProfilePanel.Visibility = Visibility.Collapsed;
        ApiKeyPanel.Visibility = Visibility.Visible;
    }

    // ÉTAPE 2 : Choix du profil (Avisé / Non-Avisé)
    /// L'utilisateur choisit le mode "AVISÉ" (expert) : construction manuelle des options.
    private void BtnAvise_Click(object sender, RoutedEventArgs e)
    {
        ShowEtfExplanation(isAdvanced: true);
    }

    /// L'utilisateur choisit le mode "NON-AVISÉ" (débutant) : sélection par objectifs.
    private void BtnNonAvise_Click(object sender, RoutedEventArgs e)
    {
        ShowEtfExplanation(isAdvanced: false);
    }

    /// Affiche l'écran d'explication "ETF virtuel" (action unique vs panier multi-secteurs).
    private void ShowEtfExplanation(bool isAdvanced)
    {
        ProfilePanel.Visibility = Visibility.Collapsed;
        EtfExplanationPanel.Visibility = Visibility.Visible;
        _isAdvanced = isAdvanced; // Mémorise le choix pour plus tard
    }

    /// Passe de l'écran "Explication ETF" au workflow interactif (questions/réponses).
    private void BtnContinueToSectors_Click(object sender, RoutedEventArgs e)
    {
        StartWorkflow(_isAdvanced);
    }

    // ÉTAPE 3 : Démarrage du workflow interactif
    /// Démarre le workflow : cache tous les panneaux sauf celui du workflow,
    /// puis crée le contrôleur qui va poser les questions (secteurs, options, etc.).
    private void StartWorkflow(bool isAdvanced)
    {
        ApiKeyPanel.Visibility = Visibility.Collapsed;
        ProfilePanel.Visibility = Visibility.Collapsed;
        EtfExplanationPanel.Visibility = Visibility.Collapsed;
        WorkflowPanel.Visibility = Visibility.Visible;
        ResultsPanel.Visibility = Visibility.Collapsed;
        
        // Création du contrôleur qui va gérer toute la suite (questions, validation, calculs)
        _controller = new WorkflowController(this, isAdvanced);
        _controller.Start();
    }

    // GESTION DES INTERACTIONS UTILISATEUR
    /// Soumet la réponse de l'utilisateur au contrôleur quand il clique sur "Valider".
    private void BtnSubmit_Click(object sender, RoutedEventArgs e)
    {
        _controller?.ProcessInput(InputBox.Text);
        InputBox.Clear(); // Vide le champ pour la prochaine question
    }

    /// Permet de soumettre aussi avec la touche "Entrée" (plus rapide).
    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnSubmit_Click(sender, e);
        }
    }

    /// Bouton "Annuler" : redirige vers "Nouvelle Simulation" (réinitialisation complète).
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        BtnNewSimulation_Click(sender, e);
    }

    /// Réinitialise tout : retourne au choix du profil, vide l'historique et détruit le contrôleur.
    private void BtnNewSimulation_Click(object sender, RoutedEventArgs e)
    {
        ApiKeyPanel.Visibility = Visibility.Collapsed;
        ProfilePanel.Visibility = Visibility.Visible; // Retour au choix Avisé/Non-Avisé
        EtfExplanationPanel.Visibility = Visibility.Collapsed;
        WorkflowPanel.Visibility = Visibility.Collapsed;
        ResultsPanel.Visibility = Visibility.Collapsed;
        
        // Nettoyage complet de l'interface
        StatusText.Text = "";
        QuestionText.Text = "";
        InputBox.Text = "";
        HistoryLog.Text = "";
        _controller = null; // Détruit le contrôleur actuel
    }

    /// Exporte le payoff du portefeuille dans un fichier CSV.
    private void BtnExportCSV_Click(object sender, RoutedEventArgs e)
    {
        _controller?.ExportPayoffData();
    }

    /// Lance une simulation Monte-Carlo avec le nombre de simulations spécifié par l'utilisateur.
    private void BtnRunMC_Click(object sender, RoutedEventArgs e)
    {
        if (_controller == null)
        {
            McStatusText.Text = " Aucun contrôleur actif";
            return;
        }

        // Validation : l'utilisateur doit entrer un nombre entier positif
        if (!int.TryParse(McSimsInput.Text.Trim(), out int sims) || sims <= 0)
        {
            McStatusText.Text = " Entrez un nombre valide";
            return;
        }

        McStatusText.Text = $" Calcul en cours...";
        McPriceText.Text = string.Empty;

        _controller.RunMonteCarlo(sims); // Lance le calcul (asynchrone dans le contrôleur)
    }
    
    // MÉTHODES APPELÉES PAR LE WORKFLOWCONTROLLER
    // Ces méthodes permettent au contrôleur de mettre à jour l'interface
    // sans avoir à connaître les détails de l'UI WPF.

    /// Affiche le résultat de la simulation Monte-Carlo dans le panneau des résultats.
    public void ShowMonteCarloResult(string text)
    {
        try
        {
            McPriceText.Text = text ?? string.Empty;
            McStatusText.Text = string.Empty; // Efface le message "Calcul en cours..."
        }
        catch { } // Évite les crashes si les contrôles n'existent pas
    }

    /// Met à jour le message de statut (en bleu) dans le panneau workflow.
    public void UpdateStatus(string message)
    {
        StatusText.Text = message;
        try
        {
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(25, 118, 210)); // Bleu
        }
        catch { }
    }
    
    /// Affiche un message d'erreur (en rouge) dans le panneau workflow.
    public void ShowError(string message)
    {
        StatusText.Text = message;
        try
        {
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Rouge
        }
        catch { }
    }

    /// Met à jour la question affichée dans le panneau workflow.
    public void UpdateQuestion(string question)
    {
        QuestionText.Text = question;
    }

    // GESTION DES PANNEAUX D'AIDE CONTEXTUELS
    /// Affiche l'aide détaillée pour les objectifs (mode Non-Avisé).
    public void ShowObjectiveHelp(string help)
    {
        if (string.IsNullOrWhiteSpace(help))
        {
            ObjectiveHelpBorder.Visibility = Visibility.Collapsed;
            ObjectiveHelpText.Text = string.Empty;
            return;
        }

        ObjectiveHelpText.Text = help;
        ObjectiveHelpBorder.Visibility = Visibility.Visible;
    }

    /// Cache le panneau d'aide des objectifs.
    public void HideObjectiveHelp()
    {
        ObjectiveHelpBorder.Visibility = Visibility.Collapsed;
        ObjectiveHelpText.Text = string.Empty;
    }

    /// Pré-charge le texte d'aide (mais le garde masqué jusqu'à ce que l'utilisateur clique sur "En savoir plus").
    public void SetObjectiveHelpText(string help)
    {
        ObjectiveHelpText.Text = help ?? string.Empty;
        ObjectiveHelpBorder.Visibility = Visibility.Collapsed;
        try
        {
            HelpToggleLink.Inlines.Clear();
            HelpToggleLink.Inlines.Add(new Run("En savoir plus"));
        }
        catch { }
    }

    /// Affiche un aperçu rapide des données de marché (S0, volatilité, dividendes, horizon).
    public void ShowMarketPreview(string text)
    {
        try
        {
            MarketPreviewText.Text = text ?? string.Empty;
            MarketPreviewBorder.Visibility = Visibility.Visible;
        }
        catch { }
    }

    /// Cache le panneau d'aperçu marché.
    public void HideMarketPreview()
    {
        try
        {
            MarketPreviewBorder.Visibility = Visibility.Collapsed;
            MarketPreviewText.Text = string.Empty;
        }
        catch { }
    }

    /// Affiche une aide courte (une ligne) au-dessus de la zone de saisie.
    public void ShowShortHelp(string text)
    {
        try
        {
            ShortHelpText.Text = text ?? string.Empty;
            ShortHelpPanel.Visibility = Visibility.Visible;
        }
        catch { }
    }

    /// Cache l'aide courte.
    public void HideShortHelp()
    {
        try
        {
            ShortHelpPanel.Visibility = Visibility.Collapsed;
            ShortHelpText.Text = string.Empty;
        }
        catch { }
    }

    /// Toggle (afficher/masquer) le panneau d'aide détaillée des objectifs.
    private void HelpToggleLink_Click(object sender, RoutedEventArgs e)
    {
        if (ObjectiveHelpBorder.Visibility == Visibility.Visible)
        {
            ObjectiveHelpBorder.Visibility = Visibility.Collapsed;
            HelpToggleLink.Inlines.Clear();
            HelpToggleLink.Inlines.Add(new Run("En savoir plus"));
        }
        else
        {
            ObjectiveHelpBorder.Visibility = Visibility.Visible;
            HelpToggleLink.Inlines.Clear();
            HelpToggleLink.Inlines.Add(new Run("Réduire"));
        }
    }

    // HISTORIQUE ET AFFICHAGE DES RÉSULTATS
    /// Ajoute une ligne à l'historique des étapes (visible dans le panneau workflow).
    public void AddHistory(string entry)
    {
        if (!string.IsNullOrEmpty(HistoryLog.Text))
            HistoryLog.Text += "\n";
        HistoryLog.Text += entry;
    }

    /// Affiche les résultats finaux : données de marché, métriques du portefeuille, grecques et graphique.
    public void ShowResults(string marketData, string portfolioMetrics, string greeks, List<(double spot, double payoff)> payoffData, System.Collections.IEnumerable? optionDetails = null)
    {
        WorkflowPanel.Visibility = Visibility.Collapsed;
        ResultsPanel.Visibility = Visibility.Visible;

        MarketDataText.Text = marketData;
        PortfolioMetricsText.Text = portfolioMetrics;

        // Affiche le tableau détaillé des options si fourni (mode Avisé)
        try
        {
            if (optionDetails != null)
            {
                OptionDetailsGrid.ItemsSource = optionDetails;
                OptionDetailsGrid.Visibility = Visibility.Visible;
            }
            else
            {
                OptionDetailsGrid.ItemsSource = null;
                OptionDetailsGrid.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
        
        GreeksText.Text = greeks;

        DrawPayoffChart(payoffData); // Dessine le graphique du payoff
    }

    /// Dessine le graphique de payoff avec ScottPlot.
    /// Affiche le profit/perte en fonction du prix du sous-jacent à maturité.
    private void DrawPayoffChart(List<(double spot, double payoff)> data)
    {
        // Utilise Dispatcher.InvokeAsync pour garantir que le graphique est dessiné sur le thread UI
        Dispatcher.InvokeAsync(() =>
        {
            PayoffPlot.Plot.Clear();
            
            if (data.Count == 0)
            {
                PayoffPlot.Plot.Title("Aucune donnée disponible");
                PayoffPlot.Refresh();
                return;
            }

            // Conversion des tuples en tableaux pour ScottPlot
            double[] xs = data.Select(d => d.spot).ToArray();
            double[] ys = data.Select(d => d.payoff).ToArray();

            // Courbe du payoff (en bleu)
            var scatter = PayoffPlot.Plot.Add.Scatter(xs, ys);
            scatter.LineWidth = 2;
            scatter.Color = ScottPlot.Color.FromHex("#2196F3");
            
            // Ligne horizontale à zéro (profit/perte nul) en rouge pointillé
            var zeroLine = PayoffPlot.Plot.Add.HorizontalLine(0);
            zeroLine.LineColor = ScottPlot.Color.FromHex("#F44336");
            zeroLine.LineWidth = 1;
            zeroLine.LinePattern = ScottPlot.LinePattern.Dashed;

            // Titres des axes
            PayoffPlot.Plot.Axes.Title.Label.Text = "Payoff à Maturité";
            PayoffPlot.Plot.Axes.Bottom.Label.Text = "Prix du Sous-Jacent (S)";
            PayoffPlot.Plot.Axes.Left.Label.Text = "Payoff (€)";
            
            PayoffPlot.Plot.Axes.AutoScale(); // Ajuste automatiquement les échelles
            PayoffPlot.Refresh();
        }, System.Windows.Threading.DispatcherPriority.Render);
    }
}
