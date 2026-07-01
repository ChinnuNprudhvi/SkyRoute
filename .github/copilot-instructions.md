# SkyRoute — Copilot instructions

Stack: React + RTK Query (frontend), .NET 8 Web API (backend), layered architecture
(SkyRoute.Domain / Application / Infrastructure / Api). No feature branches — commit
directly to main using Conventional Commits (feat/fix/test/refactor/docs/chore/ci).

## Pricing (never deviate from these exact formulas)
- GlobalAir: baseFare * 1.15, rounded to 2 decimals with MidpointRounding.AwayFromZero
- BudgetWings: Math.Max(baseFare * 0.90, 29.99) — the floor must be applied via
  Math.Max, never a separate `if` branch that could be bypassed
- Each provider's pricing lives in its own IPricingStrategy implementation. Never
  add a third provider's logic to an existing strategy class or a shared switch.

## Trust boundary (the most important rule in this codebase)
- BookingService must read price, IsInternational, and DocumentType from the
  cached FlightOffer (via ISearchResultRepository), NEVER from fields the client
  sends alongside flightId in the request body. This applies even if the client
  DTO happens to include those fields — ignore them, look them up instead.

## Repository pattern
- Services (FlightAggregatorService, BookingService) depend on ISearchResultRepository
  and IBookingRepository interfaces — never touch IMemoryCache or
  ConcurrentDictionary directly in the Application layer. Those live only inside
  the Infrastructure implementations.

## Logging
- Use Serilog structured logging with named properties, not string concatenation.
- Never log passenger FullName, Email, or DocumentNumber. Log identifiers only
  (bookingReference, flightId, searchId).

## Frontend
- Sorting in ResultsList is client-side only (useMemo over already-fetched
  results). Never add a sort parameter to the RTK Query search mutation or
  trigger a refetch on sort change.
- searchFlights is a mutation, not a query.

## General
- No code comments explaining that code was AI-generated.
- Keep controllers thin — no business logic, only DTO mapping and calls into
  Application services.
