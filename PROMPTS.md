# SkyRoute — Copilot CLI Prompts

The actual prompts used to drive Copilot CLI through this build, in the order
they were run. Kept verbatim as a record of the AI-driven development process,
not a summary of it — this is what was typed, not a paraphrase of what happened.

---

## Domain models

In SkyRoute.Domain, create these files as plain C# records/classes with
no framework dependencies (no EF attributes, no JSON attributes, no using statements
outside System) — this is the domain layer:

- Enums/CabinClass.cs — enum with values Economy, Business, First
- Enums/BookingStatus.cs — enum with just Confirmed for now
- Enums/DocumentType.cs — enum with values PassportNumber, NationalId
- Entities/Airport.cs — Code, Name, City, Country (all string)
- Entities/FlightOffer.cs — Id, Provider, FlightNumber (string), Origin, Destination
  (both Airport), DepartureTime, ArrivalTime (DateTime), CabinClass (enum above),
  BaseFare, TotalPrice, PricePerPerson (decimal), plus a computed DurationMinutes
  property (ArrivalTime - DepartureTime in minutes) and a computed IsInternational
  property (true when Origin.Country != Destination.Country)
- Entities/Passenger.cs — FullName, Email, DocumentNumber (all string)
- Entities/Booking.cs — Reference (string), FlightSnapshot (FlightOffer),
  Passengers (List<Passenger>), Status (BookingStatus), CreatedAt (DateTime)

Do not add any logic beyond the two computed properties on FlightOffer.

---

## Pricing strategies — GlobalAir

In SkyRoute.Application, create Interfaces/IPricingStrategy.cs:

public interface IPricingStrategy
{
    decimal CalculatePrice(decimal baseFare);
}

In SkyRoute.Infrastructure, create Pricing/GlobalAirPricingStrategy.cs implementing
IPricingStrategy: return baseFare * 1.15m, rounded to 2 decimal places using
Math.Round with MidpointRounding.AwayFromZero.

In SkyRoute.Tests, create Pricing/GlobalAirPricingStrategyTests.cs with xunit:
- a normal case (e.g. baseFare 100 -> expect 115.00)
- a rounding-boundary case where the unrounded result lands exactly on a
  half-cent (e.g. a baseFare that produces X.XX5) to prove AwayFromZero rounding
  is actually being used, not banker's rounding

---

## Pricing strategies — BudgetWings

In SkyRoute.Infrastructure, create Pricing/BudgetWingsPricingStrategy.cs implementing
IPricingStrategy: return Math.Max(baseFare * 0.90m, 29.99m) — apply the floor via
Math.Max, not a separate if-statement.

In SkyRoute.Tests, create Pricing/BudgetWingsPricingStrategyTests.cs with xunit:
- a normal case where the discount is well above the floor
- an edge case where baseFare is low enough that baseFare * 0.90 would be below
  29.99, asserting the result is exactly 29.99

---

## Provider adapters — JSON-backed mock data

Implement mock flight data for GlobalAirProvider and BudgetWingsProvider by simulating
reading from a real provider API — not by generating random data inline in C#. This
should make it obvious that swapping in a real HTTP call later is a small, contained
change, not a rewrite.

1. Create SkyRoute.Infrastructure/Providers/MockData/globalair-flights.json — a JSON
   array representing GlobalAir's own raw API response shape: flightNo (string),
   route: { from, to } (airport codes), departOffsetMinutes (int, minutes after
   midnight), durationMinutes (int), cabin (string: "Economy"/"Business"/"First"),
   fare (decimal). Generate entries covering every ordered pair among these 6 airport
   codes: JFK, LAX, ORD, LHR, DEL, BOM (30 ordered pairs), 1-2 flights per pair, with
   plausible durations per route (e.g. JFK-LHR ~420-460 min, JFK-LAX ~330-360 min,
   DEL-BOM ~130-150 min) and fares in a believable range (80-600).

