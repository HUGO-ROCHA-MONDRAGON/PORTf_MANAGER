using Pricing.Core.Models;
using Pricing.Core.Portfolio;
using Pricing.Core.Strategies;
using Pricing.Core.Analysis;

public class ButterflyTests
{
    private static Butterfly CreateButterfly(double K, double dK)
    {
        double S0 = 100.0;
        double r = 0.02;
        double q = 0.0;
        double T = 1.0;
        double sigma = 0.2;

        return new Butterfly(S0, r, q, T, sigma, K, dK);
    }

    [Fact]
    public void Butterfly_CompositionOfLegs_IsCorrect()
    {
        double K = 100.0;
        double dK = 10.0;

        var strat = CreateButterfly(K, dK);

        var pf = new Portfolio();
        pf.AddStrategy(strat, qty: 1);

        var legs = pf.FlattenLegs().ToArray();

        Assert.Equal(3, legs.Length);

        // +1 Call(K - dK)
        var lowLeg = legs.Single(l => l.Qty == 1 && l.Opt is CallOption c1 && c1.K == K - dK);
        var lowCall = Assert.IsType<CallOption>(lowLeg.Opt);
        Assert.Equal(K - dK, lowCall.K, 10);

        // -2 Call(K)
        var midLeg = Assert.Single(legs, l => l.Opt is CallOption c && c.K == K);
        var midCall = Assert.IsType<CallOption>(midLeg.Opt);
        Assert.Equal(K, midCall.K, 10);
        Assert.Equal(-2, midLeg.Qty);



        // +1 Call(K + dK)
        var highLeg = legs.Single(l => l.Qty == 1 && l.Opt is CallOption c2 && c2.K == K + dK);
        var highCall = Assert.IsType<CallOption>(highLeg.Opt);
        Assert.Equal(K + dK, highCall.K, 10);
    }

    [Fact]
    public void Butterfly_Payoff_HasExpectedTentShape()
    {
        double K = 100.0;
        double dK = 10.0;

        var strat = CreateButterfly(K, dK);

        var pf = new Portfolio();
        pf.AddStrategy(strat, qty: 1);

        var legs = pf.FlattenLegs();

        // Rappels théoriques :
        // S_T <= K - dK      -> payoff = 0
        // K - dK < S_T < K   -> payoff croissant linéaire
        // S_T = K            -> payoff max = dK
        // K < S_T < K + dK   -> payoff décroissant
        // S_T >= K + dK      -> payoff = 0

        double payoffLow   = PayoffEngine.TotalPayoff(legs, 80.0);   // < K - dK (90)
        double payoffMid   = PayoffEngine.TotalPayoff(legs, 95.0);   // entre 90 et 100
        double payoffPeak  = PayoffEngine.TotalPayoff(legs, 100.0);  // au sommet K
        double payoffHigh  = PayoffEngine.TotalPayoff(legs, 130.0);  // > K + dK (110)

        Assert.Equal(0.0, payoffLow, 10);
        Assert.True(payoffMid > 0.0);
        Assert.Equal(dK, payoffPeak, 10);   // max = dK
        Assert.Equal(0.0, payoffHigh, 10);
    }
}
