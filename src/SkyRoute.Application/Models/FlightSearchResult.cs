using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Models;

public record FlightSearchResult(
    string SearchId,
    IEnumerable<FlightOffer> Results,
    bool IsInternational,
    IReadOnlyList<string> UnavailableProviders);
