using SkyRoute.Api.Models;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;

namespace SkyRoute.Api.Mapping;

public static class MappingExtensions
{
    public static AirportDto ToDto(this Airport airport) =>
        new(airport.Code, airport.Name, airport.City, airport.Country);

    public static FlightOfferResponseDto ToDto(this FlightOffer offer) =>
        new(
            FlightId: offer.Id,
            Provider: offer.Provider,
            FlightNumber: offer.FlightNumber,
            DepartureTime: offer.DepartureTime,
            ArrivalTime: offer.ArrivalTime,
            DurationMinutes: (int)offer.DurationMinutes,
            CabinClass: offer.CabinClass.ToString(),
            TotalPrice: offer.TotalPrice,
            PricePerPerson: offer.PricePerPerson,
            Currency: "USD");

    public static SearchCriteria ToCriteria(this FlightSearchRequestDto dto) =>
        new(
            Origin: dto.OriginCode,
            Destination: dto.DestinationCode,
            DepartureDate: dto.DepartureDate.ToDateTime(TimeOnly.MinValue),
            Passengers: dto.Passengers,
            CabinClass: Enum.Parse<CabinClass>(dto.CabinClass, ignoreCase: true));

    public static FlightSearchResponseDto ToResponseDto(this FlightSearchResult result)
    {
        var partialResults = result.UnavailableProviders.Count > 0;

        return new FlightSearchResponseDto(
            SearchId: result.SearchId,
            IsInternational: result.IsInternational,
            Results: result.Results.Select(r => r.ToDto()).ToList(),
            PartialResults: partialResults,
            Notice: partialResults ? "Some results may be temporarily limited." : null);
    }

    public static BookingResponseDto ToResponseDto(this Booking booking) =>
        new(
            BookingReference: booking.Reference,
            Status: booking.Status.ToString(),
            TotalPrice: booking.FlightSnapshot.TotalPrice,
            FlightSummary: new FlightSummaryDto(
                Provider: booking.FlightSnapshot.Provider,
                FlightNumber: booking.FlightSnapshot.FlightNumber,
                Origin: booking.FlightSnapshot.Origin.Code,
                Destination: booking.FlightSnapshot.Destination.Code,
                DepartureTime: booking.FlightSnapshot.DepartureTime,
                ArrivalTime: booking.FlightSnapshot.ArrivalTime,
                CabinClass: booking.FlightSnapshot.CabinClass.ToString()));
}
