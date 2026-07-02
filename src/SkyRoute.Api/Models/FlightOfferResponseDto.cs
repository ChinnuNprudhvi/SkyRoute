namespace SkyRoute.Api.Models;

public record FlightOfferResponseDto(
    string FlightId,
    string Provider,
    string FlightNumber,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    int DurationMinutes,
    string CabinClass,
    decimal TotalPrice,
    decimal PricePerPerson,
    string Currency);
