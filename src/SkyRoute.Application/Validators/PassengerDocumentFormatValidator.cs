using FluentValidation;
using SkyRoute.Application.Models;

namespace SkyRoute.Application.Validators;

public class PassengerDocumentFormatValidator : AbstractValidator<(PassengerDto Passenger, bool IsInternational)>
{
    public PassengerDocumentFormatValidator()
    {
        RuleFor(x => x.Passenger.DocumentNumber)
            .Matches(@"^[A-Za-z0-9]{6,9}$")
            .WithMessage("Passport Number must be 6-9 alphanumeric characters")
            .When(x => x.IsInternational);

        RuleFor(x => x.Passenger.DocumentNumber)
            .Matches(@"^\d{6,12}$")
            .WithMessage("National ID must be 6-12 digits")
            .When(x => !x.IsInternational);
    }
}
