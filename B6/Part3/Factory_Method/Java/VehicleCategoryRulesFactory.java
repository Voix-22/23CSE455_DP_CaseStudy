import java.util.List;
import java.util.Objects;

enum VehicleCategory { STANDARD, ELECTRIC, COMMERCIAL }

// Product
interface VehicleCategoryRules {
    VehicleCategory category();
    double calculateSecurityDeposit(double dailyRate);
    List<String> validateRates(double dailyRate, Double weeklyRate, Double monthlyRate);
}

// ConcreteProduct
final class StandardCategoryRules implements VehicleCategoryRules {
    @Override
    public VehicleCategory category() {
        return VehicleCategory.STANDARD;
    }

    @Override
    public double calculateSecurityDeposit(double dailyRate) {
        return 0.0;
    }

    @Override
    public List<String> validateRates(double dailyRate, Double weeklyRate, Double monthlyRate) {
        return List.of();
    }
}

// ConcreteProduct
final class ElectricCategoryRules implements VehicleCategoryRules {
    private static final double DEPOSIT_MULTIPLIER = 2.0;

    @Override
    public VehicleCategory category() {
        return VehicleCategory.ELECTRIC;
    }

    @Override
    public double calculateSecurityDeposit(double dailyRate) {
        return dailyRate * DEPOSIT_MULTIPLIER;
    }

    @Override
    public List<String> validateRates(double dailyRate, Double weeklyRate, Double monthlyRate) {
        return List.of();
    }
}

// ConcreteProduct
final class CommercialCategoryRules implements VehicleCategoryRules {
    private static final double DEPOSIT_MULTIPLIER = 5.0;

    @Override
    public VehicleCategory category() {
        return VehicleCategory.COMMERCIAL;
    }

    @Override
    public double calculateSecurityDeposit(double dailyRate) {
        return dailyRate * DEPOSIT_MULTIPLIER;
    }

    @Override
    public List<String> validateRates(double dailyRate, Double weeklyRate, Double monthlyRate) {
        if (weeklyRate == null) {
            return List.of("weeklyRate is required for COMMERCIAL car types");
        }
        return List.of();
    }
}

// Creator with the parameterized factory method: the only place that knows
// which concrete rules class belongs to which category.
public final class VehicleCategoryRulesFactory {
    public VehicleCategoryRules create(VehicleCategory category) {
        Objects.requireNonNull(category, "category must not be null");
        return switch (category) {
            case STANDARD -> new StandardCategoryRules();
            case ELECTRIC -> new ElectricCategoryRules();
            case COMMERCIAL -> new CommercialCategoryRules();
        };
    }
}
