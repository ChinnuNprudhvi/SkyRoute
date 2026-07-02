using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Application.Validators;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;
using SkyRoute.Domain.Exceptions;

namespace SkyRoute.Application.Services;

public class BookingService
{
    private const string ReferenceAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private readonly ISearchResultRepository _searchResultRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IValidator<CreateBookingRequest> _createBookingRequestValidator;
    private readonly PassengerDocumentFormatValidator _passengerDocumentFormatValidator;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        ISearchResultRepository searchResultRepository,
        IBookingRepository bookingRepository,
        IValidator<CreateBookingRequest> createBookingRequestValidator,
        PassengerDocumentFormatValidator passengerDocumentFormatValidator,
        ILogger<BookingService> logger)
    {
        _searchResultRepository = searchResultRepository;
        _bookingRepository = bookingRepository;
        _createBookingRequestValidator = createBookingRequestValidator;
        _passengerDocumentFormatValidator = passengerDocumentFormatValidator;
        _logger = logger;
    }

    public async Task<Booking> CreateBookingAsync(CreateBookingRequest request)
    {
        await _createBookingRequestValidator.ValidateAndThrowAsync(request);

        var cachedResults = await _searchResultRepository.GetAsync(request.SearchId);
        if (cachedResults is null)
        {
            throw new SearchExpiredException($"Search '{request.SearchId}' was not found or has expired.");
        }

        var flightOffer = cachedResults.FirstOrDefault(f => f.Id == request.FlightId);
        if (flightOffer is null)
        {
            throw new FlightNotFoundException(
                $"Flight '{request.FlightId}' was not found within search '{request.SearchId}'.");
        }

        var documentFailures = new List<ValidationFailure>();
        foreach (var passenger in request.Passengers)
        {
            var result = await _passengerDocumentFormatValidator.ValidateAsync(
                (passenger, flightOffer.IsInternational));

            documentFailures.AddRange(result.Errors);
        }

        if (documentFailures.Count > 0)
        {
            throw new ValidationException(documentFailures);
        }

        var booking = new Booking(
            Reference: GenerateBookingReference(),
            FlightSnapshot: flightOffer,
            Passengers: request.Passengers
                .Select(p => new Passenger(p.FullName, p.Email, p.DocumentNumber))
                .ToList(),
            Status: BookingStatus.Confirmed,
            CreatedAt: DateTime.UtcNow);

        await _bookingRepository.SaveAsync(booking);

        _logger.LogInformation(
            "Booking confirmed: {BookingReference} {FlightId} {PassengerCount}",
            booking.Reference,
            flightOffer.Id,
            request.Passengers.Count);

        return booking;
    }

    private static string GenerateBookingReference()
    {
        var suffix = new char[6];
        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = ReferenceAlphabet[Random.Shared.Next(ReferenceAlphabet.Length)];
        }

        return "SKY-" + new string(suffix);
    }
}
