import java.util.List;
import java.util.function.BooleanSupplier;

// Plain test runner, no framework. Prints one PASS/FAIL line per case and a
// final "PASS n/7"; exits with 1 if any case failed.
public final class TestRunner {
    private static final String WEEKLY_REQUIRED = "weeklyRate is required for COMMERCIAL car types";
    private static final int TOTAL = 7;
    private static int passed = 0;
    private static int number = 0;

    private static void check(String description, BooleanSupplier test) {
        number++;
        boolean ok;
        try {
            ok = test.getAsBoolean();
        } catch (RuntimeException e) {
            ok = false;
        }
        if (ok) {
            passed++;
        }
        System.out.println((ok ? "PASS " : "FAIL ") + number + " " + description);
    }

    public static void main(String[] args) {
        VehicleCategoryRulesFactory factory = new VehicleCategoryRulesFactory();

        check("STANDARD: standard rules, deposit(100) = 0, no rate errors", () -> {
            VehicleCategoryRules rules = factory.create(VehicleCategory.STANDARD);
            return rules instanceof StandardCategoryRules
                    && rules.calculateSecurityDeposit(100.0) == 0.0
                    && rules.validateRates(100.0, null, null).isEmpty();
        });

        check("ELECTRIC: deposit(60) = 120, no rate errors", () -> {
            VehicleCategoryRules rules = factory.create(VehicleCategory.ELECTRIC);
            return rules.calculateSecurityDeposit(60.0) == 120.0
                    && rules.validateRates(60.0, null, null).isEmpty();
        });

        check("COMMERCIAL: deposit(80) = 400", () ->
                factory.create(VehicleCategory.COMMERCIAL).calculateSecurityDeposit(80.0) == 400.0);

        check("COMMERCIAL: missing weeklyRate gives exactly one error", () -> {
            List<String> errors = factory.create(VehicleCategory.COMMERCIAL).validateRates(80.0, null, null);
            return errors.size() == 1 && errors.get(0).equals(WEEKLY_REQUIRED);
        });

        check("COMMERCIAL: weeklyRate 500 gives no errors", () ->
                factory.create(VehicleCategory.COMMERCIAL).validateRates(80.0, 500.0, null).isEmpty());

        check("every category: create(category).category() equals that category", () -> {
            for (VehicleCategory category : VehicleCategory.values()) {
                if (factory.create(category).category() != category) {
                    return false;
                }
            }
            return true;
        });

        check("unknown category (null) is rejected with an error", () -> {
            try {
                factory.create(null);
                return false;
            } catch (NullPointerException e) {
                return true;
            }
        });

        System.out.println("PASS " + passed + "/" + TOTAL);
        System.exit(passed == TOTAL ? 0 : 1);
    }
}
