
# CH02 — Add interchangeable rental pricing policies

**Owner:** Neema Vinod  
**Change ID:** CH02 / Change2  
**Baseline commit:** `dfc61ab8dd6f9bbb2301171457dc03946a249576`  
**Implementation state in supplied ZIP:** Present in the working tree; not yet committed in the supplied Git metadata.

## Requirement
Replace the direct dependency on the static `RentalRateCalculator` in booking and return-invoice workflows with interchangeable pricing policies, while retaining the existing month/week/day rate calculation as the standard policy.

## Acceptance criteria and observed evidence
- [x] A common `IPricingPolicy` contract exists.
- [x] A standard policy delegates to the existing month/week/day calculator.
- [x] Weekend and seasonal pricing policies exist.
- [x] `PricingPolicySelector` selects a policy based on the pickup/return date window and configured peak months.
- [x] Booking creation/modification and staff return invoicing use the selector in the supplied source.
- [x] Pricing settings are bound through configuration and policies/selector are registered in dependency injection.
- [x] Pricing-focused tests were added.
- [x] User-reported full test run: 205 passed, 0 failed, 0 skipped.
- [ ] Commit hashes for the completed implementation have not yet been recorded.
- [ ] Human reviewer should verify the diff and rerun the commands from `test_result.txt` in the final repository checkout.

## Source scope
`WanderCar_DotNet/src/FlemanApi/Util/` — pricing policy abstractions, implementations, selector, settings and duration helper.  
`WanderCar_DotNet/src/FlemanApi/Service/BookingService.cs` and `StaffService.cs` — policy integration.  
`WanderCar_DotNet/src/FlemanApi/Program.cs` — configuration and dependency injection.  
`WanderCar_DotNet/src/FlemanApi.Tests/Util/` and service tests — tests and constructor wiring.
