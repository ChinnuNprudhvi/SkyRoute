using SkyRoute.Domain.Enums;

namespace SkyRoute.Application.Models;

public record SearchCriteria(
    string Origin,
    string Destination,
    DateTime DepartureDate,
    int Passengers,
    CabinClass CabinClass = CabinClass.Economy);
