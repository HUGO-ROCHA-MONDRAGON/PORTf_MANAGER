using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    /// Butterfly spread : mise sur une faible volatilité.
    /// Construction : 1 call bas + 2 calls vendus au milieu + 1 call haut.
    /// Profit max si le spot finit exactement à K (le strike central).
    /// Bon rapport risque/rendement mais zone de profit étroite.
    public sealed class Butterfly : Strategy
    {
        public Butterfly(double S0, double r, double q, double T, double sigma, double K, double dK)
        {
            // Long call OTM bas
            Add(new CallOption(S0, K - dK, r, q, T, sigma), +1);
            
            // Short 2 calls ATM → génère de la prime, c'est le cœur de la stratégie
            Add(new CallOption(S0, K,       r, q, T, sigma), -2);
            
            // Long call OTM haut → symétrisation du profil
            Add(new CallOption(S0, K + dK,  r, q, T, sigma), +1);
        }
    }
}
