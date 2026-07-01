using SkyRoute.Infrastructure.Pricing;

namespace SkyRoute.Tests.Pricing;

public class GlobalAirPricingStrategyTests
{
    private readonly GlobalAirPricingStrategy _strategy = new();

    [Fact]
    public void CalculatePrice_NormalCase_AppliesFifteenPercentMarkup()
    {
        var result = _strategy.CalculatePrice(100m);

        Assert.Equal(115.00m, result);
    }

    [Fact]
    public void CalculatePrice_HalfCentBoundary_RoundsAwayFromZero()
    {
        // 0.30m * 1.15 = 0.345m exactly, a half-cent boundary between 0.34 and 0.35.
        // MidpointRounding.AwayFromZero must yield 0.35, whereas the default
        // banker's rounding (ToEven) would yield 0.34 since 4 is even.
        var result = _strategy.CalculatePrice(0.30m);

        Assert.Equal(0.35m, result);
    }
}
