using SkyRoute.Domain.Entities;

namespace SkyRoute.Application.Interfaces;

public interface IBookingRepository
{
    Task SaveAsync(Booking booking);
    Task<Booking?> GetByReferenceAsync(string reference);
}
