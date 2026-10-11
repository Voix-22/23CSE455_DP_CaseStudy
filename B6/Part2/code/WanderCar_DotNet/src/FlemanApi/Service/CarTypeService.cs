using System.Net;
using AutoMapper;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service.Categories;

namespace FlemanApi.Service;

public class CarTypeService : ICarTypeService
{
    private readonly IGenericRepository<CarType, long> _carTypes;
    private readonly IVehicleCategoryRulesFactory _rulesFactory;
    private readonly IMapper _mapper;

    public CarTypeService(
        IGenericRepository<CarType, long> carTypes,
        IVehicleCategoryRulesFactory rulesFactory,
        IMapper mapper)
    {
        _carTypes = carTypes;
        _rulesFactory = rulesFactory;
        _mapper = mapper;
    }

    public async Task<CarTypeDTO> CreateCarTypeAsync(CreateCarTypeRequest request)
    {
        var name = request.CarTypeName.Trim();
        var lower = name.ToLower();
        if (await _carTypes.ExistsAsync(c => c.CarTypeName.ToLower() == lower))
        {
            throw new ApiException($"A car type with this name already exists: {name}", HttpStatusCode.Conflict);
        }

        var rules = _rulesFactory.Create(request.Category!.Value);
        var dailyRate = request.DailyRate!.Value;

        var errors = rules.ValidateRates(dailyRate, request.WeeklyRate, request.MonthlyRate);
        if (errors.Count > 0)
        {
            throw new ApiException(string.Join(", ", errors), HttpStatusCode.BadRequest);
        }

        var carType = new CarType
        {
            CarTypeName = name,
            DailyRate = dailyRate,
            WeeklyRate = request.WeeklyRate,
            MonthlyRate = request.MonthlyRate,
            ImagePath = request.ImagePath,
            Category = rules.Category,
            SecurityDeposit = rules.CalculateSecurityDeposit(dailyRate),
        };

        await _carTypes.AddAsync(carType);
        await _carTypes.SaveChangesAsync();
        return _mapper.Map<CarTypeDTO>(carType);
    }

    public async Task<IReadOnlyList<CarTypeDTO>> GetCarTypesByCategoryAsync(VehicleCategory category)
    {
        var carTypes = await _carTypes.FindAsync(c => c.Category == category);
        return _mapper.Map<List<CarTypeDTO>>(carTypes);
    }
}
