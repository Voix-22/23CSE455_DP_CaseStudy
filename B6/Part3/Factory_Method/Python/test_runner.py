# Plain test runner, no framework. Prints one PASS/FAIL line per case and a
# final "PASS n/7"; exits with 1 if any case failed.
import sys

from vehicle_category_rules import (
    StandardCategoryRules,
    VehicleCategory,
    VehicleCategoryRulesFactory,
)

WEEKLY_REQUIRED = "weeklyRate is required for COMMERCIAL car types"
TOTAL = 7

factory = VehicleCategoryRulesFactory()
cases = []


def case(description):
    def register(test):
        cases.append((description, test))
        return test
    return register


@case("STANDARD: standard rules, deposit(100) = 0, no rate errors")
def _standard():
    rules = factory.create(VehicleCategory.STANDARD)
    return (isinstance(rules, StandardCategoryRules)
            and rules.calculate_security_deposit(100.0) == 0.0
            and rules.validate_rates(100.0, None, None) == [])


@case("ELECTRIC: deposit(60) = 120, no rate errors")
def _electric():
    rules = factory.create(VehicleCategory.ELECTRIC)
    return (rules.calculate_security_deposit(60.0) == 120.0
            and rules.validate_rates(60.0, None, None) == [])


@case("COMMERCIAL: deposit(80) = 400")
def _commercial_deposit():
    return factory.create(VehicleCategory.COMMERCIAL).calculate_security_deposit(80.0) == 400.0


@case("COMMERCIAL: missing weeklyRate gives exactly one error")
def _commercial_missing_weekly():
    errors = factory.create(VehicleCategory.COMMERCIAL).validate_rates(80.0, None, None)
    return errors == [WEEKLY_REQUIRED]


@case("COMMERCIAL: weeklyRate 500 gives no errors")
def _commercial_with_weekly():
    return factory.create(VehicleCategory.COMMERCIAL).validate_rates(80.0, 500.0, None) == []


@case("every category: create(category).category() equals that category")
def _every_category():
    return all(factory.create(category).category() is category for category in VehicleCategory)


@case("unknown category ('HOVERCRAFT') is rejected with an error")
def _unknown_category():
    try:
        factory.create("HOVERCRAFT")
    except ValueError:
        return True
    return False


def main():
    passed = 0
    for number, (description, test) in enumerate(cases, start=1):
        try:
            ok = bool(test())
        except Exception:
            ok = False
        passed += ok
        print(f"{'PASS' if ok else 'FAIL'} {number} {description}")
    print(f"PASS {passed}/{TOTAL}")
    return 0 if passed == TOTAL and len(cases) == TOTAL else 1


if __name__ == "__main__":
    sys.exit(main())
