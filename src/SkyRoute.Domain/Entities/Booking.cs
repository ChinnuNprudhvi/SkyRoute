using SkyRoute.Domain.Enums;

namespace SkyRoute.Domain.Entities;

public record Booking(
    string Reference,
    FlightOffer FlightSnapshot,
    List<Passenger> Passengers,
    BookingStatus Status,
    DateTime CreatedAt);
