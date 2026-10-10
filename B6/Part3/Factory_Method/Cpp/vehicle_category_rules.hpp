#ifndef VEHICLE_CATEGORY_RULES_HPP
#define VEHICLE_CATEGORY_RULES_HPP

#include <memory>
#include <optional>
#include <stdexcept>
#include <string>
#include <vector>

enum class VehicleCategory { STANDARD, ELECTRIC, COMMERCIAL };

// Product
class VehicleCategoryRules {
public:
    virtual ~VehicleCategoryRules() = default;
    virtual VehicleCategory category() const = 0;
    virtual double calculateSecurityDeposit(double dailyRate) const = 0;
    virtual std::vector<std::string> validateRates(double dailyRate,
                                                   std::optional<double> weeklyRate,
                                                   std::optional<double> monthlyRate) const = 0;
};

// ConcreteProduct
class StandardCategoryRules final : public VehicleCategoryRules {
public:
    VehicleCategory category() const override { return VehicleCategory::STANDARD; }

    double calculateSecurityDeposit(double) const override { return 0.0; }

    std::vector<std::string> validateRates(double, std::optional<double>,
                                           std::optional<double>) const override {
        return {};
    }
};

// ConcreteProduct
class ElectricCategoryRules final : public VehicleCategoryRules {
public:
    VehicleCategory category() const override { return VehicleCategory::ELECTRIC; }

    double calculateSecurityDeposit(double dailyRate) const override {
        return dailyRate * kDepositMultiplier;
    }

    std::vector<std::string> validateRates(double, std::optional<double>,
                                           std::optional<double>) const override {
        return {};
    }

private:
    static constexpr double kDepositMultiplier = 2.0;
};

// ConcreteProduct
class CommercialCategoryRules final : public VehicleCategoryRules {
public:
    VehicleCategory category() const override { return VehicleCategory::COMMERCIAL; }

    double calculateSecurityDeposit(double dailyRate) const override {
        return dailyRate * kDepositMultiplier;
    }

    std::vector<std::string> validateRates(double, std::optional<double> weeklyRate,
                                           std::optional<double>) const override {
        if (!weeklyRate.has_value()) {
            return {"weeklyRate is required for COMMERCIAL car types"};
        }
        return {};
    }

private:
    static constexpr double kDepositMultiplier = 5.0;
};

// Creator with the parameterized factory method: the only place that knows
// which concrete rules class belongs to which category.
class VehicleCategoryRulesFactory {
public:
    std::unique_ptr<VehicleCategoryRules> create(VehicleCategory category) const {
        switch (category) {
            case VehicleCategory::STANDARD:
                return std::make_unique<StandardCategoryRules>();
            case VehicleCategory::ELECTRIC:
                return std::make_unique<ElectricCategoryRules>();
            case VehicleCategory::COMMERCIAL:
                return std::make_unique<CommercialCategoryRules>();
        }
        throw std::invalid_argument("Unsupported vehicle category: " +
                                    std::to_string(static_cast<int>(category)));
    }
};

#endif
