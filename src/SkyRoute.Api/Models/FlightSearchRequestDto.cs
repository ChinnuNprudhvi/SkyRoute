namespace SkyRoute.Api.Models;

public record FlightSearchRequestDto(
    string OriginCode,
    string DestinationCode,
    DateOnly DepartureDate,
    int Passengers,
    string CabinClass);
