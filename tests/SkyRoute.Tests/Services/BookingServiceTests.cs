using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Application.Services;
using SkyRoute.Application.Validators;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Exceptions;

namespace SkyRoute.Tests.Services;

public class BookingServiceTests
{
    private const string FlightId = "flight-1";
    private const string SearchId = "search-1";

    private static Airport Jfk => new("JFK", "JFK Airport", "New York", "United States");
    private static Airport Lax => new("LAX", "LAX Airport", "Los Angeles", "United States");
    private static Airport Lhr => new("LHR", "Heathrow Airport", "London", "United Kingdom");

    /// <summary>
    /// Builds a single cached FlightOffer with a known price and a route whose
    /// IsInternational value matches the requested parameter, plus a fixed searchId.
    /// </summary>
    private static (FlightOffer Offer, string SearchId) MakeCachedSearch(bool isInternational)
    {
        var origin = Jfk;
        var destination = isInternational ? Lhr : Lax;

        var offer = new FlightOffer(
            Id: FlightId,
            Provider: "GlobalAir",
            FlightNumber: "GA001",
            Origin: origin,
            Destination: destination,
            DepartureTime: DateTime.Today.AddDays(7),
            ArrivalTime: DateTime.Today.AddDays(7).AddHours(6),
            CabinClass: CabinClass.Economy,
            BaseFare: 391.30m,
            TotalPrice: 450.00m,
            PricePerPerson: 225.00m);

        return (offer, SearchId);
    }

    private static BookingService BuildService(
        Mock<ISearchResultRepository> searchResultRepository,
        Mock<IBookingRepository> bookingRepository)
    {
        return new BookingService(
            searchResultRepository.Object,
            bookingRepository.Object,
            new CreateBookingRequestValidator(),
            new PassengerDocumentFormatValidator(),
            NullLogger<BookingService>.Instance);
    }

    [Fact]
    public async Task CreateBookingAsync_ReadsPriceFromCachedOffer_NeverFromRequest()
    {
        var (cachedOffer, searchId) = MakeCachedSearch(isInternational: false);

        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync(searchId))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[cachedOffer]);

        Booking? capturedBooking = null;
        var bookingRepository = new Mock<IBookingRepository>();
        bookingRepository.Setup(r => r.SaveAsync(It.IsAny<Booking>()))
            .Callback<Booking>(b => capturedBooking = b)
            .Returns(Task.CompletedTask);

        var service = BuildService(searchResultRepository, bookingRepository);
        var request = new CreateBookingRequest(
            searchId,
            FlightId,
            [new PassengerDto("Jane Doe", "jane@example.com", "123456")]);

        await service.CreateBookingAsync(request);

        Assert.NotNull(capturedBooking);
        Assert.Equal(450.00m, capturedBooking!.FlightSnapshot.TotalPrice);
    }

    [Fact]
    public async Task CreateBookingAsync_SearchIdNotFound_ThrowsSearchExpiredException()
    {
        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((IEnumerable<FlightOffer>?)null);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = new CreateBookingRequest(
            "missing-search",
            FlightId,
            [new PassengerDto("Jane Doe", "jane@example.com", "123456")]);

        await Assert.ThrowsAsync<SearchExpiredException>(() => service.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CreateBookingAsync_FlightIdNotInCachedSearch_ThrowsFlightNotFoundException()
    {
        var (cachedOffer, searchId) = MakeCachedSearch(isInternational: false);

        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync(searchId))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[cachedOffer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = new CreateBookingRequest(
            searchId,
            "some-other-flight-id",
            [new PassengerDto("Jane Doe", "jane@example.com", "123456")]);

        await Assert.ThrowsAsync<FlightNotFoundException>(() => service.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CreateBookingAsync_InternationalRoute_RejectsNationalIdShapedDocument()
    {
        var (cachedOffer, searchId) = MakeCachedSearch(isInternational: true);

        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync(searchId))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[cachedOffer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = new CreateBookingRequest(
            searchId,
            FlightId,
            [new PassengerDto("Jane Doe", "jane@example.com", "123456789012")]); // National-ID-shaped

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CreateBookingAsync_DomesticRoute_RejectsPassportShapedDocument()
    {
        var (cachedOffer, searchId) = MakeCachedSearch(isInternational: false);

        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync(searchId))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[cachedOffer]);

        var bookingRepository = new Mock<IBookingRepository>();
        var service = BuildService(searchResultRepository, bookingRepository);

        var request = new CreateBookingRequest(
            searchId,
            FlightId,
            [new PassengerDto("Jane Doe", "jane@example.com", "AB1234567")]); // Passport-shaped

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateBookingAsync(request));
    }

    [Fact]
    public async Task CreateBookingAsync_ValidRequest_GeneratesReferenceWithSkyPrefixAndSavesConfirmedStatus()
    {
        var (cachedOffer, searchId) = MakeCachedSearch(isInternational: false);

        var searchResultRepository = new Mock<ISearchResultRepository>();
        searchResultRepository.Setup(r => r.GetAsync(searchId))
            .ReturnsAsync((IEnumerable<FlightOffer>?)[cachedOffer]);

        var bookingRepository = new Mock<IBookingRepository>();
        bookingRepository.Setup(r => r.SaveAsync(It.IsAny<Booking>())).Returns(Task.CompletedTask);

        var service = BuildService(searchResultRepository, bookingRepository);
        var request = new CreateBookingRequest(
            searchId,
            FlightId,
            [new PassengerDto("Jane Doe", "jane@example.com", "123456")]); // National-ID-shaped, matches domestic route

        var booking = await service.CreateBookingAsync(request);

        Assert.StartsWith("SKY-", booking.Reference);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        bookingRepository.Verify(r => r.SaveAsync(It.IsAny<Booking>()), Times.Once);
    }
}
