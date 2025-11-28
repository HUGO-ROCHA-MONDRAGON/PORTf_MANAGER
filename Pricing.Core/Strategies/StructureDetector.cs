using Pricing.Core.Models;

namespace Pricing.Core.Strategies
{
    public sealed class StructureMatch
    {
        public string Name { get; init; } = "";
        public string Objective { get; init; } = "";
        public string Detail { get; init; } = "";
    }

    /// Détecteur de structures connues dans un portefeuille custom.
    /// Si l'utilisateur construit manuellement une position, on essaie de reconnaître
    /// si ça correspond à une stratégie classique (Straddle, Butterfly, etc.).
    public static class StructureDetector
    {
        public static List<StructureMatch> Detect(IEnumerable<(Option Opt, int Qty)> legs)
        {
            var res = new List<StructureMatch>();
            var L = legs.ToList();

            // On test toutes les structures connues
            DetectStraddle(L, res);
            DetectStrangle(L, res);
            DetectButterfly(L, res);
            DetectBullSpread(L, res);
            DetectBearSpread(L, res);
            DetectIronCondor(L, res);
            DetectCalendar(L, res);

            return res;
        }

        // Straddle: +1 Call(K) +1 Put(K)
        static void DetectStraddle(List<(Option Opt, int Qty)> L, List<StructureMatch> res)
        {
            var calls = L.Where(x => x.Opt.IsCall && x.Qty > 0).ToList();
            var puts  = L.Where(x => !x.Opt.IsCall && x.Qty > 0).ToList();

            foreach (var c in calls)
            foreach (var p in puts)
            {
                // Check si même strike et même maturité (tolérance numérique 1e-9)
                if (Math.Abs(c.Opt.K - p.Opt.K) < 1e-9 && Math.Abs(c.Opt.T - p.Opt.T) < 1e-9)
                {
                    res.Add(new StructureMatch
                    {
                        Name = "Straddle",
                        Objective = "Pari sur forte volatilité sans biais directionnel",
                        Detail = $"+1 Call(K={c.Opt.K}) +1 Put(K={p.Opt.K}), maturité {c.Opt.T:F3} ans"
                    });
                }
            }
        }

        // Strangle: +1 Call(K2) +1 Put(K1) avec K1 < K2
        static void DetectStrangle(List<(Option Opt, int Qty)> L, List<StructureMatch> res)
        {
            var calls = L.Where(x => x.Opt.IsCall && x.Qty > 0).ToList();
            var puts  = L.Where(x => !x.Opt.IsCall && x.Qty > 0).ToList();

            foreach (var c in calls)
            foreach (var p in puts)
            {
                // Put strike < Call strike, même maturité
                if (p.Opt.K < c.Opt.K && Math.Abs(c.Opt.T - p.Opt.T) < 1e-9)
                {
                    res.Add(new StructureMatch
                    {
                        Name = "Strangle",
                        Objective = "Pari sur forte volatilité, mais moins coûteux qu'un straddle",
                        Detail = $"K_put={p.Opt.K}, K_call={c.Opt.K}, maturité {c.Opt.T:F3} ans"
                    });
                }
            }
        }
        // Butterfly: +1 C(K-dK), -2 C(K), +1 C(K+dK)
        static void DetectButterfly(List<(Option Opt, int Qty)> L, List<StructureMatch> res)
        {
            var calls = L.Where(x => x.Opt.IsCall).ToList();

            for (int i = 0; i < calls.Count; i++)
            for (int j = 0; j < calls.Count; j++)
            for (int k = 0; k < calls.Count; k++)
            {
                var c1 = calls[i]; var cM = calls[j]; var c2 = calls[k];
                
                // Pattern : +1, -2, +1 avec K central = moyenne(K1, K2)
                if (c1.Qty == +1 && cM.Qty == -2 && c2.Qty == +1)
                {
                    if (Math.Abs(cM.Opt.K - ((c1.Opt.K + c2.Opt.K) / 2.0)) < 1e-9 &&
                        Math.Abs(c1.Opt.T - cM.Opt.T) < 1e-9 && Math.Abs(cM.Opt.T - c2.Opt.T) < 1e-9)
                    {
                        res.Add(new StructureMatch
                        {
                            Name = "Butterfly (Call)",
                            Objective = "Pari sur une stabilité autour d'un strike central",
                            Detail = $"K1={c1.Opt.K}, K={cM.Opt.K}, K2={c2.Opt.K}, T={cM.Opt.T:F3}"
                        });
                    }
                }
            }
        }

