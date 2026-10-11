using FlemanApi.DTO;
using FluentValidation;

namespace FlemanApi.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Enter a valid email");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}
