
# Pattern effects — CH02 pricing policies

## Newly introduced
**Strategy** — `IPricingPolicy` defines a common price-calculation contract. `StandardPricingPolicy`, `WeekendPricingPolicy`, and `SeasonalPricingPolicy` provide interchangeable algorithms. The selector returns the policy appropriate to the booking's date window and configured peak months.

## Modified / extended
**Dependency Injection** — `Program.cs` registers pricing policies, selector, and pricing settings so services depend on abstractions rather than constructing policies directly.

**Service-layer collaboration** — `BookingService` and `StaffService` call `IPricingPolicySelector` for the booking estimate and actual return invoice. This keeps the pricing choice in one shared selection component.

**Existing pricing calculation preserved** — `RentalRateCalculator` remains the standard month/week/day calculation used by the policy implementations, rather than being deleted.

## Evidence locations
- `WanderCar_DotNet/src/FlemanApi/Util/IPricingPolicy.cs`
- `WanderCar_DotNet/src/FlemanApi/Util/StandardPricingPolicy.cs`
- `WanderCar_DotNet/src/FlemanApi/Util/WeekendPricingPolicy.cs`
- `WanderCar_DotNet/src/FlemanApi/Util/SeasonalPricingPolicy.cs`
- `WanderCar_DotNet/src/FlemanApi/Util/PricingPolicySelector.cs`
- `WanderCar_DotNet/src/FlemanApi/Service/BookingService.cs`
- `WanderCar_DotNet/src/FlemanApi/Service/StaffService.cs`
- `WanderCar_DotNet/src/FlemanApi/Program.cs`

## Caution
The Strategy classification is supported by the interface and three interchangeable implementations. Do not count the selector itself as a separate Strategy instance; describe it as the policy-selection collaborator. The selector's current precedence is seasonal when the pickup month is configured as a peak month, otherwise weekend if any day in the interval is Saturday/Sunday, otherwise standard.
