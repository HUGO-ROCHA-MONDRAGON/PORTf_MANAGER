using Pricing.Core.Models;
using Pricing.Core.Pricing;

public class MonteCarloPricerTests
{
    private static CallOption CreateCall()
        => new CallOption(100.0, 100.0, 0.02, 0.0, 1.0, 0.2);

    private static PutOption CreatePut()
        => new PutOption(100.0, 100.0, 0.02, 0.0, 1.0, 0.2);

    [Fact]
    public void Price_ReturnsNonNegativeFiniteValue_ForCallAndPut()
    {
        var call = CreateCall();
        var put  = CreatePut();

        // On peut réduire un peu le nombre de simulations pour les tests unitaires
        var mc = new MonteCarloPricer(simulations: 10_000);

        double c = mc.Price(call);
        double p = mc.Price(put);

        Assert.True(c >= 0.0);
        Assert.True(p >= 0.0);

        Assert.False(double.IsNaN(c));
        Assert.False(double.IsInfinity(c));

        Assert.False(double.IsNaN(p));
        Assert.False(double.IsInfinity(p));
    }

    [Fact]
    public void Greeks_ReturnFiniteValues()
    {
        var opt = CreateCall();
        // Utiliser une graine fixe pour reproductibilité et nombre suffisant de simulations
        var mc = new MonteCarloPricer(simulations: 10_000, seed: 42);

        double delta = mc.Delta(opt);
        double gamma = mc.Gamma(opt);
        double vega = mc.Vega(opt);
        double theta = mc.Theta(opt);
        double rho = mc.Rho(opt);

        // Les grecques doivent être finies (pas NaN, pas Infinity)
        // C'est le critère principal pour Monte Carlo
        Assert.False(double.IsNaN(delta), $"Delta is NaN");
        Assert.False(double.IsInfinity(delta), $"Delta is Infinity");

        Assert.False(double.IsNaN(gamma), $"Gamma is NaN");
        Assert.False(double.IsInfinity(gamma), $"Gamma is Infinity");

        Assert.False(double.IsNaN(vega), $"Vega is NaN");
        Assert.False(double.IsInfinity(vega), $"Vega is Infinity");

        Assert.False(double.IsNaN(theta), $"Theta is NaN");
        Assert.False(double.IsInfinity(theta), $"Theta is Infinity");

        Assert.False(double.IsNaN(rho), $"Rho is NaN");
        Assert.False(double.IsInfinity(rho), $"Rho is Infinity");

        // Vérification basique de signe pour delta (le plus stable)
        // Delta d'un call ATM doit être entre 0 et 1
        Assert.InRange(delta, 0.0, 1.0);
    }



}
