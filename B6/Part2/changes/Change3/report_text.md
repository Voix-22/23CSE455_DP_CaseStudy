
# CH02 — Strategy-based rental pricing

The baseline .NET implementation calculated rental prices through a static `RentalRateCalculator` called directly from booking and return-invoice services. CH02 introduces the Strategy pattern using `IPricingPolicy` and three implementations: standard, weekend, and seasonal pricing. `PricingPolicySelector` chooses the policy using the pickup/return dates and configured peak months. The existing month/week/day calculation is retained as the standard calculation, while `Program.cs` wires the policies and settings through dependency injection.

The supplied terminal evidence records 22 passing pricing-policy tests and a final full suite of 205 passing tests with no failures. The baseline suite had 182 passes and one failure in `ProcessReturnAsync_CreatesInvoiceHeaderAndDetails`; a later user-provided run reports all 205 tests passing. These results should be verified by rerunning the suite on the exact commit submitted.

The implementation adds flexibility and separates pricing algorithms from service workflows. It also introduces additional types and configuration complexity. The current weekend policy applies a surcharge to the total rental amount if the rental includes any weekend day, while seasonal selection uses the pickup month. Those rules should be documented as deliberate acceptance criteria or refined if the intended business rule differs.
