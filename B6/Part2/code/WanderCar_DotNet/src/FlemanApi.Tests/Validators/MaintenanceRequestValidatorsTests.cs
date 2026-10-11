using FlemanApi.DTO;
using FlemanApi.Models;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class ScheduleMaintenanceRequestValidatorTests
{
    private readonly ScheduleMaintenanceRequestValidator _validator = new();

    private static ScheduleMaintenanceRequest MakeValidRequest() => new()
    {
        CarId = 1,
        MaintenanceType = MaintenanceType.SERVICE,
        ScheduledDate = new DateTime(2026, 10, 12),
        Notes = "oil change",
    };

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.TestValidate(MakeValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NullCarId_HasError()
    {
        var request = MakeValidRequest();
        request.CarId = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CarId).WithErrorMessage("carId is required");
    }

    [Test]
    public void Validate_NullMaintenanceType_HasError()
    {
        var request = MakeValidRequest();
        request.MaintenanceType = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaintenanceType).WithErrorMessage("maintenanceType is required");
    }

    [Test]
    public void Validate_UndefinedMaintenanceType_HasError()
    {
        var request = MakeValidRequest();
        request.MaintenanceType = (MaintenanceType)999;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaintenanceType);
    }

    [Test]
    public void Validate_NullScheduledDate_HasError()
    {
        var request = MakeValidRequest();
        request.ScheduledDate = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ScheduledDate).WithErrorMessage("scheduledDate is required");
    }

    [Test]
    public void Validate_NullNotes_Passes()
    {
        var request = MakeValidRequest();
        request.Notes = null;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NotesTooLong_HasError()
    {
        var request = MakeValidRequest();
        request.Notes = new string('x', 501);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Notes).WithErrorMessage("notes must be at most 500 characters");
    }
}

[TestFixture]
public class MaintenanceTransitionRequestValidatorTests
{
    private readonly MaintenanceTransitionRequestValidator _validator = new();

    [Test]
    public void Validate_EmptyBody_Passes()
    {
        var result = _validator.TestValidate(new MaintenanceTransitionRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NotesAtLimit_Passes()
    {
        var result = _validator.TestValidate(new MaintenanceTransitionRequest { Notes = new string('x', 500) });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NotesTooLong_HasError()
    {
        var result = _validator.TestValidate(new MaintenanceTransitionRequest { Notes = new string('x', 501) });

        result.ShouldHaveValidationErrorFor(x => x.Notes).WithErrorMessage("notes must be at most 500 characters");
    }
}
