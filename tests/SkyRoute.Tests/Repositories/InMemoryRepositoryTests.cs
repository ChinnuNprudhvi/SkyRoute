using Microsoft.Extensions.DependencyInjection;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;
using SkyRoute.Infrastructure;
using SkyRoute.Infrastructure.Repositories;

namespace SkyRoute.Tests.Repositories;

public class InMemoryRepositoryTests
{
    [Fact]
    public async Task SearchResultRepository_SavesAndRetrieves_ByOpaqueId()
    {
        var repo = new InMemorySearchResultRepository(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

        var offers = new List<FlightOffer>
        {
            new("id1", "GlobalAir", "GA001",
                new Airport("JFK", "JFK Airport", "New York", "United States"),
                new Airport("LAX", "LAX Airport", "Los Angeles", "United States"),
                DateTime.Today, DateTime.Today.AddHours(6),
                CabinClass.Economy, 100m, 115m, 115m),
        };

        var searchId = await repo.SaveAsync(offers, TimeSpan.FromMinutes(10));

        Assert.False(string.IsNullOrWhiteSpace(searchId));
        Assert.DoesNotContain("JFK", searchId);

        var retrieved = await repo.GetAsync(searchId);

        Assert.NotNull(retrieved);
        Assert.Single(retrieved!);
    }

    [Fact]
    public async Task SearchResultRepository_ReturnsNull_ForUnknownId()
    {
        var repo = new InMemorySearchResultRepository(
            new Microsoft.Extensions.Caching.Memory.MemoryCache(
                new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));

        var result = await repo.GetAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task BookingRepository_SavesAndRetrieves_ByReference()
    {
        var repo = new InMemoryBookingRepository();
        var flight = new FlightOffer("id1", "GlobalAir", "GA001",
            new Airport("JFK", "JFK Airport", "New York", "United States"),
            new Airport("LAX", "LAX Airport", "Los Angeles", "United States"),
            DateTime.Today, DateTime.Today.AddHours(6),
            CabinClass.Economy, 100m, 115m, 115m);
        var booking = new Booking("REF123", flight, [], BookingStatus.Confirmed, DateTime.UtcNow);

        await repo.SaveAsync(booking);
        var retrieved = await repo.GetByReferenceAsync("REF123");

        Assert.NotNull(retrieved);
        Assert.Equal("REF123", retrieved!.Reference);
    }

    [Fact]
    public void AddSkyRouteInfrastructure_RegistersAllDependencies()
    {
        var services = new ServiceCollection();
        services.AddSkyRouteInfrastructure();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ISearchResultRepository>());
        Assert.NotNull(provider.GetService<IBookingRepository>());

        var flightProviders = provider.GetServices<IFlightProvider>().ToList();
        Assert.Equal(2, flightProviders.Count);
        Assert.Contains(flightProviders, p => p.ProviderName == "GlobalAir");
        Assert.Contains(flightProviders, p => p.ProviderName == "BudgetWings");
    }
}
