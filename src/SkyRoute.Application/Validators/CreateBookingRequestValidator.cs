using FluentValidation;
using SkyRoute.Application.Models;

namespace SkyRoute.Application.Validators;

// Structural validation only — runs before we know whether the route is
// international, so document format rules live separately in
// PassengerDocumentFormatValidator.
public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(r => r.SearchId).NotEmpty();
        RuleFor(r => r.FlightId).NotEmpty();
        RuleFor(r => r.Passengers).NotEmpty();
        RuleForEach(r => r.Passengers).SetValidator(new PassengerDtoValidator());
    }
}
