using Pricing.Core.Models;
using Pricing.Core.Portfolio;
using Pricing.Core.Strategies;
using Pricing.Core.Pricing;
public class CalendarSpreadTests
{
    private static CalendarSpread CreateSpread(double T_short, double T_long, double K)
    {
        double S0 = 100.0;
        double r = 0.02;
        double q = 0.0;
        double sigma = 0.2;

        return new CalendarSpread(S0, r, q, T_short, T_long, sigma, K);
    }

    [Fact]
    public void CalendarSpread_CompositionOfLegs_IsCorrect()
    {
        double K = 100.0;
        double T_short = 0.5;
        double T_long  = 1.0;

        var spread = CreateSpread(T_short, T_long, K);

        var pf = new Portfolio();
        pf.AddStrategy(spread, qty: 1);

        var legs = pf.FlattenLegs().ToArray();
        Assert.Equal(2, legs.Length);

        // Long call T_long
        var longLeg = Assert.Single(legs, l => l.Qty == 1);
        var longCall = Assert.IsType<CallOption>(longLeg.Opt);
        Assert.Equal(K, longCall.K, 10);
        Assert.Equal(T_long, longCall.T, 10);

        // Short call T_short
        var shortLeg = Assert.Single(legs, l => l.Qty == -1);
        var shortCall = Assert.IsType<CallOption>(shortLeg.Opt);
        Assert.Equal(K, shortCall.K, 10);
        Assert.Equal(T_short, shortCall.T, 10);
    }

    [Fact]
    public void CalendarSpread_InitialPrice_IsPositive()
    {
        double K = 100.0;
        double T_short = 0.5;
        double T_long  = 1.0;

        var spread = CreateSpread(T_short, T_long, K);

        var pf = new Portfolio();
        pf.AddStrategy(spread, qty: 1);

        var pricer = new BlackScholesPricer();
        double price = pf.Price(pricer);

        Assert.True(price > 0.0);
    }
}
