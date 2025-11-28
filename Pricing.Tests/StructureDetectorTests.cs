using System.Linq;
using Pricing.Core.Models;
using Pricing.Core.Strategies;
using Xunit;

public class StructureDetectorTests
{
    private const double S0    = 100.0;
    private const double r     = 0.02;
    private const double q     = 0.0;
    private const double T     = 1.0;
    private const double sigma = 0.2;

    [Fact]
    public void Detect_Straddle_FromLegs()
    {
        var strat = new Straddle(S0, r, q, T, sigma, 100.0);
        var legs = strat.GetLegs();

        var matches = StructureDetector.Detect(legs);

        Assert.Contains(matches, m => m.Name == "Straddle");
    }

    [Fact]
    public void Detect_Strangle_FromLegs()
    {
        var strat = new Strangle(S0, r, q, T, sigma, 90.0, 110.0);
        var legs = strat.GetLegs();

        var matches = StructureDetector.Detect(legs);

        Assert.Contains(matches, m => m.Name == "Strangle");
    }

    [Fact]
    public void Detect_Butterfly_FromLegs()
    {
        var strat = new Butterfly(S0, r, q, T, sigma, 100.0, 10.0);
        var legs = strat.GetLegs();

        var matches = StructureDetector.Detect(legs);

        Assert.Contains(matches, m => m.Name.StartsWith("Butterfly"));
    }

    [Fact]
    public void Detect_BullCallSpread_FromLegs()
    {
        var strat = new BullCallSpread(S0, r, q, T, sigma, 100.0, 120.0);
        var legs = strat.GetLegs();

        var matches = StructureDetector.Detect(legs);

        Assert.Contains(matches, m => m.Name == "Bull Call Spread");
    }

   [Fact]
    public void Detect_BearPutSpread_FromLegs()
    {
     double K1 = 100.0; // strike bas
        double K2 = 120.0; // strike haut

        // On construit explicitement un put short K1 (bas) + un put long K2 (haut)
        var shortPutLow  = new PutOption(S0, K1, r, q, T, sigma);
        var longPutHigh  = new PutOption(S0, K2, r, q, T, sigma);

        var legs = new (Option Opt, int Qty)[]
        {
            (shortPutLow, -1),  // p1 : short put bas
            (longPutHigh, +1)   // p2 : long put haut
        };

        var matches = StructureDetector.Detect(legs);

        Assert.Contains(matches, m => m.Name == "Bear Put Spread");
    }

    [Fact]
    public void Detect_IronCondor_FromLegs()
    {
        var strat = new IronCondor(S0, r, q, T, sigma, 80.0, 90.0, 110.0, 120.0);
        var legs = strat.GetLegs();

        var matches = StructureDetector.Detect(legs);

        Assert.Contains(matches, m => m.Name == "Iron Condor");
    }

    [Fact]
    public void Detect_CalendarSpread_FromTwoCalls()
    {
        double K = 100.0;
        double T1 = 0.5;
        double T2 = 1.0;

        var shortCall = new CallOption(S0, K, r, q, T1, sigma);
        var longCall  = new CallOption(S0, K, r, q, T2, sigma);

        var legs = new (Option Opt, int Qty)[]
        {
            (shortCall, -1),
            (longCall,  +1)
        };

        var matches = StructureDetector.Detect(legs);

        Assert.Contains(matches, m => m.Name == "Calendar Spread");
    }
}

