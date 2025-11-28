using Pricing.Core.Models;
using Pricing.Core.Portfolio;
using Pricing.Core.Strategies;
using Pricing.Core.Analysis;

public class IronCondorTests
{
    private static IronCondor CreateCondor(double K1, double K2, double K3, double K4)
    {
        double S0 = 100.0;
        double r = 0.02;
        double q = 0.0;
        double T = 1.0;
        double sigma = 0.2;

        return new IronCondor(S0, r, q, T, sigma, K1, K2, K3, K4);
    }

    [Fact]
    public void IronCondor_CompositionOfLegs_IsCorrect()
    {
        double K1 = 80.0;
        double K2 = 90.0;
        double K3 = 110.0;
        double K4 = 120.0;

        var condor = CreateCondor(K1, K2, K3, K4);

        var pf = new Portfolio();
        pf.AddStrategy(condor, qty: 1);

        var legs = pf.FlattenLegs().ToArray();
        Assert.Equal(4, legs.Length);

        // +1 Put K1
        var putLong = Assert.Single(legs, l => l.Qty == 1 && l.Opt is PutOption p && p.K == K1);
        var putLongOpt = Assert.IsType<PutOption>(putLong.Opt);
        Assert.Equal(K1, putLongOpt.K, 10);

        // -1 Put K2
        var putShort = Assert.Single(legs, l => l.Qty == -1 && l.Opt is PutOption p && p.K == K2);
        var putShortOpt = Assert.IsType<PutOption>(putShort.Opt);
        Assert.Equal(K2, putShortOpt.K, 10);

        // -1 Call K3
        var callShort = Assert.Single(legs, l => l.Qty == -1 && l.Opt is CallOption c && c.K == K3);
        var callShortOpt = Assert.IsType<CallOption>(callShort.Opt);
        Assert.Equal(K3, callShortOpt.K, 10);

        // +1 Call K4
        var callLong = Assert.Single(legs, l => l.Qty == 1 && l.Opt is CallOption c && c.K == K4);
        var callLongOpt = Assert.IsType<CallOption>(callLong.Opt);
        Assert.Equal(K4, callLongOpt.K, 10);
    }

    [Fact]
    public void IronCondor_Payoff_HasExpectedShape()
    {
        double K1 = 80.0;
        double K2 = 90.0;
        double K3 = 110.0;
        double K4 = 120.0;

        var condor = CreateCondor(K1, K2, K3, K4);

        var pf = new Portfolio();
        pf.AddStrategy(condor, qty: 1);

        var legs = pf.FlattenLegs();

        // Rappels pour ce short iron condor :
        // S_T << K1       -> payoff = K1 - K2  (constante négative)
        // K2 < S_T < K3   -> payoff = 0
        // S_T >> K4       -> payoff = K3 - K4  (constante négative)

        double payoffLeft  = PayoffEngine.TotalPayoff(legs, 50.0);   // très en-dessous de K1
        double payoffMid   = PayoffEngine.TotalPayoff(legs, 100.0);  // entre K2 et K3
        double payoffRight = PayoffEngine.TotalPayoff(legs, 150.0);  // très au-dessus de K4

        Assert.Equal(K1 - K2, payoffLeft, 10);  // 80 - 90 = -10
        Assert.Equal(0.0, payoffMid, 10);
        Assert.Equal(K3 - K4, payoffRight, 10); // 110 - 120 = -10
    }
}
