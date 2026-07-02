using FluentValidation;
using SkyRoute.Application.Models;

namespace SkyRoute.Application.Validators;

public class PassengerDtoValidator : AbstractValidator<PassengerDto>
{
    public PassengerDtoValidator()
    {
        RuleFor(p => p.FullName).NotEmpty();
        RuleFor(p => p.Email).NotEmpty().EmailAddress();
        RuleFor(p => p.DocumentNumber).NotEmpty();
    }
}
