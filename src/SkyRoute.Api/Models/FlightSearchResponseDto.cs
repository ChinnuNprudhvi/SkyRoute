namespace SkyRoute.Api.Models;

// Intentionally has no field carrying raw provider names — only a generic
// PartialResults flag + Notice, so clients never learn which upstream
// provider(s) failed.
public record FlightSearchResponseDto(
    string SearchId,
    bool IsInternational,
    List<FlightOfferResponseDto> Results,
    bool PartialResults,
    string? Notice);
