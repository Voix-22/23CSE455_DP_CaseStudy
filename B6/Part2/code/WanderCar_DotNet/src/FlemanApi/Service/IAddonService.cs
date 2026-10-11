using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface IAddonService
{
    Task<IReadOnlyList<AddonDTO>> GetAddonsAsync();
}
