using FlemanApi.Models;

namespace FlemanApi.Repository;

public record CarTypeCountProjection(long CarTypeId, long TotalCars, long AvailableCars, long BookedCars, long MaintenanceCars);


public interface IAvailabilityRepository
{
    // Physical fleet size for a (hub, carType) combo, minus anything pulled
    // out of service for maintenance.
    Task<long> CountCapacityAsync(long hubId, long carTypeId);

    // How many CONFIRMED/ONGOING bookings already reserve this (hub,
    // carType) for a date range overlapping the one requested.
    Task<long> CountOverlappingAsync(long hubId, long carTypeId, DateTime pickupDatetime, DateTime returnDatetime, long? excludeBookingId);

    // Per-car-type fleet counts, optionally scoped to one hub — backs the
    // staff dashboard's available/booked/maintenance breakdown.
    Task<IReadOnlyList<CarTypeCountProjection>> CountByCarTypeAsync(long? hubId);
}
