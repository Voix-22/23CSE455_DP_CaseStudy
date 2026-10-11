using FlemanApi.DTO;
using FlemanApi.Models;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class CreateCarTypeRequestValidatorTests
{
    private readonly CreateCarTypeRequestValidator _validator = new();

    private static CreateCarTypeRequest MakeValidRequest() => new()
    {
        CarTypeName = "City EV",
        Category = VehicleCategory.ELECTRIC,
        DailyRate = 60.0,
        WeeklyRate = 350.0,
        MonthlyRate = 1200.0,
    };

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.TestValidate(MakeValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_OnlyRequiredFields_Passes()
    {
        var request = new CreateCarTypeRequest { CarTypeName = "Hatchback", Category = VehicleCategory.STANDARD, DailyRate = 18.0 };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_EmptyCarTypeName_HasError()
    {
        var request = MakeValidRequest();
        request.CarTypeName = "";

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CarTypeName).WithErrorMessage("carTypeName is required");
    }

    [Test]
    public void Validate_CarTypeNameTooLong_HasError()
    {
        var request = MakeValidRequest();
        request.CarTypeName = new string('x', 101);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CarTypeName).WithErrorMessage("carTypeName must be at most 100 characters");
    }

    [Test]
    public void Validate_NullCategory_HasError()
    {
        var request = MakeValidRequest();
        request.Category = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Category).WithErrorMessage("category is required");
    }

    [Test]
    public void Validate_CategoryOutsideEnum_HasError()
    {
        var request = MakeValidRequest();
        request.Category = (VehicleCategory)999;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Test]
    public void Validate_NullDailyRate_HasError()
    {
        var request = MakeValidRequest();
        request.DailyRate = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DailyRate).WithErrorMessage("dailyRate is required");
    }

    [Test]
    public void Validate_ZeroDailyRate_HasError()
    {
        var request = MakeValidRequest();
        request.DailyRate = 0.0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DailyRate).WithErrorMessage("dailyRate must be greater than 0");
    }

    [Test]
    public void Validate_NegativeWeeklyRate_HasError()
    {
        var request = MakeValidRequest();
        request.WeeklyRate = -1.0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.WeeklyRate).WithErrorMessage("weeklyRate must be greater than 0");
    }

    [Test]
    public void Validate_NegativeMonthlyRate_HasError()
    {
        var request = MakeValidRequest();
        request.MonthlyRate = -1.0;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MonthlyRate).WithErrorMessage("monthlyRate must be greater than 0");
    }
}
