using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class AddonServiceTests
{
    [Test]
    public async Task GetAddonsAsync_ReturnsAllAddonsMappedToDto()
    {
        var addons = new List<Addon>
        {
            new() { AddonId = 1, AddonName = "GPS", DailyRate = 10.0 },
            new() { AddonId = 2, AddonName = "Child Seat", DailyRate = 5.0 },
        };
        var repoMock = MockRepositoryFactory.Create<Addon, long>(addons, a => a.AddonId);
        var service = new AddonService(repoMock.Object, TestMapperFactory.Create());

        var result = await service.GetAddonsAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.AddonName == "GPS" && d.DailyRate == 10.0);
    }

    [Test]
    public async Task GetAddonsAsync_NoAddons_ReturnsEmptyList()
    {
        var repoMock = MockRepositoryFactory.Create<Addon, long>(new List<Addon>(), a => a.AddonId);
        var service = new AddonService(repoMock.Object, TestMapperFactory.Create());

        var result = await service.GetAddonsAsync();

        result.Should().BeEmpty();
    }
}
