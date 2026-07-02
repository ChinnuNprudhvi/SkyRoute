using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Mapping;
using SkyRoute.Api.Models;
using SkyRoute.Application.Services;

namespace SkyRoute.Api.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightsController : ControllerBase
{
    private readonly FlightAggregatorService _flightAggregatorService;

    public FlightsController(FlightAggregatorService flightAggregatorService)
    {
        _flightAggregatorService = flightAggregatorService;
    }

    [HttpPost("search")]
    public async Task<ActionResult<FlightSearchResponseDto>> Search(FlightSearchRequestDto request)
    {
        var criteria = request.ToCriteria();
        var result = await _flightAggregatorService.SearchAsync(criteria);

        return Ok(result.ToResponseDto());
    }
}
