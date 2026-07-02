namespace SkyRoute.Api.Models;

public record BookingResponseDto(
    string BookingReference,
    string Status,
    decimal TotalPrice,
    FlightSummaryDto FlightSummary);

public record FlightSummaryDto(
    string Provider,
    string FlightNumber,
    string Origin,
    string Destination,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    string CabinClass);
