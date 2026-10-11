using FlemanApi.Models;

namespace FlemanApi.Service.Categories;

// Every car type that existed before CH02: no deposit, no extra rate rule.
public class StandardCategoryRules : IVehicleCategoryRules
{
    public VehicleCategory Category => VehicleCategory.STANDARD;

    public double CalculateSecurityDeposit(double dailyRate) => 0.0;

    public IReadOnlyList<string> ValidateRates(double dailyRate, double? weeklyRate, double? monthlyRate) =>
        Array.Empty<string>();
}
