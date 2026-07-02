using FluentValidation;
using Moq;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Application.Validators;
using SkyRoute.Application.Services;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Exceptions;

namespace SkyRoute.Tests.Services;

public class BookingServiceTests
{
    private static Airport Jfk => new("JFK", "JFK Airport", "New York", "United States");
    private static Airport Lhr => new("LHR", "Heathrow", "London", "United Kingdom");
    private static Airport Lax => new("LAX", "LAX Airport", "Los Angeles", "United States");

    private static FlightOffer MakeOffer(string id, Airport origin, Airport destination) =>
        new(id, "GlobalAir", "GA001", origin, destination,
            DateTime.Today, DateTime.Today.AddHours(3),
            CabinClass.Economy, 100m, 115m, 115m);

    private static CreateBookingRequest MakeRequest(string searchId, string flightId, string documentNumber = "123456") =>
        new(searchId, flightId, [new PassengerDto("Jane Doe", "jane@example.com", documentNumber)]);

    private static BookingService BuildService(
        Mock<ISearchResultRepository> searchResultRepository,
        Mock<IBookingRepository> bookingRepository)
    {
        return new BookingService(
            searchResultRepository.Object,
            bookingRepository.Object,
            new CreateBookingRequestValidator(),
            new PassengerDocumentFormatValidator());
    }

    [Fact]
    public async Task CreateBookingAsync_ValidDomesticRequest_SavesAndReturnsBooking()
    {
        var offer = MakeOffer("flight-1", Jfk, Lax);
        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync("search-1"))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[offer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = MakeRequest("search-1", "flight-1", "123456");

        var booking = await service.CreateBookingAsync(request);

        Assert.StartsWith("SKY-", booking.Reference);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Same(offer, booking.FlightSnapshot);
        bookingRepository.Verify(r => r.SaveAsync(It.Is<Booking>(b => b.Reference == booking.Reference)), Times.Once);
    }

    [Fact]
    public async Task CreateBookingAsync_StructurallyInvalidRequest_ThrowsValidationException()
    {
        var searchResultRepository = new Mock<ISearchResultRepository>();
        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = new CreateBookingRequest("", "", []);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateBookingAsync(request));
        searchResultRepository.Verify(r => r.GetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookingAsync_ExpiredSearch_ThrowsSearchExpiredException()
    {
        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync("missing-search"))
            .ReturnsAsync((IEnumerable<FlightOffer>?)null);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = MakeRequest("missing-search", "flight-1");

        await Assert.ThrowsAsync<SearchExpiredException>(() => service.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CreateBookingAsync_FlightNotInCachedResults_ThrowsFlightNotFoundException()
    {
        var offer = MakeOffer("flight-1", Jfk, Lax);
        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync("search-1"))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[offer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = MakeRequest("search-1", "does-not-exist");

        await Assert.ThrowsAsync<FlightNotFoundException>(() => service.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CreateBookingAsync_InternationalFlight_RequiresPassportFormat_ThrowsWhenDocumentIsDigitsOnly()
    {
        // JFK -> LHR is international, so a purely-numeric national-ID-style document must fail.
        var offer = MakeOffer("flight-1", Jfk, Lhr);
        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync("search-1"))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[offer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = MakeRequest("search-1", "flight-1", "123456789012"); // 12 digits, not alphanumeric passport-shaped... but only digits still matches [A-Za-z0-9]{6,9}? length 12 > 9 so fails
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateBookingAsync(request));

        Assert.Contains(ex.Errors, e => e.ErrorMessage.Contains("Passport Number"));
    }

    [Fact]
    public async Task CreateBookingAsync_DomesticFlight_RequiresNationalIdFormat_ThrowsWhenDocumentIsAlphanumeric()
    {
        // JFK -> LAX is domestic, so a passport-style alphanumeric document must fail.
        var offer = MakeOffer("flight-1", Jfk, Lax);
        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync("search-1"))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[offer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = MakeRequest("search-1", "flight-1", "AB1234567");
        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.CreateBookingAsync(request));

        Assert.Contains(ex.Errors, e => e.ErrorMessage.Contains("National ID"));
    }

    [Fact]
    public async Task CreateBookingAsync_UsesPriceAndInternationalFlagFromCachedOffer_NotFromRequest()
    {
        // Even though nothing on CreateBookingRequest carries price/IsInternational,
        // this test documents/guards the trust-boundary rule: those values must
        // always come from the cached FlightOffer.
        var offer = MakeOffer("flight-1", Jfk, Lhr);
        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync("search-1"))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[offer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = MakeRequest("search-1", "flight-1", "AB123456");

        var booking = await service.CreateBookingAsync(request);

        Assert.Equal(offer.TotalPrice, booking.FlightSnapshot.TotalPrice);
        Assert.Equal(offer.PricePerPerson, booking.FlightSnapshot.PricePerPerson);
        Assert.True(booking.FlightSnapshot.IsInternational);
    }
}
