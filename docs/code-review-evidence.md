# Tier-1 AI code review — trust boundary and pricing rules, reviewed against .github/copilot-instructions.md on 2026-07-02

## Review: BookingService.cs & FlightAggregatorService.cs vs. copilot-instructions.md

**1. Trust boundary** ("BookingService must read price, IsInternational, and DocumentType from the cached FlightOffer, NEVER from fields the client sends")
✅ **Satisfied.** `BookingService.CreateBookingAsync` (lines 41–52) looks up `flightOffer` exclusively from `cachedResults` (via `_searchResultRepository.GetAsync(request.SearchId)`), matched by `request.FlightId`. Price and `IsInternational` are never read from `request` — line 58 passes `flightOffer.IsInternational` to the document validator, and the `Booking` is constructed at line 70 with `FlightSnapshot: flightOffer` (the cached object), not any price/international fields from the client DTO. `CreateBookingRequest` itself (not shown here) only contributes `SearchId`, `FlightId`, and `Passengers` — nothing price-related is even accepted from the client.

**2. Pricing floor logic** (`BudgetWings`: `Math.Max(baseFare * 0.90, 29.99)`, no separate `if` branch)
⚠️ **Not applicable to these two files.** Neither `BookingService.cs` nor `FlightAggregatorService.cs` contains any fare/pricing computation — that logic lives in `SkyRoute.Infrastructure/Pricing/BudgetWingsPricingStrategy.cs` and `GlobalAirPricingStrategy.cs`, outside the scope of this review. No violation present here because there's no pricing code to violate.

**3. PII logging rule** ("Never log passenger FullName, Email, or DocumentNumber. Log identifiers only")
✅ **Satisfied in both files.**
- `BookingService.cs` line 79–83: logs only `booking.Reference`, `flightOffer.Id`, and `request.Passengers.Count` — a count, not passenger content. The `foreach` loop over `request.Passengers` (lines 55–61) that touches `FullName`/`Email`/`DocumentNumber` never logs anything; failures go into `ValidationFailure` objects, not log calls.
- `FlightAggregatorService.cs` lines 42–46 and 65–68: logs `searchId`, `mergedResults.Count`, `unavailableProviders.Count > 0`, and `provider.ProviderName` — no passenger data ever passes through this service at all.

**4. Per-provider failure isolation**
✅ **Satisfied.** `SearchProviderSafelyAsync` (lines 54–77) wraps each individual `provider.SearchAsync(criteria)` call in its own `try/catch` (line 59–76), invoked once per provider via `_providers.Select(...)` (lines 29–31) and awaited together with `Task.WhenAll` (line 33). A single provider's exception is caught locally, logged as a warning (line 65), and converted to `Array.Empty<FlightOffer>()` plus an entry in the shared `unavailableProviders` list (guarded by a `lock`, lines 70–73) — so one failing provider can't fault `Task.WhenAll` or block the other providers' results from merging.

**Summary:** All applicable rules pass. The only rule with no code to check against in these two files is the pricing-floor rule, since pricing lives in the `IPricingStrategy` implementations in `SkyRoute.Infrastructure`, not in `BookingService` or `FlightAggregatorService`.
