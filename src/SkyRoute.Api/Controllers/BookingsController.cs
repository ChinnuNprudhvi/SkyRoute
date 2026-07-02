using Microsoft.AspNetCore.Mvc;
using SkyRoute.Api.Mapping;
using SkyRoute.Api.Models;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Models;
using SkyRoute.Application.Services;

namespace SkyRoute.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly IBookingRepository _bookingRepository;

    public BookingsController(BookingService bookingService, IBookingRepository bookingRepository)
    {
        _bookingService = bookingService;
        _bookingRepository = bookingRepository;
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponseDto>> Create(CreateBookingRequest request)
    {
        var booking = await _bookingService.CreateBookingAsync(request);
        var dto = booking.ToResponseDto();

        return CreatedAtAction(nameof(GetByReference), new { reference = dto.BookingReference }, dto);
    }

    [HttpGet("{reference}")]
    public async Task<ActionResult<BookingResponseDto>> GetByReference(string reference)
    {
        var booking = await _bookingRepository.GetByReferenceAsync(reference);
        if (booking is null)
        {
            return NotFound();
        }

        return Ok(booking.ToResponseDto());
    }
}
