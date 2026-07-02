namespace SkyRoute.Application.Models;

public record CreateBookingRequest(string SearchId, string FlightId, List<PassengerDto> Passengers);
