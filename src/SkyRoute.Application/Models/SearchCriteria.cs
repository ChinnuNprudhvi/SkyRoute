namespace SkyRoute.Application.Models;

public record SearchCriteria(string Origin, string Destination, DateTime DepartureDate, int Passengers);