2. Create SkyRoute.Infrastructure/Providers/MockData/budgetwings-flights.json — same
   route coverage, but with BudgetWings' own different raw shape to prove the adapter
   is genuinely normalizing different schemas, not just relabeling one: flight_number
   (string), origin_code, destination_code (strings), departure_offset_minutes (int),
   duration_minutes (int), cabin_class (string, UPPERCASE), base_fare_usd (decimal).

3. In SkyRoute.Infrastructure.csproj, add an ItemGroup so both JSON files are copied
   to the output directory (CopyToOutputDirectory="PreserveNewest").

4. Create GlobalAirProvider.cs implementing IFlightProvider (ProviderName = "GlobalAir"):
   - A private record GlobalAirRawFlight matching the JSON shape (System.Text.Json
     with JsonPropertyName attributes matching the raw field names)
   - A private async method LoadRawFlightsAsync() that reads and deserializes
     globalair-flights.json from disk. Directly above it, add the comment: "// Swap
     this method for an HttpClient call to the real GlobalAir search endpoint —
     everything below this line (mapping, pricing) stays unchanged."
   - SearchAsync: call LoadRawFlightsAsync(), filter entries where route.from/route.to
     match the requested origin/destination (case-insensitive), map each match to a
     domain FlightOffer (build DepartureTime/ArrivalTime from criteria.DepartureDate +
     the offset/duration, look up Origin/Destination from AirportSeedData, parse the
     cabin string to CabinClass), then call GlobalAirPricingStrategy.CalculatePrice on
     the raw fare for TotalPrice, divided by Passengers for PricePerPerson.
   - Constructor takes IPricingStrategy (not a concrete type).

5. Create BudgetWingsProvider.cs the same way against budgetwings-flights.json, with
   its own raw DTO matching that file's field names/casing, ProviderName = "BudgetWings".

---

## Repositories and search cache

In SkyRoute.Application/Interfaces, create:

public interface ISearchResultRepository
{
    Task<string> SaveAsync(IEnumerable<FlightOffer> results, TimeSpan ttl);
    Task<IEnumerable<FlightOffer>?> GetAsync(string searchId);
}

public interface IBookingRepository
{
    Task SaveAsync(Booking booking);
    Task<Booking?> GetByReferenceAsync(string reference);
}

In SkyRoute.Infrastructure/Repositories, create:
- InMemorySearchResultRepository.cs implementing ISearchResultRepository, wrapping
  IMemoryCache. SaveAsync generates a short opaque searchId (not derived from the
  search criteria), stores the results under that key with the given ttl, returns
  the searchId. GetAsync returns null if expired/missing.
- InMemoryBookingRepository.cs implementing IBookingRepository, wrapping a
  ConcurrentDictionary<string, Booking> keyed by booking reference.

In SkyRoute.Infrastructure, create DependencyInjection.cs — a static class with one
extension method AddSkyRouteInfrastructure(this IServiceCollection services) that
registers IMemoryCache, both repositories, and both flight providers (GlobalAirProvider,
BudgetWingsProvider as IFlightProvider) — so Program.cs only needs to call this one method.

---

## Aggregator + booking service — resilience, trust boundary, FluentValidation

In SkyRoute.Domain/Exceptions, create:
- SearchExpiredException.cs — thrown when a searchId isn't found or has expired
- FlightNotFoundException.cs — thrown when a flightId isn't found within a valid search
Simple exceptions, message constructor only.

In SkyRoute.Application/Models, create:
- CreateBookingRequest.cs — SearchId, FlightId (string), Passengers (List<PassengerDto>)
- PassengerDto.cs — FullName, Email, DocumentNumber (string)

In SkyRoute.Application/Validators, create using FluentValidation:
- CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest> — SearchId
  and FlightId NotEmpty, Passengers NotEmpty, RuleForEach(Passengers) using a
  PassengerDtoValidator (FullName NotEmpty, Email NotEmpty + EmailAddress(),
  DocumentNumber NotEmpty). This is structural validation only — it runs before
  we know whether the route is international.
