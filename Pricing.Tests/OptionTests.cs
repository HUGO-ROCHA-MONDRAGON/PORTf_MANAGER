using Pricing.Core.Models;

public class OptionTests
{
    [Fact]
    public void CallOption_Properties_AreInitializedCorrectly()
    {
        double s0 = 100.0;
        double k  = 110.0;
        double r  = 0.02;
        double q  = 0.01;
        double T  = 1.5;
        double sigma = 0.25;

        var call = new CallOption(s0, k, r, q, T, sigma);

        Assert.Equal(s0, call.S0);
        Assert.Equal(k,  call.K);
        Assert.Equal(r,  call.r);
        Assert.Equal(q,  call.q);
        Assert.Equal(T,  call.T);
        Assert.Equal(sigma, call.Sigma);

        Assert.True(call.IsCall);
    }

    [Fact]
    public void PutOption_Properties_AreInitializedCorrectly()
    {
        double s0 = 100.0;
        double k  = 90.0;
        double r  = 0.02;
        double q  = 0.0;
        double T  = 0.5;
        double sigma = 0.3;

        var put = new PutOption(s0, k, r, q, T, sigma);

        Assert.Equal(s0, put.S0);
        Assert.Equal(k,  put.K);
        Assert.Equal(r,  put.r);
        Assert.Equal(q,  put.q);
        Assert.Equal(T,  put.T);
        Assert.Equal(sigma, put.Sigma);

        Assert.False(put.IsCall);
    }

    [Fact]
    public void YearsFrom_ReturnsApproxOneYearFor365Days()
    {
        var today = new DateTime(2025, 1, 1);
        var maturity = today.AddDays(365);

        double years = Option.YearsFrom(today, maturity);

        Assert.InRange(years, 0.99, 1.01);
    }

    [Fact]
    public void YearsFrom_ReturnsZeroWhenMaturityIsInThePast()
    {
        var today = new DateTime(2025, 1, 1);
        var maturityPast = today.AddDays(-30);

        double years = Option.YearsFrom(today, maturityPast);

        Assert.Equal(0.0, years);
    }
}
