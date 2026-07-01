using SkyRoute.Application.Interfaces;

namespace SkyRoute.Infrastructure.Pricing;

public class BudgetWingsPricingStrategy : IPricingStrategy
{
    public decimal CalculatePrice(decimal baseFare)
    {
        return Math.Max(baseFare * 0.90m, 29.99m);
    }
}
