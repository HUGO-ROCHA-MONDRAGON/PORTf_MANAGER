using Pricing.Core.Models;
using Pricing.Core.Pricing;

namespace Pricing.Core.Annexe
{
    /// Solveur de volatilité implicite
    /// Utilise Newton-Raphson avec comme sol de secours bissection si ça converge pas
    public static class ImpliedVolSolver
    {
        /// Trouve la vol σ qui donne le prix observé sur le marché
        /// Utile pour calibrer le modèle ou détecter des opportunités d'arbitrage
        public static double Solve(
            Option o,
            double targetPrice,
            double tol = 1e-8,
            int maxIter = 50,
            double low = 0.001,
            double high = 2.0)
        {
            if (targetPrice <= 0) throw new ArgumentException("Le prix cible doit être > 0.", nameof(targetPrice));
            if (o.T <= 0) throw new ArgumentException("T doit être > 0 pour inverser la vol.", nameof(o));

            var pricer = new BlackScholesPricer();

            // Première tentative : Newton-Raphson (rapide si ça converge)
            double sigma = Math.Clamp(o.Sigma > 0 ? o.Sigma : 0.20, low, high);
            for (int i = 0; i < maxIter; i++)
            {
                o.Sigma = sigma;
                double price = pricer.Price(o);
                double err = price - targetPrice;
                if (Math.Abs(err) < tol) return sigma;

                double vega = pricer.Vega(o);
                // Si vega proche de 0 ou NaN, Newton va diverger -> on passe en bissection
                if (vega < 1e-8 || double.IsNaN(vega)) break;

                double next = sigma - err / vega;
                if (double.IsNaN(next) || next <= low || next >= high) break;

                sigma = next;
            }

            //  Solution : bissection (plus lent mais toujours converge)
            double a = low, b = high;
            o.Sigma = a; double fa = pricer.Price(o) - targetPrice;
            o.Sigma = b; double fb = pricer.Price(o) - targetPrice;

            if (fa * fb > 0)
                throw new InvalidOperationException("Racine non bornée : ajustez low/high ou vérifiez targetPrice.");

            for (int i = 0; i < maxIter; i++)
            {
                double m = 0.5 * (a + b);
                o.Sigma = m;
                double fm = pricer.Price(o) - targetPrice;

                if (Math.Abs(fm) < tol || (b - a) < 1e-10)
                    return m;

                // Dichotomie classique
                if (fa * fm <= 0) { b = m; fb = fm; }
                else { a = m; fa = fm; }
            }

            return 0.5 * (a + b);  // Valeur médiane si pas totalement convergé
        }
    }
}
