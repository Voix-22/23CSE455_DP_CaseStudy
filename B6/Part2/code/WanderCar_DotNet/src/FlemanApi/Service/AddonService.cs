using AutoMapper;
using FlemanApi.DTO;
using FlemanApi.Models;
using FlemanApi.Repository;

namespace FlemanApi.Service;

public class AddonService : IAddonService
{
    private readonly IGenericRepository<Addon, long> _addons;
    private readonly IMapper _mapper;

    public AddonService(IGenericRepository<Addon, long> addons, IMapper mapper)
    {
        _addons = addons;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AddonDTO>> GetAddonsAsync() =>
        _mapper.Map<List<AddonDTO>>(await _addons.GetAllAsync());
}
