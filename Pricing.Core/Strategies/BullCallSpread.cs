using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    /// Bull Call Spread : stratégie haussière modérée avec risque limité.
    /// Long call à K1 (strike bas) + short call à K2 (strike haut).
    /// Gain max = K2 - K1 - prime nette payée
    public sealed class BullCallSpread : Strategy
    {
        public BullCallSpread(double S0, double r, double q, double T, double sigma, double K1, double K2)
        {
            // Achat d'un call au strike bas pour profiter de la hausse
            Add(new CallOption(S0, K1, r, q, T, sigma), +1);
            
            // Vente d'un call au strike haut pour financer l'achat (réduit le coût initial)
            Add(new CallOption(S0, K2, r, q, T, sigma), -1);
        }
    }
}
