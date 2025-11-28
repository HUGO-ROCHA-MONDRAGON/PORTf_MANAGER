using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    /// Bear Put Spread : pari baissier avec perte maximale définie.
    /// Long put à K2 (strike haut) + short put à K1 (strike bas).
    /// Rentable si le sous-jacent baisse sous K2, mais gain plafonné.
    public sealed class BearPutSpread : Strategy
    {
        public BearPutSpread(double S0, double r, double q, double T, double sigma, double K1, double K2)
        {
            // Achat put strike haut : protection baisse
            Add(new PutOption (S0, K2, r, q, T, sigma), +1);
            
            // Vente put strike bas : finance partiellement l'achat (spread moins cher qu'un put seul)
            Add(new PutOption (S0, K1, r, q, T, sigma), -1);
        }
    }
}
