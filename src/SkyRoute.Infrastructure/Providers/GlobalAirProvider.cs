using System.Text.Json;
using System.Text.Json.Serialization;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Domain.Entities;
using SkyRoute.Domain.Enums;

namespace SkyRoute.Infrastructure.Providers;

public class GlobalAirProvider : IFlightProvider
{
    private static readonly string DataFilePath = Path.Combine(
        AppContext.BaseDirectory, "Providers", "MockData", "globalair-flights.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IPricingStrategy _pricingStrategy;

    public GlobalAirProvider(IPricingStrategy pricingStrategy)
    {
        _pricingStrategy = pricingStrategy;
    }

    public string ProviderName => "GlobalAir";

    public async Task<IReadOnlyList<FlightOffer>> SearchAsync(SearchCriteria criteria)
    {
        var rawFlights = await LoadRawFlightsAsync();

        var matches = rawFlights.Where(f =>
            string.Equals(f.Route.From, criteria.Origin, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(f.Route.To, criteria.Destination, StringComparison.OrdinalIgnoreCase));

        var offers = new List<FlightOffer>();

        foreach (var raw in matches)
        {
            var origin = AirportSeedData.Get(raw.Route.From);
            var destination = AirportSeedData.Get(raw.Route.To);
            var departureTime = criteria.DepartureDate.Date.AddMinutes(raw.DepartOffsetMinutes);
            var arrivalTime = departureTime.AddMinutes(raw.DurationMinutes);
            var cabinClass = Enum.Parse<CabinClass>(raw.Cabin, ignoreCase: true);
            var totalPrice = _pricingStrategy.CalculatePrice(raw.Fare);

            offers.Add(new FlightOffer(
                Id: Guid.NewGuid().ToString(),
                Provider: ProviderName,
                FlightNumber: raw.FlightNo,
                Origin: origin,
                Destination: destination,
                DepartureTime: departureTime,
                ArrivalTime: arrivalTime,
                CabinClass: cabinClass,
                BaseFare: raw.Fare,
                TotalPrice: totalPrice,
                PricePerPerson: totalPrice / criteria.Passengers));
        }

        return offers;
    }

    // Swap this method for an HttpClient call to the real GlobalAir search endpoint —
    // everything below this line (mapping, pricing) stays unchanged.
    private static async Task<List<GlobalAirRawFlight>> LoadRawFlightsAsync()
    {
        await using var stream = File.OpenRead(DataFilePath);
        var flights = await JsonSerializer.DeserializeAsync<List<GlobalAirRawFlight>>(stream, JsonOptions);
        return flights ?? [];
    }

    private record GlobalAirRawFlight(
        [property: JsonPropertyName("flightNo")] string FlightNo,
        [property: JsonPropertyName("route")] GlobalAirRawRoute Route,
        [property: JsonPropertyName("departOffsetMinutes")] int DepartOffsetMinutes,
        [property: JsonPropertyName("durationMinutes")] int DurationMinutes,
        [property: JsonPropertyName("cabin")] string Cabin,
        [property: JsonPropertyName("fare")] decimal Fare);

    private record GlobalAirRawRoute(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string To);
}
