using FlemanApi.DTO;
using FluentValidation;

namespace FlemanApi.Validators;

// Shape checks only. Category-specific rate rules live in
// IVehicleCategoryRules and are applied by CarTypeService.
public class CreateCarTypeRequestValidator : AbstractValidator<CreateCarTypeRequest>
{
    public CreateCarTypeRequestValidator()
    {
        RuleFor(x => x.CarTypeName)
            .NotEmpty().WithMessage("carTypeName is required")
            .MaximumLength(100).WithMessage("carTypeName must be at most 100 characters");

        RuleFor(x => x.Category)
            .NotNull().WithMessage("category is required")
            .IsInEnum().WithMessage("category must be STANDARD, ELECTRIC or COMMERCIAL");

        RuleFor(x => x.DailyRate)
            .NotNull().WithMessage("dailyRate is required")
            .GreaterThan(0).WithMessage("dailyRate must be greater than 0");

        RuleFor(x => x.WeeklyRate)
            .GreaterThan(0).When(x => x.WeeklyRate is not null)
            .WithMessage("weeklyRate must be greater than 0");

        RuleFor(x => x.MonthlyRate)
            .GreaterThan(0).When(x => x.MonthlyRate is not null)
            .WithMessage("monthlyRate must be greater than 0");
    }
}
