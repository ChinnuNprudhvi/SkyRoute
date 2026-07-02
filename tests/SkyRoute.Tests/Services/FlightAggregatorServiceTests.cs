using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Application.Services;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;

namespace SkyRoute.Tests.Services;

public class FlightAggregatorServiceTests
{
    private static Airport Jfk => new("JFK", "JFK Airport", "New York", "United States");
    private static Airport Lhr => new("LHR", "Heathrow", "London", "United Kingdom");
    private static Airport Lax => new("LAX", "LAX Airport", "Los Angeles", "United States");

    private static FlightOffer MakeOffer(string id, string provider, Airport origin, Airport destination) =>
        new(id, provider, "F1", origin, destination,
            DateTime.Today, DateTime.Today.AddHours(3),
            CabinClass.Economy, 100m, 115m, 115m);

    [Fact]
    public async Task SearchAsync_MergesResultsFromAllHealthyProviders()
    {
        var providerA = new Mock<IFlightProvider>();
        providerA.SetupGet(p => p.ProviderName).Returns("A");
        providerA.Setup(p => p.SearchAsync(It.IsAny<SearchCriteria>()))
            .ReturnsAsync((IReadOnlyList<FlightOffer>)[MakeOffer("1", "A", Jfk, Lax)]);

        var providerB = new Mock<IFlightProvider>();
        providerB.SetupGet(p => p.ProviderName).Returns("B");
        providerB.Setup(p => p.SearchAsync(It.IsAny<SearchCriteria>()))
            .ReturnsAsync((IReadOnlyList<FlightOffer>)[MakeOffer("2", "B", Jfk, Lax)]);

        var repository = new Mock<ISearchResultRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<IEnumerable<FlightOffer>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("search-id-123");

        var service = new FlightAggregatorService(
            [providerA.Object, providerB.Object], repository.Object, NullLogger<FlightAggregatorService>.Instance);

        var result = await service.SearchAsync(new SearchCriteria("JFK", "LAX", DateTime.Today, 1));

        Assert.Equal(2, result.Results.Count());
        Assert.Empty(result.UnavailableProviders);
        Assert.Equal("search-id-123", result.SearchId);
        Assert.False(result.IsInternational);
        repository.Verify(
            r => r.SaveAsync(It.IsAny<IEnumerable<FlightOffer>>(), TimeSpan.FromMinutes(15)),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ProviderFailure_DoesNotFailOthers_AndIsRecordedAsUnavailable()
    {
        var healthyProvider = new Mock<IFlightProvider>();
        healthyProvider.SetupGet(p => p.ProviderName).Returns("Healthy");
        healthyProvider.Setup(p => p.SearchAsync(It.IsAny<SearchCriteria>()))
            .ReturnsAsync((IReadOnlyList<FlightOffer>)[MakeOffer("1", "Healthy", Jfk, Lhr)]);

        var failingProvider = new Mock<IFlightProvider>();
        failingProvider.SetupGet(p => p.ProviderName).Returns("Failing");
        failingProvider.Setup(p => p.SearchAsync(It.IsAny<SearchCriteria>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var repository = new Mock<ISearchResultRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<IEnumerable<FlightOffer>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("search-id-456");

        var service = new FlightAggregatorService(
            [healthyProvider.Object, failingProvider.Object],
            repository.Object,
            NullLogger<FlightAggregatorService>.Instance);

        var result = await service.SearchAsync(new SearchCriteria("JFK", "LHR", DateTime.Today, 1));

        Assert.Single(result.Results);
        Assert.Contains("Failing", result.UnavailableProviders);
        Assert.True(result.IsInternational);
    }

    [Fact]
    public async Task SearchAsync_AllProvidersFail_ReturnsEmptyResultsWithoutThrowing()
    {
        var failingProvider = new Mock<IFlightProvider>();
        failingProvider.SetupGet(p => p.ProviderName).Returns("Failing");
        failingProvider.Setup(p => p.SearchAsync(It.IsAny<SearchCriteria>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var repository = new Mock<ISearchResultRepository>();
        repository.Setup(r => r.SaveAsync(It.IsAny<IEnumerable<FlightOffer>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync("search-id-789");

        var service = new FlightAggregatorService(
            [failingProvider.Object], repository.Object, NullLogger<FlightAggregatorService>.Instance);

        var result = await service.SearchAsync(new SearchCriteria("JFK", "LAX", DateTime.Today, 1));

        Assert.Empty(result.Results);
        Assert.Single(result.UnavailableProviders);
        Assert.False(result.IsInternational);
    }
}
