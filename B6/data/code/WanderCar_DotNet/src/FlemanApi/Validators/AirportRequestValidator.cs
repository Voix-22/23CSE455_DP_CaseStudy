using FlemanApi.DTO;
using FluentValidation;

namespace FlemanApi.Validators;

// Field lengths/required-ness match AirportRequest.java's Bean Validation
// annotations exactly.
public class AirportRequestValidator : AbstractValidator<AirportRequest>
{
    public AirportRequestValidator()
    {
        RuleFor(x => x.AirportName)
            .NotEmpty().WithMessage("airportName is required")
            .MaximumLength(150).WithMessage("airportName must be at most 150 characters");

        RuleFor(x => x.AirportCode)
            .NotEmpty().WithMessage("airportCode is required")
            .MaximumLength(10).WithMessage("airportCode must be at most 10 characters");

        RuleFor(x => x.CityId).NotNull().WithMessage("cityId is required");
        RuleFor(x => x.StateId).NotNull().WithMessage("stateId is required");
        RuleFor(x => x.HubId).NotNull().WithMessage("hubId is required");
    }
}
