using Pricing.Core.Models;
using Pricing.Core.Portfolio;
using Pricing.Core.Strategies;
using Pricing.Core.Analysis;

public class BullCallSpreadTests
{
    private static BullCallSpread CreateSpread(double K1, double K2)
    {
        double S0 = 100.0;
        double r = 0.02;
        double q = 0.0;
        double T = 1.0;
        double sigma = 0.2;

        return new BullCallSpread(S0, r, q, T, sigma, K1, K2);
    }

    [Fact]
    public void BullCallSpread_CompositionOfLegs_IsCorrect()
    {
        double K1 = 100.0;
        double K2 = 120.0;

        var spread = CreateSpread(K1, K2);

        var pf = new Portfolio();
        pf.AddStrategy(spread, qty: 1);

        var legs = pf.FlattenLegs().ToArray();

        Assert.Equal(2, legs.Length);

        // Long call K1
        var longLeg = legs.Single(l => l.Qty == 1);
        var longCall = Assert.IsType<CallOption>(longLeg.Opt);
        Assert.Equal(K1, longCall.K, 10);

        // Short call K2
        var shortLeg = legs.Single(l => l.Qty == -1);
        var shortCall = Assert.IsType<CallOption>(shortLeg.Opt);
        Assert.Equal(K2, shortCall.K, 10);
    }

    [Fact]
    public void BullCallSpread_Payoff_HasExpectedShape()
    {
        double K1 = 100.0;
        double K2 = 120.0;

        var spread = CreateSpread(K1, K2);

        var pf = new Portfolio();
        pf.AddStrategy(spread, qty: 1);

        var legs = pf.FlattenLegs();

        // Théorie pour un bull call spread (long K1, short K2, K2 > K1) :
        // S_T <= K1      -> 0
        // K1 < S_T < K2  -> S_T - K1
        // S_T >= K2      -> K2 - K1

        double payoffLow    = PayoffEngine.TotalPayoff(legs, 90.0);  // <= K1
        double payoffMiddle = PayoffEngine.TotalPayoff(legs, 110.0); // entre K1 et K2
        double payoffHigh   = PayoffEngine.TotalPayoff(legs, 130.0); // >= K2

        Assert.Equal(0.0, payoffLow, 10);
        Assert.Equal(110.0 - K1, payoffMiddle, 10); // 10
        Assert.Equal(K2 - K1, payoffHigh, 10);      // 20
    }
}
