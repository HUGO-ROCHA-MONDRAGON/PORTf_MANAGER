using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    /// Iron Condor : position neutre pour marché range-bound.
    /// Combinaison : Bear Call Spread (calls) + Bull Put Spread (puts).
    /// Profit max si le prix reste dans la fourchette [K2, K3] à l'échéance.
    /// Perte limitée mais significative si grosse cassure hors de la zone.
    public sealed class IronCondor : Strategy
    {
        public IronCondor(double S0, double r, double q, double T, double sigma,
                          double K1, double K2, double K3, double K4)
        {
            // Bull Put Spread (partie basse) : K1 < K2
            Add(new PutOption (S0, K1, r, q, T, sigma), +1);  // long put OTM bas
            Add(new PutOption (S0, K2, r, q, T, sigma), -1);  // short put proche ATM
            
            // Bear Call Spread (partie haute) : K3 < K4
            Add(new CallOption(S0, K3, r, q, T, sigma), -1);  // short call proche ATM
            Add(new CallOption(S0, K4, r, q, T, sigma), +1);  // long call OTM haut
            
            // Zone de profit : entre K2 et K3 (on encaisse les primes des positions vendues)
        }
    }
}
