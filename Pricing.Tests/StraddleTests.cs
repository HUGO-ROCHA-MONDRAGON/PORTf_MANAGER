using System.Linq;
using Pricing.Core.Models;
using Pricing.Core.Portfolio;
using Pricing.Core.Strategies;
using Pricing.Core.Analysis;
using Xunit;

public class StraddleTests
{
    private static Straddle CreateStraddle(double K)
    {
        double S0 = 100.0;
        double r = 0.02;
        double q = 0.0;
        double T = 1.0;
        double sigma = 0.2;

        return new Straddle(S0, r, q, T, sigma, K);
    }

    [Fact]
    public void Straddle_CompositionOfLegs_IsCorrect()
    {
        double K = 100.0;

        var strat = CreateStraddle(K);

        var pf = new Portfolio();
        pf.AddStrategy(strat, qty: 1);

        var legs = pf.FlattenLegs().ToArray();
        Assert.Equal(2, legs.Length);

        // Call leg
        var callLeg = Assert.Single(legs, l => l.Opt is CallOption);
        var call = Assert.IsType<CallOption>(callLeg.Opt);
        Assert.Equal(1, callLeg.Qty);
        Assert.Equal(K, call.K, 10);

        // Put leg
        var putLeg = Assert.Single(legs, l => l.Opt is PutOption);
        var put = Assert.IsType<PutOption>(putLeg.Opt);
        Assert.Equal(1, putLeg.Qty);
        Assert.Equal(K, put.K, 10);
    }

    [Fact]
    public void Straddle_Payoff_IsAbsoluteValueOfSTMinusK()
    {
        double K = 100.0;

        var strat = CreateStraddle(K);

        var pf = new Portfolio();
        pf.AddStrategy(strat, qty: 1);

        var legs = pf.FlattenLegs();

        double ST_low  = 80.0;
        double ST_atK  = 100.0;
        double ST_high = 120.0;

        double payoffLow  = PayoffEngine.TotalPayoff(legs, ST_low);
        double payoffAtK  = PayoffEngine.TotalPayoff(legs, ST_atK);
        double payoffHigh = PayoffEngine.TotalPayoff(legs, ST_high);

        Assert.Equal(System.Math.Abs(ST_low - K), payoffLow, 10);   // 20
        Assert.Equal(0.0, payoffAtK, 10);
        Assert.Equal(System.Math.Abs(ST_high - K), payoffHigh, 10); // 20
    }
}
