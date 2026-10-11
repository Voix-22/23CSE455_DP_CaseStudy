using FlemanApi.Models;

namespace FlemanApi.Service.Categories;

public class CommercialCategoryRules : IVehicleCategoryRules
{
    private const double DepositMultiplier = 5.0;

    public VehicleCategory Category => VehicleCategory.COMMERCIAL;

    public double CalculateSecurityDeposit(double dailyRate) => dailyRate * DepositMultiplier;

    // Commercial vehicles are let by the week, so a weekly rate is mandatory.
    public IReadOnlyList<string> ValidateRates(double dailyRate, double? weeklyRate, double? monthlyRate)
    {
        var errors = new List<string>();
        if (weeklyRate is null) errors.Add("weeklyRate is required for COMMERCIAL car types");
        return errors;
    }
}
