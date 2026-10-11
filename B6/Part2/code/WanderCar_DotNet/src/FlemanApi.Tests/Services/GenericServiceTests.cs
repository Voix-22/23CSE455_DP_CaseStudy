using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;
using Moq;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class GenericServiceTests
{
    private List<State> _states = null!;
    private Mock<IGenericRepository<State, long>> _repoMock = null!;
    private GenericService<State, StateDTO, long> _service = null!;

    [SetUp]
    public void SetUp()
    {
        _states = new List<State>
        {
            new() { StateId = 1, StateName = "Maharashtra" },
            new() { StateId = 2, StateName = "Karnataka" },
        };
        _repoMock = MockRepositoryFactory.Create<State, long>(_states, s => s.StateId, (s, id) => s.StateId = id);
        _service = new GenericService<State, StateDTO, long>(_repoMock.Object, TestMapperFactory.Create());
    }

    [Test]
    public async Task GetByIdAsync_ExistingId_ReturnsMappedDto()
    {
        var dto = await _service.GetByIdAsync(1);

        dto.Should().NotBeNull();
        dto!.StateName.Should().Be("Maharashtra");
    }

    [Test]
    public async Task GetByIdAsync_MissingId_ReturnsNull()
    {
        var dto = await _service.GetByIdAsync(999);

        dto.Should().BeNull();
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllMappedDtos()
    {
        var dtos = await _service.GetAllAsync();

        dtos.Should().HaveCount(2);
        dtos.Should().Contain(d => d.StateName == "Karnataka");
    }

    [Test]
    public async Task CreateAsync_AddsEntityAndReturnsMappedDto()
    {
        var dto = new StateDTO { StateName = "Gujarat" };

        var created = await _service.CreateAsync(dto);

        created.StateName.Should().Be("Gujarat");
        _states.Should().Contain(s => s.StateName == "Gujarat");
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_ExistingId_MapsDtoOntoEntityAndSaves()
    {
        var dto = new StateDTO { StateId = 1, StateName = "Updated Maharashtra" };

        var updated = await _service.UpdateAsync(1, dto);

        updated.StateName.Should().Be("Updated Maharashtra");
        _states.Single(s => s.StateId == 1).StateName.Should().Be("Updated Maharashtra");
    }

    [Test]
    public async Task UpdateAsync_MissingId_ThrowsResourceNotFound()
    {
        var dto = new StateDTO { StateId = 999, StateName = "Nowhere" };

        var act = async () => await _service.UpdateAsync(999, dto);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task DeleteAsync_ExistingId_RemovesEntity()
    {
        await _service.DeleteAsync(1);

        _states.Should().NotContain(s => s.StateId == 1);
        _repoMock.Verify(r => r.Remove(It.IsAny<State>()), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_MissingId_ThrowsResourceNotFound()
    {
        var act = async () => await _service.DeleteAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
}
