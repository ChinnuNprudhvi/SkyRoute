using SkyRoute.Application.Models;
using SkyRoute.Application.Validators;

namespace SkyRoute.Tests.Validators;

public class PassengerDocumentFormatValidatorTests
{
    private readonly PassengerDocumentFormatValidator _validator = new();

    [Theory]
    [InlineData("AB12345")]
    [InlineData("ABCDEFGHI")]
    [InlineData("123456")]
    public void International_ValidPassportFormat_Passes(string documentNumber)
    {
        var passenger = new PassengerDto("Jane Doe", "jane@example.com", documentNumber);

        var result = _validator.Validate((passenger, true));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("AB12")]           // too short
    [InlineData("ABCDEFGHIJK")]    // too long
    [InlineData("AB-123456")]      // invalid characters
    public void International_InvalidPassportFormat_FailsWithPassportMessage(string documentNumber)
    {
        var passenger = new PassengerDto("Jane Doe", "jane@example.com", documentNumber);

        var result = _validator.Validate((passenger, true));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Passport Number must be 6-9 alphanumeric characters");
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("123456789012")]
    public void Domestic_ValidNationalIdFormat_Passes(string documentNumber)
    {
        var passenger = new PassengerDto("Jane Doe", "jane@example.com", documentNumber);

        var result = _validator.Validate((passenger, false));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("12345")]              // too short
    [InlineData("1234567890123")]      // too long
    [InlineData("AB123456")]           // not digits-only
    public void Domestic_InvalidNationalIdFormat_FailsWithNationalIdMessage(string documentNumber)
    {
        var passenger = new PassengerDto("Jane Doe", "jane@example.com", documentNumber);

        var result = _validator.Validate((passenger, false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "National ID must be 6-12 digits");
    }
}
