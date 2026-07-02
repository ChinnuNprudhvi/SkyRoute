using System.Collections.Concurrent;
using SkyRoute.Application.Interfaces;
using SkyRoute.Domain.Entities;

namespace SkyRoute.Infrastructure.Repositories;

public class InMemoryBookingRepository : IBookingRepository
{
    private readonly ConcurrentDictionary<string, Booking> _bookings = new();

    public Task SaveAsync(Booking booking)
    {
        _bookings[booking.Reference] = booking;
        return Task.CompletedTask;
    }

    public Task<Booking?> GetByReferenceAsync(string reference)
    {
        _bookings.TryGetValue(reference, out var booking);
        return Task.FromResult(booking);
    }
}
