using Pricing.Core.Models;

namespace Pricing.Core.Pricing;

public interface IPricingMethod
{
    double Price(Option opt);
    double Delta(Option opt);
    double Gamma(Option opt);
    double Vega(Option opt);
    double Theta(Option opt);
    double Rho(Option opt);
}
