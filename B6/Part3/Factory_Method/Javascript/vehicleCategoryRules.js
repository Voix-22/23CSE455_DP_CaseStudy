'use strict';

const VehicleCategory = Object.freeze({
  STANDARD: 'STANDARD',
  ELECTRIC: 'ELECTRIC',
  COMMERCIAL: 'COMMERCIAL',
});

// Product. JavaScript has no interface construct: the contract is these three
// method names, and the base class only makes a missing override fail loudly.
class VehicleCategoryRules {
  category() {
    throw new Error('category() is not implemented');
  }

  calculateSecurityDeposit(dailyRate) {
    throw new Error('calculateSecurityDeposit() is not implemented');
  }

  validateRates(dailyRate, weeklyRate, monthlyRate) {
    throw new Error('validateRates() is not implemented');
  }
}

// ConcreteProduct
class StandardCategoryRules extends VehicleCategoryRules {
  category() {
    return VehicleCategory.STANDARD;
  }

  calculateSecurityDeposit(dailyRate) {
    return 0;
  }

  validateRates(dailyRate, weeklyRate, monthlyRate) {
    return [];
  }
}

// ConcreteProduct
class ElectricCategoryRules extends VehicleCategoryRules {
  static DEPOSIT_MULTIPLIER = 2;

  category() {
    return VehicleCategory.ELECTRIC;
  }

  calculateSecurityDeposit(dailyRate) {
    return dailyRate * ElectricCategoryRules.DEPOSIT_MULTIPLIER;
  }

  validateRates(dailyRate, weeklyRate, monthlyRate) {
    return [];
  }
}

// ConcreteProduct
class CommercialCategoryRules extends VehicleCategoryRules {
  static DEPOSIT_MULTIPLIER = 5;

  category() {
    return VehicleCategory.COMMERCIAL;
  }

  calculateSecurityDeposit(dailyRate) {
    return dailyRate * CommercialCategoryRules.DEPOSIT_MULTIPLIER;
  }

  validateRates(dailyRate, weeklyRate, monthlyRate) {
    if (weeklyRate == null) {
      return ['weeklyRate is required for COMMERCIAL car types'];
    }
    return [];
  }
}

// Creator with the parameterized factory method: the only place that knows
// which concrete rules class belongs to which category.
class VehicleCategoryRulesFactory {
  create(category) {
    switch (category) {
      case VehicleCategory.STANDARD:
        return new StandardCategoryRules();
      case VehicleCategory.ELECTRIC:
        return new ElectricCategoryRules();
      case VehicleCategory.COMMERCIAL:
        return new CommercialCategoryRules();
      default:
        throw new RangeError(`Unsupported vehicle category: ${category}`);
    }
  }
}

module.exports = {
  VehicleCategory,
  VehicleCategoryRules,
  StandardCategoryRules,
  ElectricCategoryRules,
  CommercialCategoryRules,
  VehicleCategoryRulesFactory,
};