- PassengerDocumentFormatValidator : AbstractValidator<(PassengerDto Passenger, bool
  IsInternational)> — a single DocumentNumber rule expressed with .When(): when
  IsInternational is true, Matches(@"^[A-Za-z0-9]{6,9}$") with message "Passport
  Number must be 6-9 alphanumeric characters"; when false, Matches(@"^\d{6,12}$")
  with message "National ID must be 6-12 digits". No if/else — use FluentValidation's
  conditional .When() exclusively for the branching.

In SkyRoute.Application/Models, create FlightSearchResult.cs — a record with SearchId
(string), Results (IEnumerable<FlightOffer>), IsInternational (bool), UnavailableProviders
(IReadOnlyList<string>).

In SkyRoute.Application/Services, create FlightAggregatorService.cs:
- Constructor: IEnumerable<IFlightProvider>, ISearchResultRepository, ILogger<FlightAggregatorService>
- SearchAsync(FlightSearchCriteria criteria) returns FlightSearchResult
- Fan out to all providers in parallel. Wrap each provider call so a failure does
  NOT fail the others: on exception, log a warning with the provider name, treat
  that provider's contribution as an empty list, and record the provider's name
  in an UnavailableProviders list. Task.WhenAll runs only over tasks that can no
  longer throw.
- Merge all providers' offers into one list
- Determine IsInternational once from the origin/destination airports (Country
  differs), not per-offer
- Save merged results via ISearchResultRepository.SaveAsync, 15-minute TTL, get
  back the generated searchId
- Return FlightSearchResult with all four fields populated — an empty Results list
  (whether from all providers failing or a genuinely empty market) is NOT an error,
  just return it normally

In SkyRoute.Application/Services, create BookingService.cs:
- Constructor: ISearchResultRepository, IBookingRepository,
  IValidator<CreateBookingRequest>, PassengerDocumentFormatValidator
- CreateBookingAsync(CreateBookingRequest request):
  1. Validate request structurally with IValidator<CreateBookingRequest> first —
     throw FluentValidation's ValidationException on failure
  2. Look up ISearchResultRepository.GetAsync(request.SearchId) — throw
     SearchExpiredException if null
  3. Find the FlightOffer with matching Id to request.FlightId in the cached
     results — throw FlightNotFoundException if not found
  4. Read TotalPrice, PricePerPerson, and IsInternational from THAT cached
     FlightOffer only — never from the request
  5. For each passenger, run PassengerDocumentFormatValidator against
     (passenger, flightOffer.IsInternational) — throw ValidationException
     aggregating any failures
  6. Generate booking reference: "SKY-" + 6 random uppercase alphanumeric characters
  7. Build and save a Booking via IBookingRepository.SaveAsync — Status =
     Confirmed, CreatedAt = DateTime.UtcNow, FlightSnapshot = the cached offer,
     Passengers mapped from the DTOs
  8. Return the saved Booking

---

## BookingService tests — trust boundary proof

In SkyRoute.Tests/Services, create BookingServiceTests.cs using xunit and Moq. Add
a private helper method MakeCachedSearch(bool isInternational) that builds a single
FlightOffer with a known TotalPrice (e.g. 450.00m), PricePerPerson (e.g. 225.00m),
and Origin/Destination airports chosen so IsInternational matches the parameter —
returns the FlightOffer and a fixed searchId string.

Tests to include:

1. CreateBookingAsync_ReadsPriceFromCachedOffer_NeverFromRequest — build a
   CreateBookingRequest with a valid searchId/flightId, mock
   ISearchResultRepository.GetAsync to return the cached offer from the helper
   (TotalPrice 450.00m), mock IBookingRepository.SaveAsync to capture the Booking
   passed to it via Moq Callback. Call CreateBookingAsync, then assert the captured
   Booking's FlightSnapshot.TotalPrice equals exactly 450.00m.

2. CreateBookingAsync_SearchIdNotFound_ThrowsSearchExpiredException — mock
   GetAsync to return null, assert CreateBookingAsync throws SearchExpiredException.

