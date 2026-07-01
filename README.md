# SkyRoute — Flight Search & Booking

A flight search and booking module for a travel aggregator platform, built as a
technical assessment. Aggregates mock results from two airline providers
(GlobalAir, BudgetWings), each with its own pricing rule, and supports an
end-to-end search-to-booking flow.

## Stack

- Backend: .NET 8 Web API, layered architecture (Domain / Application /
  Infrastructure / Api)
- Frontend: React + RTK Query
- Persistence: in-memory (see Architecture Decisions — no database required for
  this scope)
- Logging: Serilog (see Architecture Decisions)

> **Note on stack:** the original brief specified Angular + .NET. This build
> uses React + RTK Query instead, since it's the stack I can review and defend
> in depth — happy to walk through the Angular-equivalent mapping live.

## Setup & run

### Backend
```bash
cd src/SkyRoute.Api
dotnet run
```
Swagger UI available at `https://localhost:<port>/swagger` once running.

### Frontend
```bash
cd client
npm install
npm run dev
```
Runs at `http://localhost:5173` by default.

### Tests
```bash
dotnet test
```

## Architecture decisions

_(filled in as each piece is built — see DEVLOG.md for the running commentary)_

- Pricing logic is isolated per provider via `IPricingStrategy`, so onboarding
  a new airline provider doesn't require touching existing pricing code.
- Booking price and document-type are read from a server-side search cache
  (`ISearchResultRepository`), never trusted from the client request — closes
  an obvious integrity gap since mock providers regenerate data per search.
- Repository interfaces (`ISearchResultRepository`, `IBookingRepository`) wrap
  in-memory storage today, so swapping to a real database later is additive,
  not a rewrite.

## Trade-offs & known limitations

- **No persistent database.** In-memory storage only — booking data doesn't
  survive an API restart. Given the scope (mock providers regenerate data per
  search, nothing else needs long-term persistence), this was a deliberate
  choice, not an oversight. A repository abstraction is already in place so
  swapping in a real database is additive.
- **No booking cancellation / refund flow.** `BookingStatus` ships with only
  `Confirmed`. With more time, I'd add `Cancelled` and the associated flow.
- **Code review automation covers GitHub Copilot PR review only** (not a
  custom AI review agent) — a deliberate scope cut given the deadline.

## AI-assisted development

Built with GitHub Copilot CLI, working incrementally with a shared
`.github/copilot-instructions.md` for consistent context. See `DEVLOG.md` for
a running log of what was built, what Copilot generated vs. what needed
correction, and the reasoning behind each decision.
