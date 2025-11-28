using Pricing.Core.Models;
using Pricing.Core.Pricing;

namespace Pricing.Core.Analysis
{
    /// Analyseur de portefeuille : calcule les besoins en capital + interprète les grecques.
    public static class PortfolioAnalyzer
    {
        /// Calcul du capital nécessaire pour tenir le portefeuille.
        public static double CalculateCapitalRequired(
            IEnumerable<(Option opt, int qty)> legs,
            IPricingMethod pricer,
            double marginPct = 0.20)
        {
            double capital = 0.0;
            foreach (var (opt, qty) in legs)
            {
                var unitPrice = pricer.Price(opt);
                if (qty > 0)
                {
                    // Position longue : on paie la prime
                    capital += unitPrice * qty * 100.0;
                }
                else if (qty < 0)
                {
                    // Position vendue : le broker demande de la marge
                    // Ici on estime à 20% du notionnel
                    capital += opt.S0 * Math.Abs(qty) * 100.0 * marginPct;
                }
            }
            return capital;
        }

        public static string InterpretDelta(double delta)
        {
            if (delta > 0.1)
                return "→ Portefeuille HAUSSIER (gagne si sous-jacent monte)";
            else if (delta < -0.1)
                return "→ Portefeuille BAISSIER (gagne si sous-jacent baisse)";
            else
                return "→ Portefeuille NEUTRE (peu sensible aux mouvements)";
        }

        public static string InterpretGamma(double gamma)
        {
            if (gamma > 0)
                return "→ Gamma POSITIF : profitez des grands mouvements (Long options)";
            else if (gamma < 0)
                return "→ Gamma NÉGATIF : perdez sur grands mouvements (Short options)";
            else
                return "→ Gamma neutre";
        }

        public static string InterpretVega(double vega)
        {
            if (vega > 0.01)
                return "→ Vous GAGNEZ si volatilité augmente (Long Vega)";
            else if (vega < -0.01)
                return "→ Vous PERDEZ si volatilité augmente (Short Vega)";
            else
                return "→ Peu sensible à la volatilité";
        }

        public static string InterpretTheta(double theta)
        {
            if (theta < -0.01)
                return "→ Vous PERDEZ de la valeur chaque jour (decay défavorable)";
            else if (theta > 0.01)
                return "→ Vous GAGNEZ de la valeur chaque jour (decay favorable)";
            else
                return "→ Peu sensible au temps";
        }

        public static string InterpretNetPosition(double totalValue)
        {
            if (totalValue > 0)
                return $"\nDÉBIT net : {Math.Abs(totalValue):F2} € (positions Long dominantes)\n   → Vous avez PAYÉ plus que REÇU en primes";
            else if (totalValue < 0)
                return $"\nCRÉDIT net : {Math.Abs(totalValue):F2} € (positions Short dominantes)\n   → Vous avez REÇU plus que PAYÉ en primes";
            else
                return "\nPortefeuille équilibré (zéro-cost strategy)";
        }
    }
}
