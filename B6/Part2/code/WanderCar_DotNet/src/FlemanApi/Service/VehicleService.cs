using AutoMapper;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.VehicleServiceImpl.
public class VehicleService : IVehicleService
{
    private readonly IGenericRepository<Car, long> _cars;
    private readonly IGenericRepository<CarType, long> _carTypes;
    private readonly IAvailabilityRepository _availability;
    private readonly IMapper _mapper;

    public VehicleService(
        IGenericRepository<Car, long> cars,
        IGenericRepository<CarType, long> carTypes,
        IAvailabilityRepository availability,
        IMapper mapper)
    {
        _cars = cars;
        _carTypes = carTypes;
        _availability = availability;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<CarTypeDTO>> GetCarTypesAsync() =>
        _mapper.Map<List<CarTypeDTO>>(await _carTypes.GetAllAsync());

    public async Task<IReadOnlyList<CarTypeDTO>> GetCarTypesForHubAsync(
        long? hubId, DateTime? pickupDatetime, DateTime? returnDatetime)
    {
        if (hubId is null) return await GetCarTypesAsync();

        var carTypes = await _carTypes.GetAllAsync();
        var result = new List<CarTypeDTO>();

        // Category-level (not per-car) availability: capacity is the
        // physical fleet of that type at this hub minus anything under
        // maintenance; demand is how many CONFIRMED/ONGOING bookings
        // already reserve that type here for an overlapping date range.
        // Every type is always returned — availableCount 0 is shown
        // disabled on the vehicle-selection page, not hidden.
        foreach (var ct in carTypes)
        {
            var dto = _mapper.Map<CarTypeDTO>(ct);
            var capacity = await _availability.CountCapacityAsync(hubId.Value, ct.CarTypeId);
            var overlapping = pickupDatetime is not null && returnDatetime is not null
                ? await _availability.CountOverlappingAsync(hubId.Value, ct.CarTypeId, pickupDatetime.Value, returnDatetime.Value, null)
                : 0;
            dto.AvailableCount = (int)Math.Max(0, capacity - overlapping);
            result.Add(dto);
        }

        return result;
    }

    public async Task<IReadOnlyList<CarDTO>> GetAvailableCarsAsync(
        long? hubId, DateTime? pickupDatetime, DateTime? returnDatetime)
    {
        if (hubId is null)
        {
            var all = await _cars.GetAllAsync();
            var dtos = new List<CarDTO>();
            foreach (var c in all) dtos.Add(await ToDtoAsync(c, true));
            return dtos;
        }

        // Two different questions, deliberately not conflated: "which cars
        // exist at this hub" (every one of them) vs "which of them are
        // physically free right now" (plain status check — individual
        // cars aren't reserved by date, only the category is).
        var atHub = await _cars.FindAsync(c => c.HubId == hubId.Value);
        var result = new List<CarDTO>();
        foreach (var c in atHub) result.Add(await ToDtoAsync(c, c.Status == CarStatus.AVAILABLE));
        return result;
    }

    public async Task<CarDTO> GetCarByIdAsync(long carId)
    {
        var car = await _cars.GetByIdAsync(carId)
            ?? throw new ResourceNotFoundException($"Car not found: {carId}");
        return await ToDtoAsync(car, car.Status == CarStatus.AVAILABLE);
    }

    public async Task<CarTypeDTO> GetCarTypeByIdAsync(long carTypeId)
    {
        var carType = await _carTypes.GetByIdAsync(carTypeId)
            ?? throw new ResourceNotFoundException($"Car type not found: {carTypeId}");
        return _mapper.Map<CarTypeDTO>(carType);
    }

    private async Task<CarDTO> ToDtoAsync(Car car, bool bookableNow)
    {
        var dto = _mapper.Map<CarDTO>(car);
        dto.BookableNow = bookableNow;
        // No relationship — an explicit second lookup by the plain
        // CarTypeId column to build the nested CarTypeDTO the frontend expects.
        var carType = await _carTypes.GetByIdAsync(car.CarTypeId);
        if (carType is not null) dto.CarType = _mapper.Map<CarTypeDTO>(carType);
        return dto;
    }
}
