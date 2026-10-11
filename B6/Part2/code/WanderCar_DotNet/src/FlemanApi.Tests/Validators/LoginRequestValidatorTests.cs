using FlemanApi.DTO;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "secret" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyEmail_HasError()
    {
        var request = new LoginRequest { Email = "", Password = "secret" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required");
    }

    [Test]
    public void Validate_InvalidEmailFormat_HasError()
    {
        var request = new LoginRequest { Email = "not-an-email", Password = "secret" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Enter a valid email");
    }

    [Test]
    public void Validate_EmptyPassword_HasError()
    {
        var request = new LoginRequest { Email = "user@example.com", Password = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorMessage("Password is required");
    }
}
