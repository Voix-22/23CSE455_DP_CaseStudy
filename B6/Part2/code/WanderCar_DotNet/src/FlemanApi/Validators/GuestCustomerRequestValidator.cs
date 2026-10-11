using FlemanApi.DTO;
using FluentValidation;

namespace FlemanApi.Validators;

public class GuestCustomerRequestValidator : AbstractValidator<GuestCustomerRequest>
{
    public GuestCustomerRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required");
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Enter a valid email");
    }
}
