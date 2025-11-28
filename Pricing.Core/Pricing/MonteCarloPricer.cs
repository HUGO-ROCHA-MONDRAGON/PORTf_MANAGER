using Pricing.Core.Models;

namespace Pricing.Core.Pricing;

/// Pricer Monte Carlo pour options européennes en utilisant Box-Muller pour générer les chocs gaussiens
/// Pas aussi précis que BS en fermé, mais flexible pour des payoffs complexes
public sealed class MonteCarloPricer : IPricingMethod
{
    private readonly int _n;  // nb de simulations
    private readonly Random _rng;

    public MonteCarloPricer(int simulations = 50_000, int? seed = null)
    {
        if (simulations <= 0) throw new ArgumentException("Le nombre de simulations doit être > 0", nameof(simulations));
        _n = simulations;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    /// Prix via MC : on simule S_T sous la mesure risque-neutre
    /// puis on moyenne les payoffs actualisés
    public double Price(Option o)
    {
        // Paramètres du GBM risque-neutre
        double mu = (o.r - o.q - 0.5 * o.Sigma * o.Sigma) * o.T;
        double vol = o.Sigma * Math.Sqrt(o.T);

        double sumPayoffs = 0.0;

        for (int i = 0; i < _n; i++)
        {
            // Box-Muller pour avoir Z suit  N(0,1)
            double u1 = _rng.NextDouble();
            double u2 = _rng.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);

            // Prix terminal
            double ST = o.S0 * Math.Exp(mu + vol * z);

            // Payoff Call ou Put
            double payoff = o.IsCall ? Math.Max(ST - o.K, 0.0) : Math.Max(o.K - ST, 0.0);

            sumPayoffs += payoff;
        }

        // Actualisation + moyenne
        return Math.Exp(-o.r * o.T) * (sumPayoffs / _n);
    }

    // Delta par différence finie centrée
    // Pas super précis mais ça marche
    public double Delta(Option o)
    {
        double dS = o.S0 * 0.01;
        var oUp = CloneWithSpot(o, o.S0 + dS);
        var oDown = CloneWithSpot(o, o.S0 - dS);
        return (Price(oUp) - Price(oDown)) / (2.0 * dS);
    }

    // Gamma par différence finie d'ordre 2
    public double Gamma(Option o)
    {
        double dS = o.S0 * 0.01;
        var oUp = CloneWithSpot(o, o.S0 + dS);
        var oDown = CloneWithSpot(o, o.S0 - dS);
        double pUp = Price(oUp);
        double p0 = Price(o);
        double pDown = Price(oDown);
        return (pUp - 2.0 * p0 + pDown) / (dS * dS);
    }

    // Vega par bump de la vol
    public double Vega(Option o)
    {
        double dSigma = 0.01;
        var oUp = CloneWithVolatility(o, o.Sigma + dSigma);

        return (Price(oUp) - Price(o)) / dSigma;
    }

    /// Theta :Approximé par -(P(T - ΔT) - P(T)) / ΔT.
    public double Theta(Option o)
    {
        double dT = 1.0 / 365.0; // bump d'un jour
        if (o.T <= dT) return 0.0; // éviter T négatif
        var oDown = CloneWithMaturity(o, o.T - dT);
        return -(Price(oDown) - Price(o)) / dT;
    }

    /// Rho : Approximé par (P(r + Δr) - P(r)) / Δr.
    public double Rho(Option o)
    {
        double dr = 0.01; // bump de 1% absolu
        var oUp = CloneWithRate(o, o.r + dr);
        return (Price(oUp) - Price(o)) / dr;
    }

    //  Méthodes utilitaires pour cloner une option avec un paramètre modifié 

    /// Clone l'option en modifiant S0
    private static Option CloneWithSpot(Option o, double newS0)
    {
        return o.IsCall
            ? new CallOption(newS0, o.K, o.r, o.q, o.T, o.Sigma)
            : new PutOption(newS0, o.K, o.r, o.q, o.T, o.Sigma);
    }

    /// Clone l'option en modifiant la vol
    private static Option CloneWithVolatility(Option o, double newSigma)
    {
        return o.IsCall
            ? new CallOption(o.S0, o.K, o.r, o.q, o.T, newSigma)
            : new PutOption(o.S0, o.K, o.r, o.q, o.T, newSigma);
    }

    /// Clone l'option en modifiant la maturité T
    private static Option CloneWithMaturity(Option o, double newT)
    {
        return o.IsCall
            ? new CallOption(o.S0, o.K, o.r, o.q, newT, o.Sigma)
            : new PutOption(o.S0, o.K, o.r, o.q, newT, o.Sigma);
    }

    /// Clone l'option en modifiant le taux sans risque r
    private static Option CloneWithRate(Option o, double newR)
    {
        return o.IsCall
            ? new CallOption(o.S0, o.K, newR, o.q, o.T, o.Sigma)
            : new PutOption(o.S0, o.K, newR, o.q, o.T, o.Sigma);
    }
}
