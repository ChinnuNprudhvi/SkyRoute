using Microsoft.Extensions.Logging;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Services;

public class FlightAggregatorService
{
    private static readonly TimeSpan SearchResultTtl = TimeSpan.FromMinutes(15);

    private readonly IEnumerable<IFlightProvider> _providers;
    private readonly ISearchResultRepository _searchResultRepository;
    private readonly ILogger<FlightAggregatorService> _logger;

    public FlightAggregatorService(
        IEnumerable<IFlightProvider> providers,
        ISearchResultRepository searchResultRepository,
        ILogger<FlightAggregatorService> logger)
    {
        _providers = providers;
        _searchResultRepository = searchResultRepository;
        _logger = logger;
    }

    public async Task<FlightSearchResult> SearchAsync(SearchCriteria criteria)
    {
        var unavailableProviders = new List<string>();
        var providerTasks = _providers
            .Select(provider => SearchProviderSafelyAsync(provider, criteria, unavailableProviders))
            .ToList();

        var providerResults = await Task.WhenAll(providerTasks);

        var mergedResults = providerResults.SelectMany(offers => offers).ToList();

        var isInternational = mergedResults.Count > 0 &&
            mergedResults[0].Origin.Country != mergedResults[0].Destination.Country;

        var searchId = await _searchResultRepository.SaveAsync(mergedResults, SearchResultTtl);

        _logger.LogInformation(
            "Flight search completed: {SearchId} {ResultCount} {PartialResults}",
            searchId,
            mergedResults.Count,
            unavailableProviders.Count > 0);

        return new FlightSearchResult(searchId, mergedResults, isInternational, unavailableProviders);
    }

    // Wraps a single provider call so a failure there can't fail Task.WhenAll for the
    // others: any exception is caught here, logged, and converted into an empty
    // result plus an entry in unavailableProviders.
    private async Task<IReadOnlyList<FlightOffer>> SearchProviderSafelyAsync(
        IFlightProvider provider,
        SearchCriteria criteria,
        List<string> unavailableProviders)
    {
        try
        {
            return await provider.SearchAsync(criteria);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Flight provider {ProviderName} failed to return search results",
                provider.ProviderName);

            lock (unavailableProviders)
            {
                unavailableProviders.Add(provider.ProviderName);
            }

            return Array.Empty<FlightOffer>();
        }
    }
}
