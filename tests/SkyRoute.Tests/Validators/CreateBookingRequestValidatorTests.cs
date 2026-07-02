using SkyRoute.Application.Models;
using SkyRoute.Application.Validators;

namespace SkyRoute.Tests.Validators;

public class CreateBookingRequestValidatorTests
{
    private readonly CreateBookingRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Passes()
    {
        var request = new CreateBookingRequest(
            "search-1",
            "flight-1",
            [new PassengerDto("Jane Doe", "jane@example.com", "123456")]);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_SearchId_And_FlightId_Fails()
    {
        var request = new CreateBookingRequest("", "", [new PassengerDto("Jane Doe", "jane@example.com", "123456")]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookingRequest.SearchId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookingRequest.FlightId));
    }

    [Fact]
    public void Empty_Passengers_Fails()
    {
        var request = new CreateBookingRequest("search-1", "flight-1", []);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBookingRequest.Passengers));
    }

    [Fact]
    public void Invalid_Passenger_Email_Fails()
    {
        var request = new CreateBookingRequest(
            "search-1",
            "flight-1",
            [new PassengerDto("Jane Doe", "not-an-email", "123456")]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("Email"));
    }
}
