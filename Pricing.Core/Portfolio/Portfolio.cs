using Pricing.Core.Models;
using Pricing.Core.Pricing;
using Pricing.Core.Strategies;

namespace Pricing.Core.Portfolio
{
    /// Portefeuille d'options et/ou de stratégies
    /// Permet de combiner plusieurs positions (long/short) et calculer les grecques globales
    public sealed class Portfolio
    {
        // Stockage des positions : options simples et stratégies complètes
        private readonly List<(Option Opt, int Qty)> _optionLines = new();
        private readonly List<(Strategy Strat, int Qty)> _strategyLines = new();

        public IReadOnlyList<(Option Opt, int Qty)> OptionLines => _optionLines;
        public IReadOnlyList<(Strategy Strat, int Qty)> StrategyLines => _strategyLines;

        // Ajouter une option (qty > 0 = long, qty < 0 = short)
        public void AddOption(Option o, int qty = 1)
        {
            if (qty == 0) return;  // ignore les quantités nulles
            _optionLines.Add((o, qty));
        }

        // Ajouter une stratégie complète (ex: bull call spread)
        public void AddStrategy(Strategy s, int qty = 1)
        {
            if (qty == 0) return;
            _strategyLines.Add((s, qty));
        }

        /// décompose toutes les stratégies en option individuelle
        /// Utile pour calculer les grecques totales
        public IEnumerable<(Option Opt, int Qty)> FlattenLegs()
        {
            // D'abord les options directes
            foreach (var (o, q) in _optionLines) yield return (o, q);
            
            // Puis on décompose chaque stratégie en ses jambes
            foreach (var (s, q) in _strategyLines)
                foreach (var leg in s.GetLegs())
                    yield return (leg.Opt, leg.Qty * q);
        }

        // Calcul des grecques : on somme sur toutes les options décomposées
        public double Price(IPricingMethod pricer) => FlattenLegs().Sum(l => l.Qty * pricer.Price(l.Opt));
        public double Delta(IPricingMethod pricer) => FlattenLegs().Sum(l => l.Qty * pricer.Delta(l.Opt));
        public double Gamma(IPricingMethod pricer) => FlattenLegs().Sum(l => l.Qty * pricer.Gamma(l.Opt));
        public double Vega(IPricingMethod pricer)  => FlattenLegs().Sum(l => l.Qty * pricer.Vega(l.Opt));
        public double Theta(IPricingMethod pricer) => FlattenLegs().Sum(l => l.Qty * pricer.Theta(l.Opt));
        public double Rho(IPricingMethod pricer)   => FlattenLegs().Sum(l => l.Qty * pricer.Rho(l.Opt));

        // Prix via Monte Carlo
        public double PriceMonteCarlo(MonteCarloPricer mc) => FlattenLegs().Sum(l => l.Qty * mc.Price(l.Opt));
    }
}
