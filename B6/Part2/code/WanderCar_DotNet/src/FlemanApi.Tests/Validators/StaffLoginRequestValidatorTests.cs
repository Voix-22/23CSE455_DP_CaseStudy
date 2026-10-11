using FlemanApi.DTO;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class StaffLoginRequestValidatorTests
{
    private readonly StaffLoginRequestValidator _validator = new();

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var request = new StaffLoginRequest { Username = "Team2@gmail.com", Password = "123456789" };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyUsername_HasError()
    {
        var request = new StaffLoginRequest { Username = "", Password = "123456789" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Username).WithErrorMessage("Staff username is required");
    }

    [Test]
    public void Validate_EmptyPassword_HasError()
    {
        var request = new StaffLoginRequest { Username = "Team2@gmail.com", Password = "" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorMessage("Staff password is required");
    }
}
