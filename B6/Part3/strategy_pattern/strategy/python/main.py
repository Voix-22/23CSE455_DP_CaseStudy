from dataclasses import dataclass
from datetime import timedelta
import math

@dataclass
class CarType:
    daily_rate: float
    weekly_rate: float = 0
    monthly_rate: float = 0

def days_between(pickup, ret):
    return max(1, math.ceil((ret - pickup).total_seconds() / 86400))

def base_price(car, days):
    rem, amount = days, 0.0
    if car.monthly_rate > 0 and rem >= 30:
        amount += (rem // 30) * car.monthly_rate
        rem %= 30
    if car.weekly_rate > 0 and rem >= 7:
        amount += (rem // 7) * car.weekly_rate
        rem %= 7
    return amount + rem * car.daily_rate

class Standard:
    name = "Standard"
    def calculate(self, car, p, r):
        return 0.0 if r <= p else base_price(car, days_between(p, r))

class Weekend:
    name = "Weekend"
    def __init__(self, percent=20): self.percent = percent
    def calculate(self, car, p, r):
        amount = base_price(car, days_between(p, r))
        d, end = p.date(), r.date()
        while d < end:
            if d.weekday() >= 5: return amount * (1 + self.percent / 100)
            d += timedelta(days=1)
        return amount

class Seasonal:
    name = "Seasonal"
    def __init__(self, months=(12, 1, 4), percent=30):
        self.months, self.percent = set(months), percent
    def calculate(self, car, p, r):
        amount = base_price(car, days_between(p, r))
        return amount * (1 + self.percent / 100) if p.month in self.months else amount

class Selector:
    def __init__(self, months=(12, 1, 4)):
        self.months = set(months)
        self.standard, self.weekend, self.seasonal = Standard(), Weekend(), Seasonal(months)
    def select(self, p, r):
        if p.month in self.months: return self.seasonal
        d = p.date()
        while d < r.date():
            if d.weekday() >= 5: return self.weekend
            d += timedelta(days=1)
        return self.standard

if __name__ == "__main__":
    from datetime import datetime
    car = CarType(100, 600, 2000)
    p, r = datetime(2026,2,6,10), datetime(2026,2,9,10)
    policy = Selector().select(p, r)
    assert policy.name == "Weekend" and policy.calculate(car,p,r) == 360
    print(f"{policy.name}: {policy.calculate(car,p,r):.2f}")
