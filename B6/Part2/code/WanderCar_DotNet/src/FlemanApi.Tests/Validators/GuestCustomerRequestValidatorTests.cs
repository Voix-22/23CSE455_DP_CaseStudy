using FlemanApi.DTO;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class GuestCustomerRequestValidatorTests
{
    private readonly GuestCustomerRequestValidator _validator = new();

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var request = new GuestCustomerRequest { FirstName = "John", Email = "john@example.com" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyFirstName_HasError()
    {
        var request = new GuestCustomerRequest { FirstName = "", Email = "john@example.com" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FirstName).WithErrorMessage("First name is required");
    }

    [Test]
    public void Validate_EmptyEmail_HasError()
    {
        var request = new GuestCustomerRequest { FirstName = "John", Email = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Email is required");
    }

    [Test]
    public void Validate_InvalidEmailFormat_HasError()
    {
        var request = new GuestCustomerRequest { FirstName = "John", Email = "not-an-email" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Enter a valid email");
    }
}
