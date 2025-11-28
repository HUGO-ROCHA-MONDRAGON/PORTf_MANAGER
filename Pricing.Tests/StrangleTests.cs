using System.Linq;
using Pricing.Core.Models;
using Pricing.Core.Portfolio;
using Pricing.Core.Strategies;
using Pricing.Core.Analysis;
using Xunit;

public class StrangleTests
{
    private static Strangle CreateStrangle(double K1, double K2)
    {
        double S0 = 100.0;
        double r = 0.02;
        double q = 0.0;
        double T = 1.0;
        double sigma = 0.2;

        return new Strangle(S0, r, q, T, sigma, K1, K2);
    }

    [Fact]
    public void Strangle_CompositionOfLegs_IsCorrect()
    {
        double K1 = 90.0;
        double K2 = 110.0;

        var strat = CreateStrangle(K1, K2);

        var pf = new Portfolio();
        pf.AddStrategy(strat, qty: 1);

        var legs = pf.FlattenLegs().ToArray();
        Assert.Equal(2, legs.Length);

        // Put leg (K1)
        var putLeg = Assert.Single(legs, l => l.Opt is PutOption);
        var put = Assert.IsType<PutOption>(putLeg.Opt);
        Assert.Equal(1, putLeg.Qty);
        Assert.Equal(K1, put.K, 10);

        // Call leg (K2)
        var callLeg = Assert.Single(legs, l => l.Opt is CallOption);
        var call = Assert.IsType<CallOption>(callLeg.Opt);
        Assert.Equal(1, callLeg.Qty);
        Assert.Equal(K2, call.K, 10);
    }

    [Fact]
    public void Strangle_Payoff_HasExpectedShape()
    {
        double K1 = 90.0;
        double K2 = 110.0;

        var strat = CreateStrangle(K1, K2);

        var pf = new Portfolio();
        pf.AddStrategy(strat, qty: 1);

        var legs = pf.FlattenLegs();

        double ST_low    = 70.0;   // très en dessous de K1
        double ST_middle = 100.0;  // entre K1 et K2
        double ST_high   = 130.0;  // très au dessus de K2

        double payoffLow    = PayoffEngine.TotalPayoff(legs, ST_low);
        double payoffMiddle = PayoffEngine.TotalPayoff(legs, ST_middle);
        double payoffHigh   = PayoffEngine.TotalPayoff(legs, ST_high);

        // En dessous de K1 : payoff = K1 - ST
        Assert.Equal(K1 - ST_low, payoffLow, 10);

        // Entre K1 et K2 : payoff = 0
        Assert.Equal(0.0, payoffMiddle, 10);

        // Au-dessus de K2 : payoff = ST - K2
        Assert.Equal(ST_high - K2, payoffHigh, 10);
    }
}
