using SkyRoute.Application.Interfaces;
using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Tests.Pricing;

public class BudgetWingsPricingStrategyTests
{
    private readonly IPricingStrategy _strategy = new BudgetWingsPricingStrategy();

    [Fact]
    public void CalculatePrice_NormalCase_AppliesTenPercentDiscount()
    {
        var result = _strategy.CalculatePrice(100m);

        Assert.Equal(90.00m, result);
    }

    [Fact]
    public void CalculatePrice_BelowFloor_ReturnsFloorValue()
    {
        // 20m * 0.90 = 18.00, which is below the 29.99 floor.
        var result = _strategy.CalculatePrice(20m);

        Assert.Equal(29.99m, result);
    }
}
