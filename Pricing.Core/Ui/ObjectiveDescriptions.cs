using System.Text;

namespace Pricing.Core.Ui
{
    /// Textes centralisés pour le choix d'objectifs investisseur.
    public static class ObjectiveDescriptions
    {
        /// Prompt WPF pour le choix des objectifs (affichage multi-ligne).
        public static string GetWpfObjectivesPrompt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Choisissez jusqu'à 3 objectifs (séparés par virgules) :\n");
            sb.AppendLine("1 = Augmentation du marché (Bull Call Spread)");
            sb.AppendLine("2 = Baisse du marché (Bear Put Spread)");
            sb.AppendLine("3 = Stabilité (Butterfly)");
            sb.AppendLine("4 = Forte volatilité (Straddle)");
            sb.AppendLine("5 = Volatilité modérée (Strangle)");
            sb.AppendLine("6 = Vision à long terme (Calendar Spread)");
            sb.AppendLine("7 = Range trading (Iron Condor)\n");
            sb.Append("Exemple : 1,4");
            return sb.ToString();
        }

        /// Aide détaillée pour chaque objectif - explications accessibles.
        public static string GetDetailedHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Objectifs disponibles :");
            sb.AppendLine();
            sb.AppendLine("1) Hausse modérée — Si vous pensez que le prix va augmenter un peu : stratégie peu coûteuse qui profite d'une montée progressive.");
            sb.AppendLine("2) Baisse modérée — Si vous pensez que le prix va baisser un peu : protection contre la baisse avec coût limité.");
            sb.AppendLine("3) Stabilité (Butterfly) — Si vous pensez que le prix restera proche d'un niveau : permet de gagner si le prix reste stable.");
            sb.AppendLine("4) Forte volatilité — Si vous attendez un grand mouvement (dans un sens ou l'autre) : pari sur la montée de la volatilité.");
            sb.AppendLine("5) Volatilité modérée (Strangle) — Similaire au straddle mais moins cher : utile si vous attendez un mouvement mais pas forcément symétrique.");
            sb.AppendLine("6) Vision à long terme (Calendar Spread) — Parier sur la différence de comportement entre courtes et longues échéances.");
            sb.AppendLine("7) Range trading (Iron Condor) — Si vous pensez que le prix restera dans une fourchette : objectif de gain limité avec risque contrôlé.");
            sb.AppendLine();
            sb.AppendLine("Comment l'utiliser : tapez les numéros séparés par des virgules (ex : 1,4). L'application vous propose ensuite une stratégie adaptée.");
            sb.AppendLine();
            sb.AppendLine("Conseil : Si vous n'êtes pas sûr, choisissez '1' pour une approche simple et peu risquée, ou '3' si vous pensez que le prix restera stable.");
            return sb.ToString();
        }
    }
}