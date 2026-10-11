namespace FlemanApi.Service;

// Generic CRUD service contract — requirement #7's service-layer half.
// Wraps IGenericRepository<TEntity,TKey> and maps to/from a DTO with
// AutoMapper so callers never see the EF entity directly.
public interface IGenericService<TDto, in TKey> where TDto : class
{
    Task<TDto?> GetByIdAsync(TKey id);
    Task<IReadOnlyList<TDto>> GetAllAsync();
    Task<TDto> CreateAsync(TDto dto);
    Task<TDto> UpdateAsync(TKey id, TDto dto);
    Task DeleteAsync(TKey id);
}
