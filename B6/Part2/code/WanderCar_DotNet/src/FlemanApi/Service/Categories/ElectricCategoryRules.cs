using FlemanApi.Models;

namespace FlemanApi.Service.Categories;

public class ElectricCategoryRules : IVehicleCategoryRules
{
    private const double DepositMultiplier = 2.0;

    public VehicleCategory Category => VehicleCategory.ELECTRIC;

    public double CalculateSecurityDeposit(double dailyRate) => dailyRate * DepositMultiplier;

    public IReadOnlyList<string> ValidateRates(double dailyRate, double? weeklyRate, double? monthlyRate) =>
        Array.Empty<string>();
}
