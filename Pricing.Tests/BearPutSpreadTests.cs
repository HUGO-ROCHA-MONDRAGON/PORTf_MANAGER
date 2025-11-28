using Pricing.Core.Models;
using Pricing.Core.Portfolio;
using Pricing.Core.Strategies;
using Pricing.Core.Analysis;


public class BearPutSpreadTests
{
    private static BearPutSpread CreateSpread(double K1, double K2)
    {
        double S0 = 100.0;
        double r = 0.02;
        double q = 0.0;
        double T = 1.0;
        double sigma = 0.2;

        return new BearPutSpread(S0, r, q, T, sigma, K1, K2);
    }

    [Fact]
    public void BearPutSpread_CompositionOfLegs_IsCorrect()
    {
        double K1 = 90.0;
        double K2 = 110.0;

        var spread = CreateSpread(K1, K2);

        // On passe par le Portfolio pour aplatir les jambes
        var pf = new Portfolio();
        pf.AddStrategy(spread, qty: 1);

        var legs = pf.FlattenLegs().ToArray();

        Assert.Equal(2, legs.Length);

        // Long put strike K2
        var longLeg = legs.Single(l => l.Qty == 1);
        var longPut = Assert.IsType<PutOption>(longLeg.Opt);
        Assert.Equal(K2, longPut.K, 10);

        // Short put strike K1
        var shortLeg = legs.Single(l => l.Qty == -1);
        var shortPut = Assert.IsType<PutOption>(shortLeg.Opt);
        Assert.Equal(K1, shortPut.K, 10);
    }

    [Fact]
    public void BearPutSpread_Payoff_HasExpectedShape()
    {
        double K1 = 90.0;
        double K2 = 110.0;

        var spread = CreateSpread(K1, K2);

        var pf = new Portfolio();
        pf.AddStrategy(spread, qty: 1);

        var legs = pf.FlattenLegs();

        // Rappels théoriques pour un bear put spread (long K2, short K1, K2 > K1) :
        // S_T >= K2      -> payoff = 0
        // K1 < S_T < K2  -> payoff = K2 - S_T
        // S_T <= K1      -> payoff = K2 - K1 (constante)

        double payoffHigh = PayoffEngine.TotalPayoff(legs, 120.0); // S_T > K2
        double payoffMid  = PayoffEngine.TotalPayoff(legs, 100.0); // K1 < S_T < K2
        double payoffLow  = PayoffEngine.TotalPayoff(legs, 80.0);  // S_T < K1

        Assert.Equal(0.0, payoffHigh, 10);
        Assert.Equal(K2 - 100.0, payoffMid, 10);   // 110 - 100 = 10
        Assert.Equal(K2 - K1, payoffLow, 10);      // 110 - 90 = 20
    }
}
