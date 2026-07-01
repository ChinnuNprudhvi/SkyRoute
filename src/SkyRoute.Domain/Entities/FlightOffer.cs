using SkyRoute.Domain.Enums;

namespace SkyRoute.Domain.Entities;

public record FlightOffer(
    string Id,
    string Provider,
    string FlightNumber,
    Airport Origin,
    Airport Destination,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    CabinClass CabinClass,
    decimal BaseFare,
    decimal TotalPrice,
    decimal PricePerPerson)
{
    public double DurationMinutes => (ArrivalTime - DepartureTime).TotalMinutes;

    public bool IsInternational => Origin.Country != Destination.Country;
}
