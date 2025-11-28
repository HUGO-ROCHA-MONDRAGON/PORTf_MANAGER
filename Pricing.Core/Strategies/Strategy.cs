using Pricing.Core.Models;
using Pricing.Core.Pricing;

namespace Pricing.Core.Strategies
{
    /// Classe de base pour toutes les stratégies d'options.
    /// Une stratégie = collection de jambes (legs), chaque jambe = option + quantité signée.
    /// Quantité > 0 = Long (achat), Quantité < 0 = Short (vente).
    public abstract class Strategy
    {
        protected readonly List<(Option Opt, int Qty)> Legs = new();

        protected void Add(Option opt, int qty) => Legs.Add((opt, qty));

        public IReadOnlyList<(Option Opt, int Qty)> GetLegs() => Legs;

        // Calcul du prix total de la stratégie (somme pondérée des primes)
        public double Price(IPricingMethod pricer)
            => Legs.Sum(l => l.Qty * pricer.Price(l.Opt));

        // Calcul des grecques : on somme simplement les grecques de chaque jambe pondérées par la quantité
        public double Delta(IPricingMethod pricer)
            => Legs.Sum(l => l.Qty * pricer.Delta(l.Opt));

        public double Gamma(IPricingMethod pricer)
            => Legs.Sum(l => l.Qty * pricer.Gamma(l.Opt));

        public double Vega(IPricingMethod pricer)
            => Legs.Sum(l => l.Qty * pricer.Vega(l.Opt));

        public double Theta(IPricingMethod pricer)
            => Legs.Sum(l => l.Qty * pricer.Theta(l.Opt));

        public double Rho(IPricingMethod pricer)
            => Legs.Sum(l => l.Qty * pricer.Rho(l.Opt));

        /// Génère une description textuelle de la stratégie.
        /// Format : "Long 1C(K=100, T=0.25) + Short 1C(K=110, T=0.25)"
        public virtual string Describe()
        {
            string LegText((Option Opt, int Qty) l)
            {
                string side = l.Qty >= 0 ? "Long" : "Short";
                string type = l.Opt.IsCall ? "C" : "P";
                string tStr = l.Opt.T.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
                return $"{side} {Math.Abs(l.Qty)}{type}(K={l.Opt.K:0.##}, T={tStr})";
            }
            return string.Join(" + ", Legs.Select(LegText));
        }
    }
}
