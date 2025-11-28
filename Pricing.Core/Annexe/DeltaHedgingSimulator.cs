using System.Security.Cryptography;
using Pricing.Core.Models;
using Pricing.Core.Pricing;

namespace Pricing.Core.Annexe
{
    public static class DeltaHedgingSimulator
    {
        /// Simule une couverture delta discrète sous GBM, avec coût de transaction gamma * |Δ_{i+1}-Δ_i| * S.
        /// PnL net = (Δ_N S_T + B_N) - payoff(S_T)  (les coûts sont déjà retirés dans B)
        public static DeltaHedgingResult Run(
            Option opt,
            IPricingMethod pricer,
            int steps = 50,
            int paths = 2000,
            double gamma = 0.0,
            ulong? seed = null)
        {
            if (steps <= 1) throw new ArgumentException("steps doit être >= 2");
            if (paths <= 0) throw new ArgumentException("paths doit être > 0");
            if (opt.T <= 0) throw new ArgumentException("T doit être > 0");

            double dt = opt.T / steps;
            double mu = (opt.r - opt.q - 0.5 * opt.Sigma * opt.Sigma) * dt;
            double sigSqrtDt = opt.Sigma * Math.Sqrt(dt);

            var pnls = new double[paths];
            var tcosts = new double[paths];

            using RandomNumberGenerator rng = CreateRng(seed);

            Span<byte> buf = stackalloc byte[16]; // pour 2 uniforms indépendants

            for (int p = 0; p < paths; p++)
            {
                // 1) Initialisation
                double S = opt.S0;
                double t = 0.0;

                double delta = DeltaAt(pricer, opt, S, opt.T);
                double price0 = PriceAt(pricer, opt, S, opt.T);

                // Portefeuille réplicant (sans frictions) : V0 = Price0 = Δ0 S0 + B0
                double B = price0 - delta * S;

                double tcSum = 0.0; // accumulateur de couts

                // 2) Boucle de rebalancement
                for (int i = 0; i < steps; i++)
                {
                    t += dt;

                    // Simule S_{t+dt} (GBM) via 2 uniforms indépendants (Box-Muller)
                    rng.GetBytes(buf);
                    double u1 = (BitConverter.ToUInt64(buf[..8]) / (double)ulong.MaxValue);
                    double u2 = (BitConverter.ToUInt64(buf[8..]) / (double)ulong.MaxValue);
                    if (u1 <= 0.0 || u1 >= 1.0 || u2 <= 0.0 || u2 >= 1.0) { i--; continue; }

                    double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2 * Math.PI * u2);
                    S = S * Math.Exp(mu + sigSqrtDt * z);

                    // Accrue le cash au taux r
                    B *= Math.Exp(opt.r * dt);

                    // Recalcule Δ pour maturité restante
                    double Tleft = Math.Max(opt.T - t, 0.0);
                    double newDelta = DeltaAt(pricer, opt, S, Tleft);

                    // Rebalancement
                    double dDelta = newDelta - delta;
                    if (Math.Abs(dDelta) > 1e-12)
                    {
                        // Achat/vente dΔ actions
                        B -= dDelta * S;

                        // Coût de transaction (proportionnel à notional échangé)
                        if (gamma > 0.0)
                        {
                            double c = gamma * Math.Abs(dDelta) * S;
                            tcSum += c;
                            B -= c; // on retire le coût du cash pour que PnL soit net
                        }
                    }

                    delta = newDelta;
                }

                // 3) Clôture à maturité : valeur du portefeuille répliquant vs payoff
                double payoff = opt.IsCall ? Math.Max(S - opt.K, 0.0) : Math.Max(opt.K - S, 0.0);
                double portfolio = delta * S + B;

                pnls[p] = portfolio - payoff; // déjà net des coûts
                tcosts[p] = tcSum;
            }

            // Statistiques
            double mean = pnls.Average();
            double var = pnls.Select(x => (x - mean) * (x - mean)).Average();
            double std = Math.Sqrt(Math.Max(var, 0.0));
            double meanCost = tcosts.Average();

            return new DeltaHedgingResult
            {
                Paths = paths,
                Steps = steps,
                GammaCost = gamma,
                MeanPnL = mean,
                StdPnL = std,
                MeanTransactionCost = meanCost
            };
        }

        // Helpers : recalcule prix/delta avec S et T résiduel sans muter l'option initiale
        private static double PriceAt(IPricingMethod pricer, Option o, double spotPrice, double timeLeft)
        {
            Option slice = o.IsCall
                ? new CallOption(spotPrice, o.K, o.r, o.q, timeLeft, o.Sigma)
                : new PutOption (spotPrice, o.K, o.r, o.q, timeLeft, o.Sigma);
            return pricer.Price(slice);
        }

        private static double DeltaAt(IPricingMethod pricer, Option o, double spotPrice, double timeLeft)
        {
            Option slice = o.IsCall
                ? new CallOption(spotPrice, o.K, o.r, o.q, timeLeft, o.Sigma)
                : new PutOption (spotPrice, o.K, o.r, o.q, timeLeft, o.Sigma);
            return pricer.Delta(slice);
        }

        private static RandomNumberGenerator CreateRng(ulong? seed)
        {
            return RandomNumberGenerator.Create();
        }
    }
}
