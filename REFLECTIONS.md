# SkyRoute — Reflections

A few deliberate deviations from the brief, worth naming rather than leaving implicit:

**Stack: React + RTK Query instead of Angular.** The brief specifies Angular + .NET.
I built the frontend in React because it's the stack I can review line-by-line and
defend under questioning — a stack I'm only surface-familiar with would have meant
accepting Copilot's output on faith in exactly the areas most likely to be probed
in a live walkthrough. Happy to map any React decision here to its Angular
equivalent on request.

**Commit strategy: direct commits to main, no feature branches.** For solo work
paired with an AI CLI tool, branch-per-feature adds review ceremony with no second
reviewer to justify it. I kept commits atomic and conventionally scoped instead,
so the history itself stays legible without needing branch structure to carry that
weight — git log --oneline reads as a build order on its own.

**Code review automation: tier 1 only.** I designed a fuller version — deterministic
Roslyn/ESLint analyzers enforcing the trust-boundary and pricing-floor rules
specifically, aggregated via a CI check — but scoped it out given the timeline.
What shipped instead: GitHub Copilot as a PR reviewer, plus a documented CLI review
pass against the same rules (see docs/code-review-evidence.md) as evidence the
process actually ran, not just that it was planned.

**Application Insights: evaluated, not implemented.** I have Azure credit through
a student/org account with untested provisioning constraints, and no confidence
in how long resource creation would actually take under those constraints.
Rather than gamble the last hours of a deadline on an optional integration,
I scoped it to Serilog's console + rolling daily file sinks, which deliver the
same structured, correlated logging story without an external dependency.

**No booking cancellation.** BookingStatus ships with only Confirmed. Not an
oversight — the brief doesn't require it, and I'd rather ship a smaller surface
area fully correct than a larger one partially so.

**No persistent database.** In-memory only, and deliberately so — the mock
providers regenerate flight data on every search, so there's nothing else that
genuinely needs to survive a restart. The repository interfaces exist specifically
so this is a reversible decision, not a load-bearing one: swapping in a real store
later touches one new Infrastructure class, not BookingService, controllers, or
tests.
