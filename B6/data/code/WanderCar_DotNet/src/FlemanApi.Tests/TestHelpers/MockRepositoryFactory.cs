using System.Linq.Expressions;
using FlemanApi.Repository;
using Moq;

namespace FlemanApi.Tests.TestHelpers;

// Backs a Mock<IGenericRepository<TEntity,TKey>> with a plain in-memory
// List<TEntity>, evaluating the Expression<Func<TEntity,bool>> predicates
// used by FindAsync/FirstOrDefaultAsync/ExistsAsync by compiling and
// running them against that list — far less brittle than trying to Setup()
// against one specific lambda instance (which Moq can't match structurally).
public static class MockRepositoryFactory
{
    public static Mock<IGenericRepository<TEntity, TKey>> Create<TEntity, TKey>(
        List<TEntity> seed, Func<TEntity, TKey> keySelector, Action<TEntity, TKey>? keySetter = null)
        where TEntity : class
    {
        var mock = new Mock<IGenericRepository<TEntity, TKey>>();

        mock.Setup(r => r.GetByIdAsync(It.IsAny<TKey>()))
            .ReturnsAsync((TKey id) => seed.FirstOrDefault(e => Equals(keySelector(e), id)));

        mock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(() => (IReadOnlyList<TEntity>)seed.ToList());

        mock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<TEntity, bool>>>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> predicate) =>
                (IReadOnlyList<TEntity>)seed.AsQueryable().Where(predicate).ToList());

        mock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<TEntity, bool>>>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> predicate) =>
                seed.AsQueryable().Where(predicate).FirstOrDefault());

        mock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<TEntity, bool>>>()))
            .ReturnsAsync((Expression<Func<TEntity, bool>> predicate) =>
                seed.AsQueryable().Where(predicate).Any());

        var nextId = seed.Count == 0 ? 1L : Convert.ToInt64(seed.Max(e => Convert.ToInt64(keySelector(e)))) + 1;

        mock.Setup(r => r.AddAsync(It.IsAny<TEntity>()))
            .ReturnsAsync((TEntity entity) =>
            {
                if (keySetter is not null && Equals(keySelector(entity), default(TKey)))
                {
                    keySetter(entity, (TKey)Convert.ChangeType(nextId, typeof(TKey)));
                    nextId++;
                }
                seed.Add(entity);
                return entity;
            });

        mock.Setup(r => r.Remove(It.IsAny<TEntity>()))
            .Callback((TEntity entity) => seed.Remove(entity));

        mock.Setup(r => r.Update(It.IsAny<TEntity>()));

        mock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        return mock;
    }
}
