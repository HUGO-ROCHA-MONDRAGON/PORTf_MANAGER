using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    /// Strangle : variante du straddle, moins chère mais moins sensible.
    /// Long put OTM bas + long call OTM haut.
    /// Rentable uniquement si mouvement important (breakeven plus large qu'un straddle).
    public sealed class Strangle : Strategy
    {
        public Strangle(double S0, double r, double q, double T, double sigma, double K1, double K2)
        {
            // Put OTM bas : assurance baisse forte
            Add(new PutOption (S0, K1, r, q, T, sigma), +1);
            
            // Call OTM haut : assurance hausse forte
            // Zone morte entre K1 et K2 → perte totale des primes si le prix reste là
            Add(new CallOption(S0, K2, r, q, T, sigma), +1);
        }
    }
}
