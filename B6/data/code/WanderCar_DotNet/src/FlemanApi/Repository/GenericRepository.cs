using System.Linq.Expressions;
using FlemanApi.Data;
using Microsoft.EntityFrameworkCore;

namespace FlemanApi.Repository;

// EF Core-backed implementation of IGenericRepository — requirement #8.
// Reused as-is by every entity that doesn't need custom queries; entities
// that do (e.g. Car, BookingHeader) get a thin sibling interface with just
// the extra query methods, still built on this same DbContext.
public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly FlemanDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    public GenericRepository(FlemanDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(TKey id)
    {
        if (id is null) return null;
        return await DbSet.FindAsync(id);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync() =>
        await DbSet.AsNoTracking().ToListAsync();

    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
        await DbSet.AsNoTracking().Where(predicate).ToListAsync();

    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate) =>
        await DbSet.FirstOrDefaultAsync(predicate);

    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate) =>
        await DbSet.AnyAsync(predicate);

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
        return entity;
    }

    public void Update(TEntity entity) => DbSet.Update(entity);

    public void Remove(TEntity entity) => DbSet.Remove(entity);

    public Task SaveChangesAsync() => Context.SaveChangesAsync();
}
