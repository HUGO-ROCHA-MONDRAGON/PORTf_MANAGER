using Pricing.Core.Analysis;
using Pricing.Core.Models;

public class PayoffEngineTests
{
    [Fact]
    public void GenerateGrid_RespectsBoundsAndCount()
    {
        double s0 = 100.0;
        double minPct = 0.5;   // 50
        double maxPct = 1.5;   // 150
        int points = 11;

        double[] grid = PayoffEngine.GenerateGrid(s0, minPct, maxPct, points);

        Assert.Equal(points, grid.Length);
        Assert.Equal(50.0, grid.First(), 10);
        Assert.Equal(150.0, grid.Last(), 10);

        double step = grid[1] - grid[0];
        for (int i = 1; i < grid.Length; i++)
            Assert.Equal(step, grid[i] - grid[i - 1], 10);
    }

    [Fact]
    public void GenerateGrid_EnforcesMinimumPoints()
    {
        double s0 = 100.0;
        int points = 1; // < 2

        double[] grid = PayoffEngine.GenerateGrid(s0, 0.8, 1.2, points);

        Assert.Equal(2, grid.Length);
        Assert.True(grid[1] > grid[0]);
    }

    [Fact]
    public void LegPayoff_CallOption_WorksAsExpected()
    {
        // new CallOption(s0, K, r, q, T, sigma)
        var call = new CallOption(100.0, 100.0, 0.02, 0.0, 1.0, 0.2);

        double payoffInTheMoney = PayoffEngine.LegPayoff(call, 120.0);
        double payoffOutOfTheMoney = PayoffEngine.LegPayoff(call, 80.0);

        Assert.Equal(20.0, payoffInTheMoney, 10);
        Assert.Equal(0.0, payoffOutOfTheMoney, 10);
    }

    [Fact]
    public void LegPayoff_PutOption_WorksAsExpected()
    {
        // new PutOption(s0, K, r, q, T, sigma)
        var put = new PutOption(100.0, 100.0, 0.02, 0.0, 1.0, 0.2);

        double payoffInTheMoney = PayoffEngine.LegPayoff(put, 80.0);
        double payoffOutOfTheMoney = PayoffEngine.LegPayoff(put, 120.0);

        Assert.Equal(20.0, payoffInTheMoney, 10);
        Assert.Equal(0.0, payoffOutOfTheMoney, 10);
    }

    [Fact]
    public void TotalPayoff_BullSpread_IsCorrect()
    {
        // Bull Call Spread : +1 Call K=100, -1 Call K=120
        var callLow = new CallOption(100, 100, 0.02, 0.0, 1.0, 0.2);
        var callHigh = new CallOption(100, 120, 0.02, 0.0, 1.0, 0.2);

        var legs = new (Option Opt, int Qty)[]
        {
            (callLow,  +1),
            (callHigh, -1)
        };

        double payoffBelow = PayoffEngine.TotalPayoff(legs, 90.0);   // 0
        double payoffMiddle = PayoffEngine.TotalPayoff(legs, 110.0); // 10
        double payoffAbove = PayoffEngine.TotalPayoff(legs, 130.0);  // 20 (cap = K2 - K1)

        Assert.Equal(0.0, payoffBelow, 10);
        Assert.Equal(10.0, payoffMiddle, 10);
        Assert.Equal(20.0, payoffAbove, 10);

    }

    [Fact]
    public void ExportCsv_WritesExpectedHeaderAndLines()
    {
        var call = new CallOption(100, 100, 0.02, 0.0, 1.0, 0.2);
        var legs = new (Option Opt, int Qty)[] { (call, 1) };
        double[] grid = new[] { 100.0, 120.0 };
        double premiumInitiale = 5.0;

        string filePath = Path.Combine(Path.GetTempPath(), $"payoff_test_{Guid.NewGuid()}.csv");

        try
        {
            PayoffEngine.ExportCsv(filePath, legs, grid, premiumInitiale);

            Assert.True(File.Exists(filePath));

            var lines = File.ReadAllLines(filePath);
            Assert.True(lines.Length >= 3);

            Assert.Equal("S_T;Payoff;Profit;PremiumInitiale", lines[0]);

            // Ligne pour S_T = 100 → payoff = 0, profit = -5
            Assert.Equal("100;0;-5;5", lines[1]);

            // Ligne pour S_T = 120 → payoff = 20, profit = 15
            Assert.Equal("120;20;15;5", lines[2]);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
