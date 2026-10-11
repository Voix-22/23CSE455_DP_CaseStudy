using FlemanApi.DTO;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class AirportRequestValidatorTests
{
    private readonly AirportRequestValidator _validator = new();

    private static AirportRequest MakeValidRequest() => new()
    {
        AirportName = "Chhatrapati Shivaji Maharaj International",
        AirportCode = "BOM",
        CityId = 1,
        StateId = 1,
        HubId = 1,
    };

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.TestValidate(MakeValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyAirportName_HasError()
    {
        var request = MakeValidRequest();
        request.AirportName = "";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AirportName).WithErrorMessage("airportName is required");
    }

    [Test]
    public void Validate_AirportNameTooLong_HasError()
    {
        var request = MakeValidRequest();
        request.AirportName = new string('a', 151);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AirportName)
            .WithErrorMessage("airportName must be at most 150 characters");
    }

    [Test]
    public void Validate_EmptyAirportCode_HasError()
    {
        var request = MakeValidRequest();
        request.AirportCode = "";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AirportCode).WithErrorMessage("airportCode is required");
    }

    [Test]
    public void Validate_AirportCodeTooLong_HasError()
    {
        var request = MakeValidRequest();
        request.AirportCode = new string('a', 11);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AirportCode)
            .WithErrorMessage("airportCode must be at most 10 characters");
    }

    [Test]
    public void Validate_NullCityId_HasError()
    {
        var request = MakeValidRequest();
        request.CityId = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CityId).WithErrorMessage("cityId is required");
    }

    [Test]
    public void Validate_NullStateId_HasError()
    {
        var request = MakeValidRequest();
        request.StateId = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.StateId).WithErrorMessage("stateId is required");
    }

    [Test]
    public void Validate_NullHubId_HasError()
    {
        var request = MakeValidRequest();
        request.HubId = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.HubId).WithErrorMessage("hubId is required");
    }
}
