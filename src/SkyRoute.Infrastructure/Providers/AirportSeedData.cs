using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Providers;

/// <summary>
/// Static lookup of the airports referenced by the mock provider fixtures.
/// </summary>
public static class AirportSeedData
{
    private static readonly Dictionary<string, Airport> Airports = new(StringComparer.OrdinalIgnoreCase)
    {
        ["JFK"] = new Airport("JFK", "John F. Kennedy International Airport", "New York", "United States"),
        ["LAX"] = new Airport("LAX", "Los Angeles International Airport", "Los Angeles", "United States"),
        ["ORD"] = new Airport("ORD", "O'Hare International Airport", "Chicago", "United States"),
        ["LHR"] = new Airport("LHR", "Heathrow Airport", "London", "United Kingdom"),
        ["DEL"] = new Airport("DEL", "Indira Gandhi International Airport", "Delhi", "India"),
        ["BOM"] = new Airport("BOM", "Chhatrapati Shivaji Maharaj International Airport", "Mumbai", "India"),
    };

    public static Airport Get(string code)
    {
        if (Airports.TryGetValue(code, out var airport))
        {
            return airport;
        }

        throw new KeyNotFoundException($"No seed airport data found for code '{code}'.");
    }
}
