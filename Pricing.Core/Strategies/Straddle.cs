using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    /// Straddle : pari pur sur une explosion de volatilité.
    /// Long call + long put au même strike (souvent ATM).
    /// Coûte cher mais profitable si grosse variation dans un sens ou l'autre.
    /// Risque : marché qui reste flat → perte des primes payées.
    public sealed class Straddle : Strategy
    {
        public Straddle(double S0, double r, double q, double T, double sigma, double K)
        {
            // Achat simultané call + put même strike
            // Si le prix monte fort → call gagne
            // Si le prix chute fort → put gagne
            Add(new CallOption(S0, K, r, q, T, sigma), +1);
            Add(new PutOption (S0, K, r, q, T, sigma), +1);
        }
    }
}
