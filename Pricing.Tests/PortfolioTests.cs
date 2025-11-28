using Pricing.Core.Models;
using Pricing.Core.Pricing;
using Pricing.Core.Portfolio;

public class PortfolioTests
{
    private static CallOption CreateCall(double k = 100.0)
        => new CallOption(100.0, k, 0.02, 0.0, 1.0, 0.2);

    private static PutOption CreatePut(double k = 100.0)
        => new PutOption(100.0, k, 0.02, 0.0, 1.0, 0.2);

    [Fact]
    public void AddOption_WithZeroQuantity_DoesNotAddLine()
    {
        var portfolio = new Portfolio();
        var call = CreateCall();

        portfolio.AddOption(call, qty: 0);

        Assert.Empty(portfolio.OptionLines);
    }

    [Fact]
    public void AddOption_AddsLine()
    {
        var portfolio = new Portfolio();
        var call = CreateCall();

        portfolio.AddOption(call, qty: 2);

        // Ligne d’option
        Assert.Single(portfolio.OptionLines);
        var line = portfolio.OptionLines[0];
        Assert.Same(call, line.Opt);
        Assert.Equal(2, line.Qty);

        
    }

    [Fact]
    public void FlattenLegs_ReturnsOptionLinesWhenNoStrategies()
    {
        var portfolio = new Portfolio();
        var call = CreateCall(100.0);
        var put = CreatePut(90.0);

        portfolio.AddOption(call, qty: 2);
        portfolio.AddOption(put, qty: -1);

        var legs = portfolio.FlattenLegs().ToArray();

        Assert.Equal(2, legs.Length);

        Assert.Contains(legs, l => ReferenceEquals(l.Opt, call) && l.Qty == 2);
        Assert.Contains(legs, l => ReferenceEquals(l.Opt, put) && l.Qty == -1);
    }

    [Fact]
    public void Price_AggregatesOptionPricesCorrectly()
    {
        var portfolio = new Portfolio();
        var pricer = new BlackScholesPricer();

        var call = CreateCall(100.0);
        var put = CreatePut(90.0);

        portfolio.AddOption(call, qty: 2);
        portfolio.AddOption(put, qty: -1);

        double expected =
            2 * pricer.Price(call)
          + (-1) * pricer.Price(put);

        double actual = portfolio.Price(pricer);

        Assert.Equal(expected, actual, 8);
    }
}