3. CreateBookingAsync_FlightIdNotInCachedSearch_ThrowsFlightNotFoundException —
   mock GetAsync to return a cached search containing a different flightId than
   the one requested, assert FlightNotFoundException is thrown.

4. CreateBookingAsync_InternationalRoute_RejectsNationalIdShapedDocument — use the
   helper with isInternational=true, build a passenger with a DocumentNumber shaped
   like a National ID (e.g. "123456789012", all digits), assert CreateBookingAsync
   throws FluentValidation.ValidationException.

5. CreateBookingAsync_DomesticRoute_RejectsPassportShapedDocument — use the helper
   with isInternational=false, build a passenger with a DocumentNumber shaped like
   a Passport (e.g. "AB1234567", mixed alphanumeric), assert
   FluentValidation.ValidationException is thrown.

6. CreateBookingAsync_ValidRequest_GeneratesReferenceWithSkyPrefixAndSavesConfirmedStatus
   — happy path: valid searchId/flightId, correctly-shaped document for the route,
   assert the returned Booking has Reference starting with "SKY-", Status ==
   BookingStatus.Confirmed, and IBookingRepository.SaveAsync was called exactly once.

Use the real CreateBookingRequestValidator and PassengerDocumentFormatValidator
instances in these tests, not mocks — only ISearchResultRepository and
IBookingRepository should be Moq mocks.

---

## API layer — DTOs, mapping, exception middleware, controllers

In SkyRoute.Infrastructure/Providers/AirportSeedData.cs, add a public static method
GetAll() returning IReadOnlyCollection<Airport> — all entries from the existing
Airports dictionary.

In SkyRoute.Api/Models, create response/request DTOs (plain C# records, System.Text.Json
naming — camelCase on the wire):
- AirportDto: Code, Name, City, Country (string)
- FlightSearchRequestDto: OriginCode, DestinationCode (string), DepartureDate (DateOnly),
  Passengers (int), CabinClass (string)
- FlightOfferResponseDto: FlightId, Provider, FlightNumber (string), DepartureTime,
  ArrivalTime (DateTime), DurationMinutes (int), CabinClass (string), TotalPrice,
  PricePerPerson (decimal), Currency (string, always "USD")
- FlightSearchResponseDto: SearchId (string), IsInternational (bool),
  Results (List<FlightOfferResponseDto>), PartialResults (bool),
  Notice (string?) — Notice is only set when PartialResults is true, to a fixed
  generic message like "Some results may be temporarily limited." Never include
  raw provider names in this DTO.
- BookingResponseDto: BookingReference, Status (string), TotalPrice (decimal),
  FlightSummary (a small nested object: Provider, FlightNumber, Origin, Destination
  as strings, DepartureTime, ArrivalTime, CabinClass as string)

In SkyRoute.Api/Mapping, create a static MappingExtensions.cs with extension methods:
- Airport.ToDto()
- FlightOffer.ToDto() -> FlightOfferResponseDto
- FlightSearchRequestDto.ToCriteria() -> FlightSearchCriteria (parse CabinClass string
  to the enum)
- FlightSearchResult.ToResponseDto() -> FlightSearchResponseDto (PartialResults =
  UnavailableProviders.Any(), Notice set only when PartialResults is true)
- Booking.ToResponseDto() -> BookingResponseDto

In SkyRoute.Api/Middleware, create ExceptionHandlingMiddleware.cs — standard ASP.NET
Core middleware (InvokeAsync taking RequestDelegate) that catches exceptions and
writes a ProblemDetails response:
- SearchExpiredException -> 410 Gone, title "Search expired"
- FlightNotFoundException -> 404 Not Found, title "Flight not found"
- FluentValidation.ValidationException -> 400 Bad Request, title "Validation failed",
  with the validation Errors serialized into the ProblemDetails Extensions
  dictionary under key "errors" (group by property name)
