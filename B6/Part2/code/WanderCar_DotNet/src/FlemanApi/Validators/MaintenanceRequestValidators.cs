using FlemanApi.DTO;
using FluentValidation;

namespace FlemanApi.Validators;

// Shape checks only. "Not in the past" needs the clock and "car exists"
// needs the database, so both are checked by MaintenanceService.
public class ScheduleMaintenanceRequestValidator : AbstractValidator<ScheduleMaintenanceRequest>
{
    public const int MaxNotesLength = 500;

    public ScheduleMaintenanceRequestValidator()
    {
        RuleFor(x => x.CarId).NotNull().WithMessage("carId is required");

        RuleFor(x => x.MaintenanceType)
            .NotNull().WithMessage("maintenanceType is required")
            .IsInEnum().WithMessage("maintenanceType must be SERVICE, INSPECTION or REPAIR");

        RuleFor(x => x.ScheduledDate).NotNull().WithMessage("scheduledDate is required");

        RuleFor(x => x.Notes)
            .MaximumLength(MaxNotesLength).WithMessage("notes must be at most 500 characters");
    }
}

public class MaintenanceTransitionRequestValidator : AbstractValidator<MaintenanceTransitionRequest>
{
    public MaintenanceTransitionRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(ScheduleMaintenanceRequestValidator.MaxNotesLength)
            .WithMessage("notes must be at most 500 characters");
    }
}
