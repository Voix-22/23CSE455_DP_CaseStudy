using FlemanApi.Data;
using FlemanApi.Models;
using FlemanApi.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlemanApi.Tests.Repository;

[TestFixture]
public class GenericRepositoryTests
{
    private FlemanDbContext _context = null!;
    private GenericRepository<State, long> _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<FlemanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FlemanDbContext(options);
        _repository = new GenericRepository<State, long>(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task AddAsync_ThenSaveChanges_PersistsEntity()
    {
        var state = new State { StateName = "Maharashtra" };

        await _repository.AddAsync(state);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();
        all.Should().ContainSingle(s => s.StateName == "Maharashtra");
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        var state = new State { StateName = "Karnataka" };
        await _repository.AddAsync(state);
        await _repository.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(state.StateId);

        found.Should().NotBeNull();
        found!.StateName.Should().Be("Karnataka");
    }

    [Test]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var found = await _repository.GetByIdAsync(999L);

        found.Should().BeNull();
    }

    [Test]
    public async Task GetAllAsync_ReturnsEveryEntity()
    {
        await _repository.AddAsync(new State { StateName = "Maharashtra" });
        await _repository.AddAsync(new State { StateName = "Karnataka" });
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();

        all.Should().HaveCount(2);
    }

    [Test]
    public async Task FindAsync_MatchesPredicate_ReturnsFilteredEntities()
    {
        await _repository.AddAsync(new State { StateName = "Maharashtra" });
        await _repository.AddAsync(new State { StateName = "Karnataka" });
        await _repository.SaveChangesAsync();

        var found = await _repository.FindAsync(s => s.StateName == "Karnataka");

        found.Should().ContainSingle().Which.StateName.Should().Be("Karnataka");
    }

    [Test]
    public async Task FirstOrDefaultAsync_NoMatch_ReturnsNull()
    {
        var found = await _repository.FirstOrDefaultAsync(s => s.StateName == "Nowhere");

        found.Should().BeNull();
    }

    [Test]
    public async Task ExistsAsync_MatchingEntity_ReturnsTrue()
    {
        await _repository.AddAsync(new State { StateName = "Maharashtra" });
        await _repository.SaveChangesAsync();

        var exists = await _repository.ExistsAsync(s => s.StateName == "Maharashtra");

        exists.Should().BeTrue();
    }

    [Test]
    public async Task ExistsAsync_NoMatch_ReturnsFalse()
    {
        var exists = await _repository.ExistsAsync(s => s.StateName == "Nowhere");

        exists.Should().BeFalse();
    }

    [Test]
    public async Task Remove_ThenSaveChanges_DeletesEntity()
    {
        var state = new State { StateName = "Maharashtra" };
        await _repository.AddAsync(state);
        await _repository.SaveChangesAsync();

        _repository.Remove(state);
        await _repository.SaveChangesAsync();

        var all = await _repository.GetAllAsync();
        all.Should().BeEmpty();
    }

    [Test]
    public async Task Update_ThenSaveChanges_PersistsModifiedValues()
    {
        var state = new State { StateName = "Maharashtra" };
        await _repository.AddAsync(state);
        await _repository.SaveChangesAsync();

        state.StateName = "Updated";
        _repository.Update(state);
        await _repository.SaveChangesAsync();

        var found = await _repository.GetByIdAsync(state.StateId);
        found!.StateName.Should().Be("Updated");
    }
}
