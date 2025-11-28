using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    /// Calendar Spread (ou Time Spread) : exploitation de la décroissance temporelle (theta).
    /// Long call échéance longue + short call échéance courte, même strike.
    /// Profitable si le sous-jacent reste proche de K et que le temps passe.
    /// La position courte perd de la valeur plus vite que la longue (time decay différentiel).
    public sealed class CalendarSpread : Strategy
    {
        public CalendarSpread(double S0, double r, double q, double T_short, double T_long, double sigma, double K)
        {
            // Long call échéance lointaine : garde de la valeur temps
            Add(new CallOption(S0, K, r, q, T_long,  sigma), +1);
            
            // Short call échéance proche : perd rapidement sa valeur extrinsèque
            // Le but c'est de profiter de ce différentiel de theta
            Add(new CallOption(S0, K, r, q, T_short, sigma), -1);
        }
    }
}
