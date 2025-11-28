using Pricing.Core.Models;
using MathNet.Numerics;

namespace Pricing.Core.Pricing
{
    /// Implémentation du modèle Black-Scholes pour options européennes
    /// avec calcul des grecques (Delta, Gamma, Vega, Theta, Rho)
    public sealed class BlackScholesPricer : IPricingMethod
    {
        // Fonction de densité de la loi normale centrée réduite
        private static double Nd(double x) => Math.Exp(-0.5 * x * x) / Math.Sqrt(2 * Math.PI);

        // Fonction de répartition normale (CDF) 
        private static double N(double x)
        {
            return 0.5 * (1.0 + SpecialFunctions.Erf(x / Math.Sqrt(2.0)));
        }

        // Calcule de d1 et d2
        private static (double d1, double d2) D1D2(Option o)
        {
            double v = o.Sigma;
            double vSqrtT = v * Math.Sqrt(o.T);
            double d1 = (Math.Log(o.S0 / o.K) + (o.r - o.q + 0.5 * v * v) * o.T) / vSqrtT;
            return (d1, d1 - vSqrtT);
        }

        // Prix de l'option
        public double Price(Option o)
        {
            var (d1, d2) = D1D2(o);
            double dfR = Math.Exp(-o.r * o.T);
            double dfQ = Math.Exp(-o.q * o.T);
            
            // Formule classique de BS avec dividendes
            return o.IsCall
                ? o.S0 * dfQ * N(d1) - o.K * dfR * N(d2)
                : o.K * dfR * N(-d2) - o.S0 * dfQ * N(-d1);
        }

        // Delta : sensibilité au spot
        public double Delta(Option o)
        {
            var (d1, _) = D1D2(o);
            double dfQ = Math.Exp(-o.q * o.T);
            return o.IsCall ? dfQ * N(d1) : dfQ * (N(d1) - 1.0);
        }

        // Gamma : sensibilité du delta
        public double Gamma(Option o)
        {
            var (d1, _) = D1D2(o);
            return Math.Exp(-o.q * o.T) * Nd(d1) / (o.S0 * o.Sigma * Math.Sqrt(o.T));
        }

        // Vega : sensibilité à la volatilité (par point de %) 
        public double Vega(Option o)
        {
            var (d1, _) = D1D2(o);
            double vega = o.S0 * Math.Exp(-o.q * o.T) * Nd(d1) * Math.Sqrt(o.T);
            return vega * 0.01; 
        }

        // Theta : sensibilité au temps (par jour)
        public double Theta(Option o)
        {
            var (d1, d2) = D1D2(o);
            double term1 = -o.S0 * Math.Exp(-o.q * o.T) * Nd(d1) * o.Sigma / (2 * Math.Sqrt(o.T));
            
            double thetaPerYear = o.IsCall
                ? term1 - o.r * o.K * Math.Exp(-o.r * o.T) * N(d2) + o.q * o.S0 * Math.Exp(-o.q * o.T) * N(d1)
                : term1 + o.r * o.K * Math.Exp(-o.r * o.T) * N(-d2) - o.q * o.S0 * Math.Exp(-o.q * o.T) * N(-d1);

            return thetaPerYear / 365.0; 
        }

        // Rho : sensibilité au taux d'intérêt (par point de %)
        public double Rho(Option o)
        {
            var (_, d2) = D1D2(o);
            double kTdf = o.K * o.T * Math.Exp(-o.r * o.T);
            double rho = o.IsCall ? kTdf * N(d2) : -kTdf * N(-d2);
            return rho * 0.01; 
        }
    }
}
