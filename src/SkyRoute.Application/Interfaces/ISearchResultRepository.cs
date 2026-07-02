using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Interfaces;

public interface ISearchResultRepository
{
    Task<string> SaveAsync(IEnumerable<FlightOffer> results, TimeSpan ttl);
    Task<IEnumerable<FlightOffer>?> GetAsync(string searchId);
}
