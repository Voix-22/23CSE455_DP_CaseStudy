
# Refactoring record — CH02

## Code smell addressed
**Conditional/static pricing logic is difficult to vary**: booking and return-invoice flows directly call a static calculator, making it harder to add pricing variants without modifying service logic.

## Technique
- Extracted a common pricing contract (`IPricingPolicy`).
- Encapsulated algorithms in `StandardPricingPolicy`, `WeekendPricingPolicy`, and `SeasonalPricingPolicy`.
- Centralized policy choice in `PricingPolicySelector`.
- Used dependency injection and `IOptions<PricingSettings>` for policy/configuration dependencies.
- Extracted the shared billable-day calculation into `RentalDurationCalculator`.

## Behavior-preservation evidence
- Standard pricing delegates to `RentalRateCalculator.CalculateRentalAmount`, preserving the original month/week/day packing logic.
- Existing `RentalRateCalculatorTests` remain in the suite.
- The user supplied a final test output of 205 passed, 0 failed, 0 skipped.

## Trade-offs / review points
- More types and registrations are introduced, increasing navigation overhead.
- Weekend policy currently applies its configured surcharge to the total standard rental amount when at least one weekend day occurs; it does not surcharge only the weekend-day portion.
- Seasonal selection checks the pickup month only. Confirm that this matches the intended requirements before claiming a more sophisticated season-overlap rule.
- Policy-selection boundaries should be tested for pickup/return dates, month transitions, and rentals containing both peak-season and weekend days.
- Avoid claiming performance improvement without a measured benchmark.
