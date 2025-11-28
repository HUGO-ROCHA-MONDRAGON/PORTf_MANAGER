using System.Globalization;
using Pricing.Core.Models;

namespace Pricing.Core.Analysis
{
    /// Moteur d'analyse de payoff : génère les graphiques de PnL.
    /// Calcule le profil gain/perte d'une stratégie selon différents prix finaux.
    public static class PayoffEngine
    {
        /// Génère une grille de prix pour l'axe X du graphique.
        /// Par défaut de 20% à 200% du spot (ça couvre la plupart des scénarios).
        public static double[] GenerateGrid(double s0, double minPct = 0.20, double maxPct = 2.00, int points = 201)
        {
            if (points < 2) points = 2; // au minimum 2 points sinon pas de graphique
            double min = Math.Max(0.0, minPct * s0);
            double max = Math.Max(min + 1e-8, maxPct * s0);
            double[] grid = new double[points];
            double step = (max - min) / (points - 1);
            for (int i = 0; i < points; i++) grid[i] = min + i * step;
            return grid;
        }

        /// Payoff d'une option individuelle à l'échéance.
        /// Call : max(S_T - K, 0) | Put: max(K - S_T, 0)
        public static double LegPayoff(Option o, double ST) =>
            o.IsCall ? Math.Max(ST - o.K, 0.0) : Math.Max(o.K - ST, 0.0);

        /// Payoff total d'une stratégie = somme pondérée des payoffs de chaque option.
        public static double TotalPayoff(IEnumerable<(Option Opt, int Qty)> legs, double ST)
            => legs.Sum(l => l.Qty * LegPayoff(l.Opt, ST));

        /// Export CSV pour analyse externe
        /// Colonnes : S_T (prix final), Payoff brut, Profit net (payoff - prime)
        public static void ExportCsv(string filePath, IEnumerable<(Option Opt, int Qty)> legs, double[] grid, double premiumInitiale)
        {
            using var sw = new StreamWriter(filePath, false);
            sw.WriteLine("S_T;Payoff;Profit;PremiumInitiale");
            foreach (double ST in grid)
            {
                double payoff = TotalPayoff(legs, ST);
                double profit = payoff - premiumInitiale; // profit = payoff - coût initial
                sw.WriteLine($"{ST.ToString(CultureInfo.InvariantCulture)};{payoff.ToString(CultureInfo.InvariantCulture)};{profit.ToString(CultureInfo.InvariantCulture)};{premiumInitiale.ToString(CultureInfo.InvariantCulture)}");
            }
        }
    }
}
