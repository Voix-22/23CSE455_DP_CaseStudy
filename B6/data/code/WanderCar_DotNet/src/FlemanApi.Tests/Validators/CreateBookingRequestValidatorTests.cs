using FlemanApi.DTO;
using FlemanApi.Validators;
using FluentValidation.TestHelper;

namespace FlemanApi.Tests.Validators;

[TestFixture]
public class CreateBookingRequestValidatorTests
{
    private readonly CreateBookingRequestValidator _validator = new();

    private static CreateBookingRequest MakeValidRequest() => new()
    {
        CustomerId = 1,
        CarTypeId = 1,
        PickupHubId = 1,
        PickupDatetime = new DateTime(2026, 8, 10),
        ReturnDatetime = new DateTime(2026, 8, 15),
    };

    [Test]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.TestValidate(MakeValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NullCustomerId_HasError()
    {
        var request = MakeValidRequest();
        request.CustomerId = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CustomerId).WithErrorMessage("customerId is required");
    }

    [Test]
    public void Validate_NullCarTypeId_HasError()
    {
        var request = MakeValidRequest();
        request.CarTypeId = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CarTypeId).WithErrorMessage("carTypeId is required");
    }

    [Test]
    public void Validate_NullPickupHubId_HasError()
    {
        var request = MakeValidRequest();
        request.PickupHubId = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.PickupHubId).WithErrorMessage("pickupHubId is required");
    }

    [Test]
    public void Validate_NullPickupDatetime_HasError()
    {
        var request = MakeValidRequest();
        request.PickupDatetime = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.PickupDatetime).WithErrorMessage("pickupDatetime is required");
    }

    [Test]
    public void Validate_NullReturnDatetime_HasError()
    {
        var request = MakeValidRequest();
        request.ReturnDatetime = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ReturnDatetime).WithErrorMessage("returnDatetime is required");
    }

    [Test]
    public void Validate_AddonWithNullAddonId_HasError()
    {
        var request = MakeValidRequest();
        request.Addons = new List<AddonLineRequest> { new() { AddonId = null, Quantity = 1 } };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Addons[0].AddonId");
    }

    [Test]
    public void Validate_AddonWithZeroQuantity_HasError()
    {
        var request = MakeValidRequest();
        request.Addons = new List<AddonLineRequest> { new() { AddonId = 1, Quantity = 0 } };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Addons[0].Quantity");
    }

    [Test]
    public void Validate_ValidAddons_Passes()
    {
        var request = MakeValidRequest();
        request.Addons = new List<AddonLineRequest> { new() { AddonId = 1, Quantity = 2 } };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NullAddons_DoesNotValidateAddonRules()
    {
        var request = MakeValidRequest();
        request.Addons = null;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

[TestFixture]
public class AddonLineRequestValidatorTests
{
    private readonly AddonLineRequestValidator _validator = new();

    [Test]
    public void Validate_ValidLine_Passes()
    {
        var line = new AddonLineRequest { AddonId = 1, Quantity = 1 };

        var result = _validator.TestValidate(line);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_NullAddonId_HasError()
    {
        var line = new AddonLineRequest { AddonId = null, Quantity = 1 };

        var result = _validator.TestValidate(line);

        result.ShouldHaveValidationErrorFor(x => x.AddonId).WithErrorMessage("addonId is required");
    }

    [Test]
    public void Validate_QuantityLessThanOne_HasError()
    {
        var line = new AddonLineRequest { AddonId = 1, Quantity = 0 };

        var result = _validator.TestValidate(line);

        result.ShouldHaveValidationErrorFor(x => x.Quantity).WithErrorMessage("quantity must be at least 1");
    }

    [Test]
    public void Validate_NullQuantity_Passes()
    {
        var line = new AddonLineRequest { AddonId = 1, Quantity = null };

        var result = _validator.TestValidate(line);

        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }
}
