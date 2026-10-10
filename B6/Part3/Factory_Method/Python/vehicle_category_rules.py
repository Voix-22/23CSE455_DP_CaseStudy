from abc import ABC, abstractmethod
from enum import Enum


class VehicleCategory(Enum):
    STANDARD = "STANDARD"
    ELECTRIC = "ELECTRIC"
    COMMERCIAL = "COMMERCIAL"


# Product
class VehicleCategoryRules(ABC):
    @abstractmethod
    def category(self):
        ...

    @abstractmethod
    def calculate_security_deposit(self, daily_rate):
        ...

    @abstractmethod
    def validate_rates(self, daily_rate, weekly_rate, monthly_rate):
        ...


# ConcreteProduct
class StandardCategoryRules(VehicleCategoryRules):
    def category(self):
        return VehicleCategory.STANDARD

    def calculate_security_deposit(self, daily_rate):
        return 0.0

    def validate_rates(self, daily_rate, weekly_rate, monthly_rate):
        return []


# ConcreteProduct
class ElectricCategoryRules(VehicleCategoryRules):
    DEPOSIT_MULTIPLIER = 2.0

    def category(self):
        return VehicleCategory.ELECTRIC

    def calculate_security_deposit(self, daily_rate):
        return daily_rate * self.DEPOSIT_MULTIPLIER

    def validate_rates(self, daily_rate, weekly_rate, monthly_rate):
        return []


# ConcreteProduct
class CommercialCategoryRules(VehicleCategoryRules):
    DEPOSIT_MULTIPLIER = 5.0

    def category(self):
        return VehicleCategory.COMMERCIAL

    def calculate_security_deposit(self, daily_rate):
        return daily_rate * self.DEPOSIT_MULTIPLIER

    def validate_rates(self, daily_rate, weekly_rate, monthly_rate):
        if weekly_rate is None:
            return ["weeklyRate is required for COMMERCIAL car types"]
        return []


# Creator with the parameterized factory method: the only place that knows
# which concrete rules class belongs to which category.
class VehicleCategoryRulesFactory:
    _RULES_BY_CATEGORY = {
        VehicleCategory.STANDARD: StandardCategoryRules,
        VehicleCategory.ELECTRIC: ElectricCategoryRules,
        VehicleCategory.COMMERCIAL: CommercialCategoryRules,
    }

    def create(self, category):
        if category not in self._RULES_BY_CATEGORY:
            raise ValueError(f"Unsupported vehicle category: {category!r}")
        return self._RULES_BY_CATEGORY[category]()
