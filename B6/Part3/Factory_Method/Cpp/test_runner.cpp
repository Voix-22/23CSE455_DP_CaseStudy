// Plain test runner, no framework. Prints one PASS/FAIL line per case and a
// final "PASS n/7"; exits with 1 if any case failed.
#include <exception>
#include <functional>
#include <iostream>
#include <string>

#include "vehicle_category_rules.hpp"

namespace {

const std::string kWeeklyRequired = "weeklyRate is required for COMMERCIAL car types";
const int kTotal = 7;
int passed = 0;
int number = 0;

void check(const std::string& description, const std::function<bool()>& test) {
    ++number;
    bool ok = false;
    try {
        ok = test();
    } catch (const std::exception&) {
        ok = false;
    }
    if (ok) {
        ++passed;
    }
    std::cout << (ok ? "PASS " : "FAIL ") << number << " " << description << "\n";
}

}  // namespace

int main() {
    const VehicleCategoryRulesFactory factory;

    check("STANDARD: standard rules, deposit(100) = 0, no rate errors", [&] {
        const auto rules = factory.create(VehicleCategory::STANDARD);
        return dynamic_cast<const StandardCategoryRules*>(rules.get()) != nullptr
            && rules->calculateSecurityDeposit(100.0) == 0.0
            && rules->validateRates(100.0, std::nullopt, std::nullopt).empty();
    });

    check("ELECTRIC: deposit(60) = 120, no rate errors", [&] {
        const auto rules = factory.create(VehicleCategory::ELECTRIC);
        return rules->calculateSecurityDeposit(60.0) == 120.0
            && rules->validateRates(60.0, std::nullopt, std::nullopt).empty();
    });

    check("COMMERCIAL: deposit(80) = 400", [&] {
        return factory.create(VehicleCategory::COMMERCIAL)->calculateSecurityDeposit(80.0) == 400.0;
    });

    check("COMMERCIAL: missing weeklyRate gives exactly one error", [&] {
        const auto errors = factory.create(VehicleCategory::COMMERCIAL)
                                ->validateRates(80.0, std::nullopt, std::nullopt);
        return errors.size() == 1 && errors[0] == kWeeklyRequired;
    });

    check("COMMERCIAL: weeklyRate 500 gives no errors", [&] {
        return factory.create(VehicleCategory::COMMERCIAL)
            ->validateRates(80.0, 500.0, std::nullopt).empty();
    });

    check("every category: create(category).category() equals that category", [&] {
        for (const auto category : {VehicleCategory::STANDARD, VehicleCategory::ELECTRIC,
                                    VehicleCategory::COMMERCIAL}) {
            if (factory.create(category)->category() != category) {
                return false;
            }
        }
        return true;
    });

    check("unknown category (static_cast<VehicleCategory>(999)) is rejected with an error", [&] {
        try {
            factory.create(static_cast<VehicleCategory>(999));
            return false;
        } catch (const std::invalid_argument&) {
            return true;
        }
    });

    std::cout << "PASS " << passed << "/" << kTotal << "\n";
    return passed == kTotal && number == kTotal ? 0 : 1;
}
