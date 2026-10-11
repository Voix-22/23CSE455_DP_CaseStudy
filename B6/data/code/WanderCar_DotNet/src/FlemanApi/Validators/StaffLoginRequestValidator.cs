using FlemanApi.DTO;
using FluentValidation;

namespace FlemanApi.Validators;

public class StaffLoginRequestValidator : AbstractValidator<StaffLoginRequest>
{
    public StaffLoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Staff username is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Staff password is required");
    }
}
