namespace Pricing.Core.Annexe
{
    public sealed class DeltaHedgingResult
    {
        public int Paths { get; init; }
        public int Steps { get; init; }

        /// Paramètre de coût unitaire employé dans la simu 
        public double GammaCost { get; init; }

        /// PnL net moyen (deja net des coûts de transaction)
        public double MeanPnL { get; init; }

        /// Écart-type du PnL net
        public double StdPnL { get; init; }

        /// Coût de transaction moyen 
        public double MeanTransactionCost { get; init; }
    }
}