        // Bull Call Spread: +1 Call(K1) -1 Call(K2), K1 < K2
        static void DetectBullSpread(List<(Option Opt, int Qty)> L, List<StructureMatch> res)
        {
            var calls = L.Where(x => x.Opt.IsCall).ToList();

            for (int i = 0; i < calls.Count; i++)
            for (int j = 1; j < calls.Count; j++)
            {
                var c1 = calls[i]; var c2 = calls[j];
                if (c1.Qty == +1 && c2.Qty == -1 && c1.Opt.K < c2.Opt.K && Math.Abs(c1.Opt.T - c2.Opt.T) < 1e-9)
                {
                    res.Add(new StructureMatch
                    {
                        Name = "Bull Call Spread",
                        Objective = "Pari sur une hausse modérée",
                        Detail = $"K1={c1.Opt.K}, K2={c2.Opt.K}, T={c1.Opt.T:F3}"
                    });
                }
            }
        }

        // Bear Put Spread: +1 Put(K2) -1 Put(K1), K1 < K2
        static void DetectBearSpread(List<(Option Opt, int Qty)> L, List<StructureMatch> res)
        {
            var puts = L.Where(x => !x.Opt.IsCall).ToList();

            for (int i = 0; i < puts.Count; i++)
            for (int j = 1; j < puts.Count; j++)
            {
                var p1 = puts[i]; var p2 = puts[j];
                if (p2.Qty == +1 && p1.Qty == -1 && p1.Opt.K < p2.Opt.K && Math.Abs(p1.Opt.T - p2.Opt.T) < 1e-9)
                {
                    res.Add(new StructureMatch
                    {
                        Name = "Bear Put Spread",
                        Objective = "Pari sur une baisse modérée",
                        Detail = $"K1={p1.Opt.K}, K2={p2.Opt.K}, T={p1.Opt.T:F3}"
                    });
                }
            }
        }

        // Iron Condor
        static void DetectIronCondor(List<(Option Opt, int Qty)> L, List<StructureMatch> res)
        {
            var calls = L.Where(x => x.Opt.IsCall).ToList();
            var puts  = L.Where(x => !x.Opt.IsCall).ToList();

            // K1 < K2 < K3 < K4
            foreach (var p1 in puts.Where(x => x.Qty == +1))
            foreach (var p2 in puts.Where(x => x.Qty == -1))
            foreach (var c1 in calls.Where(x => x.Qty == -1))
            foreach (var c2 in calls.Where(x => x.Qty == +1))
            {
                if (p1.Opt.K < p2.Opt.K && p2.Opt.K < c1.Opt.K && c1.Opt.K < c2.Opt.K &&
                    Math.Abs(p1.Opt.T - p2.Opt.T) < 1e-9 &&
                    Math.Abs(p2.Opt.T - c1.Opt.T) < 1e-9 &&
                    Math.Abs(c1.Opt.T - c2.Opt.T) < 1e-9)
                {
                    res.Add(new StructureMatch
                    {
                        Name = "Iron Condor",
                        Objective = "Stratégie neutre en volatilité avec profits limités mais probabilité de gain élevée",
                        Detail = $"K1={p1.Opt.K} < K2={p2.Opt.K} < K3={c1.Opt.K} < K4={c2.Opt.K}, T={p1.Opt.T:F3}"
                    });
                }
            }
        }

        // Calendar Spread: -1 Call(T1) +1 Call(T2), même strike
        static void DetectCalendar(List<(Option Opt, int Qty)> L, List<StructureMatch> res)
        {
            var calls = L.Where(x => x.Opt.IsCall).ToList();
            for (int i = 0; i < calls.Count; i++)
            for (int j = i + 1; j < calls.Count; j++)
            {
                var a = calls[i];
                var b = calls[j];

                if (Math.Abs(a.Opt.K - b.Opt.K) < 1e-9 && a.Opt.T != b.Opt.T)
                {
                    if (a.Qty == -1 && b.Qty == +1)
                        res.Add(new StructureMatch
                        {
                            Name = "Calendar Spread",
                            Objective = "Pari sur évolution de la volatilité implicite entre maturités",
                            Detail = $"K={a.Opt.K}, T1={Math.Min(a.Opt.T,b.Opt.T):F3}, T2={Math.Max(a.Opt.T,b.Opt.T):F3}"
                        });
                }
            }
        }
    }
}