- Any other exception -> 500 Internal Server Error, title "An unexpected error
  occurred" (log the real exception via ILogger, don't leak its message to the
  response body)

In SkyRoute.Api/Controllers, create three thin controllers (constructor-inject the
application services, no business logic in the controller bodies):
- AirportsController: GET /api/airports -> AirportSeedData.GetAll() mapped to
  List<AirportDto>
- FlightsController: POST /api/flights/search, body FlightSearchRequestDto ->
  map to FlightSearchCriteria, call FlightAggregatorService.SearchAsync, map
  result to FlightSearchResponseDto, return 200
- BookingsController: POST /api/bookings, body CreateBookingRequest -> call
  BookingService.CreateBookingAsync, map to BookingResponseDto, return 201.
  Also add GET /api/bookings/{reference} -> IBookingRepository.GetByReferenceAsync,
  return the mapped DTO or 404 if null.

Update Program.cs:
- builder.Services.AddSkyRouteInfrastructure()
- Register FluentValidation validators: builder.Services.AddScoped<IValidator<CreateBookingRequest>, CreateBookingRequestValidator>()
  and register PassengerDocumentFormatValidator the same way
- builder.Services.AddScoped<FlightAggregatorService>() and AddScoped<BookingService>()
- builder.Services.AddControllers()
- builder.Services.AddSwaggerGen() and app.UseSwagger() / app.UseSwaggerUI() in
  Development
- CORS: builder.Services.AddCors with a named policy allowing origin
  http://localhost:5173, AllowAnyHeader, AllowAnyMethod — app.UseCors before
  app.UseAuthorization
- app.UseMiddleware<ExceptionHandlingMiddleware>() registered early in the pipeline,
  before routing
- app.MapControllers()

---

## Logging — Serilog, correlation ID

1. In SkyRoute.Api/Program.cs, wire Serilog as the logging provider:
   - builder.Host.UseSerilog((ctx, cfg) => cfg
       .Enrich.FromLogContext()
       .WriteTo.Console(outputTemplate:
         "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}"))
   This must be called before builder.Build().

2. In SkyRoute.Api/Middleware, create CorrelationIdMiddleware.cs:
   - InvokeAsync(HttpContext context, RequestDelegate next)
   - Read "X-Correlation-Id" from request headers; if missing/empty, generate a
     new Guid ("N" format, no dashes)
   - Push it into Serilog's LogContext via
     Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId)
     for the duration of the request (use a using block around the rest of the
     pipeline call)
   - Set context.Response.Headers["X-Correlation-Id"] = correlationId before
     calling next(), so it's present even if a later middleware throws

3. In Program.cs, register middleware in this exact order: CorrelationIdMiddleware
   first, then ExceptionHandlingMiddleware, then UseCors, then routing/
   MapControllers.

