using AutoMapper;
using FlemanApi.Exceptions;
using FlemanApi.Repository;

namespace FlemanApi.Service;

// AutoMapper-backed implementation of IGenericService — requirement #8's
// service-layer half. TEntity stays tracked between GetByIdAsync and
// SaveChangesAsync so UpdateAsync can map the DTO straight onto it without
// needing to know each entity's key-property name.
public class GenericService<TEntity, TDto, TKey> : IGenericService<TDto, TKey>
    where TEntity : class
    where TDto : class
{
    protected readonly IGenericRepository<TEntity, TKey> Repository;
    protected readonly IMapper Mapper;

    public GenericService(IGenericRepository<TEntity, TKey> repository, IMapper mapper)
    {
        Repository = repository;
        Mapper = mapper;
    }

    public async Task<TDto?> GetByIdAsync(TKey id)
    {
        var entity = await Repository.GetByIdAsync(id);
        return entity is null ? null : Mapper.Map<TDto>(entity);
    }

    public async Task<IReadOnlyList<TDto>> GetAllAsync()
    {
        var entities = await Repository.GetAllAsync();
        return Mapper.Map<List<TDto>>(entities);
    }

    public async Task<TDto> CreateAsync(TDto dto)
    {
        var entity = Mapper.Map<TEntity>(dto);
        await Repository.AddAsync(entity);
        await Repository.SaveChangesAsync();
        return Mapper.Map<TDto>(entity);
    }

    public async Task<TDto> UpdateAsync(TKey id, TDto dto)
    {
        var entity = await Repository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"{typeof(TEntity).Name} {id} not found");
        Mapper.Map(dto, entity);
        await Repository.SaveChangesAsync();
        return Mapper.Map<TDto>(entity);
    }

    public async Task DeleteAsync(TKey id)
    {
        var entity = await Repository.GetByIdAsync(id)
            ?? throw new ResourceNotFoundException($"{typeof(TEntity).Name} {id} not found");
        Repository.Remove(entity);
        await Repository.SaveChangesAsync();
    }
}
