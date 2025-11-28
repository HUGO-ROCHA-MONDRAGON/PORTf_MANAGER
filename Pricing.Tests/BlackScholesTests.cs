using Pricing.Core.Models;
using Pricing.Core.Pricing;

public class BlackScholesTests
{
    [Fact]
    public void PutCallParity_HoldsApproximately()
    {
        var call = new CallOption(100,100,0.02,0.00,1.0,0.20);
        var put  = new PutOption (100,100,0.02,0.00,1.0,0.20);
        var bs = new BlackScholesPricer();

        double lhs = bs.Price(call) + 100 * System.Math.Exp(-0.02 * 1.0);
        double rhs = bs.Price(put)  + 100 * System.Math.Exp(-0.00 * 1.0);
        Assert.True(System.Math.Abs(lhs - rhs) < 1e-6);
    }

    [Fact]
    public void Prices_ArePositive_ForCallAndPut()
    {
        var call = new CallOption(100,100,0.02,0.00,1.0,0.20);
        var put  = new PutOption (100,100,0.02,0.00,1.0,0.20);
        var bs = new BlackScholesPricer();

        double c = bs.Price(call);
        double p = bs.Price(put);

        Assert.True(c > 0.0);
        Assert.True(p > 0.0);
    }

    [Fact]
    public void Delta_IsInTheoreticalBounds()
    {
        var call = new CallOption(100,100,0.02,0.00,1.0,0.20);
        var put  = new PutOption (100,100,0.02,0.00,1.0,0.20);
        var bs = new BlackScholesPricer();

        double deltaCall = bs.Delta(call);
        double deltaPut  = bs.Delta(put);

        // q = 0 donc e^{-qT} = 1
        Assert.InRange(deltaCall, 0.0, 1.0);
        Assert.InRange(deltaPut, -1.0, 0.0);
    }

    [Fact]
    public void Gamma_IsPositive_ForCallAndPut()
    {
        var call = new CallOption(100,100,0.02,0.00,1.0,0.20);
        var put  = new PutOption (100,100,0.02,0.00,1.0,0.20);
        var bs = new BlackScholesPricer();

        double gammaCall = bs.Gamma(call);
        double gammaPut  = bs.Gamma(put);

        Assert.True(gammaCall > 0.0);
        Assert.True(gammaPut > 0.0);
    }

    [Fact]
    public void Vega_IsPositive_ForCallAndPut()
    {
        var call = new CallOption(100,100,0.02,0.00,1.0,0.20);
        var put  = new PutOption (100,100,0.02,0.00,1.0,0.20);
        var bs = new BlackScholesPricer();

        double vegaCall = bs.Vega(call);
        double vegaPut  = bs.Vega(put);

        Assert.True(vegaCall > 0.0);
        Assert.True(vegaPut > 0.0);
    }

    [Fact]
    public void Rho_HasCorrectSign_ForCallAndPut()
    {
        var call = new CallOption(100,100,0.02,0.00,1.0,0.20);
        var put  = new PutOption (100,100,0.02,0.00,1.0,0.20);
        var bs = new BlackScholesPricer();

        double rhoCall = bs.Rho(call);
        double rhoPut  = bs.Rho(put);

        Assert.True(rhoCall > 0.0); // call: plus le taux est élevé, plus l’option vaut cher
        Assert.True(rhoPut < 0.0);  // put: plus le taux est élevé, moins l’option vaut cher
    }
}

