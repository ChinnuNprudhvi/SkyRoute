using SkyRoute.Application.Models;
using SkyRoute.Infrastructure.Pricing;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Tests.Providers;

public class ProviderSmokeTests
{
    [Fact]
    public async Task GlobalAirProvider_ReturnsOffers_ForKnownRoute()
    {
        var provider = new GlobalAirProvider(new GlobalAirPricingStrategy());
        var criteria = new SearchCriteria("JFK", "LAX", DateTime.Today.AddDays(7), 2);

        var offers = await provider.SearchAsync(criteria);

        Assert.NotEmpty(offers);
        Assert.All(offers, o =>
        {
            Assert.Equal("GlobalAir", o.Provider);
            Assert.Equal("JFK", o.Origin.Code);
            Assert.Equal("LAX", o.Destination.Code);
            Assert.True(o.TotalPrice > 0);
            Assert.Equal(o.TotalPrice / 2, o.PricePerPerson);
        });
    }

    [Fact]
    public async Task BudgetWingsProvider_ReturnsOffers_ForKnownRoute()
    {
        var provider = new BudgetWingsProvider(new BudgetWingsPricingStrategy());
        var criteria = new SearchCriteria("DEL", "BOM", DateTime.Today.AddDays(3), 1);

        var offers = await provider.SearchAsync(criteria);

        Assert.NotEmpty(offers);
        Assert.All(offers, o =>
        {
            Assert.Equal("BudgetWings", o.Provider);
            Assert.Equal("DEL", o.Origin.Code);
            Assert.Equal("BOM", o.Destination.Code);
            Assert.True(o.ArrivalTime > o.DepartureTime);
        });
    }
}