4. In SkyRoute.Application/Services/FlightAggregatorService.cs, after the merged
   results are saved and searchId is generated, add one Information-level log:
   logger.LogInformation("Flight search completed: {SearchId} {ResultCount}
   {PartialResults}", searchId, results.Count, unavailableProviders.Count > 0).

5. In SkyRoute.Application/Services/BookingService.cs, add ILogger<BookingService>
   to the constructor. After a booking is successfully saved, add one
   Information-level log: logger.LogInformation("Booking confirmed:
   {BookingReference} {FlightId} {PassengerCount}", booking.Reference,
   flightOffer.Id, passengers.Count). Do NOT log FullName, Email, or
   DocumentNumber anywhere in this file or any other file.

6. In SkyRoute.Api/Middleware/ExceptionHandlingMiddleware.cs, add
   ILogger<ExceptionHandlingMiddleware> to the constructor, and in the catch-all
   branch, add logger.LogError(exception, "Unhandled exception handling request")
   before writing the ProblemDetails response.

---

## Logging — file sink and automatic request logging

1. In SkyRoute.Api/Program.cs, extend the existing Serilog config to add a
   second sink alongside Console:
   .WriteTo.File(
       path: "logs/skyroute-.log",
       rollingInterval: RollingInterval.Day,
       outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}")
   Keep the existing Console sink and Enrich.FromLogContext() exactly as they are.

2. Directly after app.UseMiddleware<CorrelationIdMiddleware>() and before
   app.UseMiddleware<ExceptionHandlingMiddleware>() in the pipeline, add:
   app.UseSerilogRequestLogging();

3. Add "logs/" as a new line to the root .gitignore file.

---

## Logging fix — missing business event log calls

1. In SkyRoute.Application/Services/BookingService.cs: add a private readonly
   ILogger<BookingService> _logger field, add ILogger<BookingService> logger as
   a constructor parameter, assign it in the constructor body alongside the
   existing dependencies. Do NOT modify Program.cs.

   Immediately after the booking is successfully saved via
   IBookingRepository.SaveAsync and before the method returns, add:
   _logger.LogInformation("Booking confirmed: {BookingReference} {FlightId}
   {PassengerCount}", booking.Reference, flightOffer.Id, request.Passengers.Count);

2. In SkyRoute.Application/Services/FlightAggregatorService.cs: the ILogger
   field already exists. Immediately after ISearchResultRepository.SaveAsync
   returns the generated searchId, and before the method returns its result,
   add: _logger.LogInformation("Flight search completed: {SearchId}
   {ResultCount} {PartialResults}", searchId, mergedResults.Count,
   unavailableProviders.Count > 0);

---

## Frontend — store, types, API health gate

This is a React + TypeScript + Vite + Redux Toolkit + RTK Query + MUI project. Do
NOT use Tailwind anywhere. Do NOT install @mui/x-date-pickers.

1. In vite.config.ts, add a dev server proxy: any request to "/api" forwards to
   process.env.VITE_API_TARGET (read via loadEnv), with changeOrigin: true and
   secure: false.

2. In src/main.tsx, wrap the app in MUI's ThemeProvider plus CssBaseline, the
   Redux Provider, and a top-level ErrorBoundary component.

3. In src/shared/types.ts, define TypeScript interfaces mirroring the backend
   DTOs exactly, camelCase field names (Airport, FlightSearchRequest, FlightOffer,
   FlightSearchResponse, PassengerInput, CreateBookingRequest, BookingResponse).

4. In src/features/api/skyRouteApi.ts, create the RTK Query api slice:
   getAirports (query), searchFlights (mutation), createBooking (mutation).

5. In src/features/bookingFlow/bookingFlowSlice.ts, create a slice with state:
   activeStep, searchId, isInternational, results, partialResults, notice,
   selectedFlightId, passengers. Use extraReducers to listen for
   skyRouteApi.endpoints.searchFlights.matchFulfilled and populate state,
   resetting activeStep to 1. Include reducers: setActiveStep, selectFlight
   (advances to step 2), setPassengers, resetFlow.

6. In src/features/ui/uiSlice.ts, create a slice with snackbar state and
   showSnackbar/hideSnackbar reducers.

7. In src/app/store.ts, configureStore combining all reducers plus
   skyRouteApi.middleware.

8. In src/shared/ServiceUnavailable.tsx, a full-page MUI component with a
   Retry button.

9. In src/shared/ErrorBoundary.tsx, a class component rendering
   ServiceUnavailable on catch.

10. In src/shared/ApiHealthGate.tsx, a component using useGetAirportsQuery():
    isLoading -> full-page spinner, isError (network-level) -> ServiceUnavailable
    with retry, otherwise -> children.

11. In src/shared/GlobalSnackbar.tsx, reads ui slice snackbar state, renders
    MUI Snackbar + Alert.

12. In src/App.tsx, render ApiHealthGate wrapping a placeholder div and
    GlobalSnackbar as a sibling.

---

## Frontend — search form and results list

1. bookingFlowSlice.ts — a way to derive the selected FlightOffer from
   selectedFlightId + results.

2. BookingStepper.tsx — MUI Stepper, 4 steps (Search, Select Flight, Passenger
   Details, Confirmation), reads activeStep from Redux, renders the right
   component per step.

3. SearchForm.tsx — MUI Selects for Origin/Destination populated via
   useGetAirportsQuery, date TextField, passengers number field (1-9), cabin
   class Select. Client-side validation: origin != destination. On submit,
   calls useSearchFlightsMutation(). Does not manually set activeStep.

4. ResultsList.tsx — reads results/partialResults/notice/isInternational from
   Redux. Shows snackbar on partialResults. Empty state for zero results
   (not an error). MUI Table with TableSortLabel columns (Provider, Flight,
   Duration, Price), local useState sort, useMemo-computed sorted rows — no
   network call on sort change. Row click dispatches selectFlight. "New Search"
   button dispatches resetFlow.

5. Wire BookingStepper into App.tsx.

---

## Frontend — branded header, stepper icons, texture, calendar icon

1. In src/theme.ts, extend the theme: aviation-blue primary palette, soft
   off-white background, subtle dot-grid background pattern via CssBaseline
   override.

2. Create src/shared/AppHeader.tsx — MUI AppBar with a flight icon, "SkyRoute"
   title, "Travel Platform" subtitle.

3. In BookingStepper.tsx, give each Step a custom icon (SearchIcon,
   FlightTakeoffIcon, PersonIcon, CheckCircleIcon).

4. Render AppHeader above BookingStepper in App.tsx, outside ApiHealthGate.

5. In SearchForm.tsx, wrap the date TextField with a CalendarMonthIcon
   InputAdornment that calls inputRef.current?.showPicker?.() on click.

6. Layout pass: Container maxWidth="md" wrapping step content, Paper elevation
   on the form/table for visual separation from the page texture.

---

## Frontend fix — preserve search criteria for back-navigation

In bookingFlowSlice.ts, add lastSearchCriteria state field and a
setLastSearchCriteria reducer.

In SearchForm.tsx: initialize form fields from lastSearchCriteria if non-null.
Dispatch setLastSearchCriteria right before calling the search mutation.

In ResultsList.tsx: add a "Back" button (ArrowBackIcon) that dispatches
setActiveStep(0) only — does not call resetFlow() or clear results.

---

## Frontend — passenger details form and confirmation dialog

1. bookingFlowSlice.ts — add bookingResult state and setBookingResult reducer.

2. PassengerDetailsForm.tsx — derive selected FlightOffer and passengerCount
   from Redux. Local state: array of passenger drafts. One MUI Paper card per
   passenger: Full Name, Email, document field (label/regex switches on
   isInternational — Passport ^[A-Za-z0-9]{6,9}$ vs National ID ^\d{6,12}$,
   validated on blur). Price breakdown box. "Confirm Booking" button disabled
   while invalid/empty, calls useCreateBookingMutation. On success:
   setBookingResult + setActiveStep(3). On 410: snackbar + setActiveStep(0).
   On other error: snackbar, stay on step.

3. ConfirmationDialog.tsx — MUI Dialog, open when activeStep===3 &&
   bookingResult!==null. Shows reference, flight summary, total. "Book
   Another" button dispatches resetFlow() (also clears bookingResult).

4. Wire both into BookingStepper.tsx.

---

## Frontend fix — change flight navigation with clean reset

1. In BookingStepper.tsx, add key={selectedFlightId} to PassengerDetailsForm
   where it's rendered for activeStep === 2 — forces a clean remount when a
   different flight is selected.

2. In PassengerDetailsForm.tsx, add a "Change Flight" button (ArrowBackIcon,
   variant="text") that dispatches setActiveStep(1) only.

---

## Frontend fix — simplify API health gate error classification

In client/src/shared/ApiHealthGate.tsx, remove the NETWORK_ERROR_STATUSES
classification (FETCH_ERROR/TIMEOUT_ERROR check). Replace with a plain isError
check: any error from useGetAirportsQuery renders ServiceUnavailable. Keep the
isLoading branch and success path unchanged.
