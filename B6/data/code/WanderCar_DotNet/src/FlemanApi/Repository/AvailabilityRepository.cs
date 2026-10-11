using FlemanApi.Data;
using FlemanApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FlemanApi.Repository;

public class AvailabilityRepository : IAvailabilityRepository
{
    private static readonly BookingStatus[] ActiveStatuses = { BookingStatus.CONFIRMED, BookingStatus.ONGOING };

    private readonly FlemanDbContext _context;

    public AvailabilityRepository(FlemanDbContext context)
    {
        _context = context;
    }

    public Task<long> CountCapacityAsync(long hubId, long carTypeId) =>
        _context.Cars.LongCountAsync(c =>
            c.HubId == hubId && c.CarTypeId == carTypeId && c.Status != CarStatus.UNDER_MAINTENANCE);

    public Task<long> CountOverlappingAsync(
        long hubId, long carTypeId, DateTime pickupDatetime, DateTime returnDatetime, long? excludeBookingId) =>
        _context.BookingHeaders.LongCountAsync(b =>
            b.PickupHubId == hubId
            && b.CarTypeId == carTypeId
            && b.BookingStatus.HasValue && ActiveStatuses.Contains(b.BookingStatus.Value)
            && b.PickupDatetime < returnDatetime
            && b.ReturnDatetime > pickupDatetime
            && (excludeBookingId == null || b.BookingId != excludeBookingId.Value));

    public async Task<IReadOnlyList<CarTypeCountProjection>> CountByCarTypeAsync(long? hubId)
    {
        var query = _context.Cars.AsNoTracking().AsQueryable();
        if (hubId is not null) query = query.Where(c => c.HubId == hubId.Value);

        var grouped = await query
            .GroupBy(c => c.CarTypeId)
            .Select(g => new CarTypeCountProjection(
                g.Key,
                g.LongCount(),
                g.LongCount(c => c.Status == CarStatus.AVAILABLE),
                g.LongCount(c => c.Status == CarStatus.BOOKED),
                g.LongCount(c => c.Status == CarStatus.UNDER_MAINTENANCE)))
            .ToListAsync();

        return grouped;
    }
}
