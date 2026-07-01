using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public class GlobalAirPricingStrategy : IPricingStrategy
{
    public decimal CalculatePrice(decimal baseFare)
    {
        return Math.Round(baseFare * 1.15m, 2, MidpointRounding.AwayFromZero);
    }
}
