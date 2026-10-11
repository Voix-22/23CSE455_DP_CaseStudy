using FlemanApi.DTO;
using FluentValidation;

namespace FlemanApi.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotNull().WithMessage("customerId is required");
        RuleFor(x => x.CarTypeId).NotNull().WithMessage("carTypeId is required");
        RuleFor(x => x.PickupHubId).NotNull().WithMessage("pickupHubId is required");
        RuleFor(x => x.PickupDatetime).NotNull().WithMessage("pickupDatetime is required");
        RuleFor(x => x.ReturnDatetime).NotNull().WithMessage("returnDatetime is required");

        RuleForEach(x => x.Addons).SetValidator(new AddonLineRequestValidator()).When(x => x.Addons is not null);
    }
}

public class AddonLineRequestValidator : AbstractValidator<AddonLineRequest>
{
    public AddonLineRequestValidator()
    {
        RuleFor(x => x.AddonId).NotNull().WithMessage("addonId is required");
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(1).When(x => x.Quantity is not null)
            .WithMessage("quantity must be at least 1");
    }
}
