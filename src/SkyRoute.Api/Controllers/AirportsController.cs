using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Mapping;
using SkyRoute.Api.Models;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Api.Controllers;

[ApiController]
[Route("api/airports")]
public class AirportsController : ControllerBase
{
    [HttpGet]
    public ActionResult<List<AirportDto>> GetAll()
    {
        var airports = AirportSeedData.GetAll().Select(a => a.ToDto()).ToList();
        return Ok(airports);
    }
}
