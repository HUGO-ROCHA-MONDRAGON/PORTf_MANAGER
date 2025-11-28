#nullable disable
using Pricing.CLI.Market;

public class BasketBuilderTests
{
    [Fact]
    public async Task BuildWeightedAsync_Throws_WhenSymbolsNullOrEmpty()
    {
        // symbols = null
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BasketBuilder.BuildWeightedAsync(null, new double[] { 1.0 }));

        // symbols = tableau vide
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BasketBuilder.BuildWeightedAsync(Array.Empty<string>(), Array.Empty<double>()));
    }

    [Fact]
    public async Task BuildWeightedAsync_Throws_WhenWeightsNull()
    {
        var symbols = new[] { "AAPL", "MSFT" };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BasketBuilder.BuildWeightedAsync(symbols, null));
    }

    [Fact]
    public async Task BuildWeightedAsync_Throws_WhenWeightsLengthDiffersFromSymbols()
    {
        var symbols = new[] { "AAPL", "MSFT" };
        var weights = new[] { 0.5 }; // longueur 1 au lieu de 2

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BasketBuilder.BuildWeightedAsync(symbols, weights));
    }

    [Fact]
    public async Task BuildWeightedAsync_Throws_WhenTooManySymbols()
    {
        var symbols = new[] { "AAPL", "MSFT", "GOOG", "AMZN", "TSLA" }; // 5 symboles
        var weights = new[] { 0.2, 0.2, 0.2, 0.2, 0.2 };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BasketBuilder.BuildWeightedAsync(symbols, weights));
    }

    [Fact]
    public async Task BuildWeightedAsync_Throws_WhenSumOfWeightsIsNonPositive()
    {
        var symbols = new[] { "AAPL", "MSFT" };

        // somme = 0
        var wZero = new[] { 0.0, 0.0 };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BasketBuilder.BuildWeightedAsync(symbols, wZero));

        // somme négative
        var wNegative = new[] { -1.0, 0.0 };

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await BasketBuilder.BuildWeightedAsync(symbols, wNegative));
    }
}

#nullable restore
