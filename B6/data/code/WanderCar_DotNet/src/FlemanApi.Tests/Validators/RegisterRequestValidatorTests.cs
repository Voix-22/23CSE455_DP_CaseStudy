using FlemanApi.DTO;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var request = new RegisterRequest { FullName = "Jane Doe", Email = "jane@example.com", Password = "secret1" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyFullName_HasError()
    {
        var request = new RegisterRequest { FullName = "", Email = "jane@example.com", Password = "secret1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FullName).WithErrorMessage("Full name is required");
    }

    [Test]
    public void Validate_EmptyEmail_HasError()
    {
        var request = new RegisterRequest { FullName = "Jane Doe", Email = "", Password = "secret1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required");
    }

    [Test]
    public void Validate_InvalidEmailFormat_HasError()
    {
        var request = new RegisterRequest { FullName = "Jane Doe", Email = "bad-email", Password = "secret1" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Enter a valid email");
    }

    [Test]
    public void Validate_EmptyPassword_HasError()
    {
        var request = new RegisterRequest { FullName = "Jane Doe", Email = "jane@example.com", Password = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorMessage("Password must be at least 6 characters");
    }

    [Test]
    public void Validate_PasswordTooShort_HasError()
    {
        var request = new RegisterRequest { FullName = "Jane Doe", Email = "jane@example.com", Password = "abc12" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorMessage("Password must be at least 6 characters");
    }

    [Test]
    public void Validate_PasswordExactlySixCharacters_Passes()
    {
        var request = new RegisterRequest { FullName = "Jane Doe", Email = "jane@example.com", Password = "abc123" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
