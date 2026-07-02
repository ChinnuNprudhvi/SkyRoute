using Microsoft.Extensions.Caching.Memory;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Repositories;

public class InMemorySearchResultRepository : ISearchResultRepository
{
    private const string CacheKeyPrefix = "search-results:";

    private readonly IMemoryCache _cache;

    public InMemorySearchResultRepository(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<string> SaveAsync(IEnumerable<FlightOffer> results, TimeSpan ttl)
    {
        var searchId = Guid.NewGuid().ToString("N")[..12];

        _cache.Set(CacheKeyPrefix + searchId, results.ToList(), ttl);

        return Task.FromResult(searchId);
    }

    public Task<IEnumerable<FlightOffer>?> GetAsync(string searchId)
    {
        var found = _cache.TryGetValue(CacheKeyPrefix + searchId, out List<FlightOffer>? results);

        return Task.FromResult(found ? results!.AsEnumerable() : null);
    }
}
