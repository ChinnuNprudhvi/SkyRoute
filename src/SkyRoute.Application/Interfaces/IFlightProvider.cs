using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Interfaces;

public interface IFlightProvider
{
    string ProviderName { get; }

    Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchCriteria criteria);
}
