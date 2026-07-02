using System.Text.Json;
using System.Text.Json.Serialization;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;

namespace SkyRoute.Infrastructure.Providers;

public class BudgetWingsProvider : IFlightProvider
{
    private static readonly string DataFilePath = Path.Combine(
        AppContext.BaseDirectory, "Providers", "MockData", "budgetwings-flights.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IPricingStrategy _pricingStrategy;

    public BudgetWingsProvider(IPricingStrategy pricingStrategy)
    {
        _pricingStrategy = pricingStrategy;
    }

    public string ProviderName => "BudgetWings";

    public async Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchCriteria criteria)
    {
        var rawFlights = await LoadRawFlightsAsync();

        var matches = rawFlights.Where(f =>
            string.Equals(f.OriginCode, criteria.Origin, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(f.DestinationCode, criteria.Destination, StringComparison.OrdinalIgnoreCase));

        var offers = new List<FlightOffer>();

        foreach (var raw in matches)
        {
            var origin = AirportSeedData.Get(raw.OriginCode);
            var destination = AirportSeedData.Get(raw.DestinationCode);
            var departureTime = criteria.DepartureDate.Date.AddMinutes(raw.DepartureOffsetMinutes);
            var arrivalTime = departureTime.AddMinutes(raw.DurationMinutes);
            var cabinClass = Enum.Parse<CabinClass>(raw.CabinClass, ignoreCase: true);
            var totalPrice = _pricingStrategy.CalculatePrice(raw.BaseFareUsd);

            offers.Add(new FlightOffer(
                Id: Guid.NewGuid().ToString(),
                Provider: ProviderName,
                FlightNumber: raw.FlightNumber,
                Origin: origin,
                Destination: destination,
                DepartureTime: departureTime,
                ArrivalTime: arrivalTime,
                CabinClass: cabinClass,
                BaseFare: raw.BaseFareUsd,
                TotalPrice: totalPrice,
                PricePerPerson: totalPrice / criteria.Passengers));
        }

        return offers;
    }

    // Swap this method for an HttpClient call to the real BudgetWings search endpoint —
    // everything below this line (mapping, pricing) stays unchanged.
    private static async Task<List<BudgetWingsRawFlight>> LoadRawFlightsAsync()
    {
        await using var stream = File.OpenRead(DataFilePath);
        var flights = await JsonSerializer.DeserializeAsync<List<BudgetWingsRawFlight>>(stream, JsonOptions);
        return flights ?? [];
    }

    private record BudgetWingsRawFlight(
        [property: JsonPropertyName("flight_number")] string FlightNumber,
        [property: JsonPropertyName("origin_code")] string OriginCode,
        [property: JsonPropertyName("destination_code")] string DestinationCode,
        [property: JsonPropertyName("departure_offset_minutes")] int DepartureOffsetMinutes,
        [property: JsonPropertyName("duration_minutes")] int DurationMinutes,
        [property: JsonPropertyName("cabin_class")] string CabinClass,
        [property: JsonPropertyName("base_fare_usd")] decimal BaseFareUsd);
}
