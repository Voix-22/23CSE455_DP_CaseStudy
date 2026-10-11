using FlemanApi.Models;

namespace FlemanApi.Service.Categories;

// CH02 - the rules that differ from one vehicle category to another.
// One implementation per VehicleCategory, obtained through
// IVehicleCategoryRulesFactory rather than constructed by callers.
public interface IVehicleCategoryRules
{
    VehicleCategory Category { get; }

    // Refundable deposit recorded on a car type of this category.
    double CalculateSecurityDeposit(double dailyRate);

    // Category-specific rate checks; empty when the rates are acceptable.
    IReadOnlyList<string> ValidateRates(double dailyRate, double? weeklyRate, double? monthlyRate);
}
