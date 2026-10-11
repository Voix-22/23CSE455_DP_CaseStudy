using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Service.Categories;
using FluentAssertions;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class VehicleCategoryRulesTests
{
    private readonly VehicleCategoryRulesFactory _factory = new();

    [Test]
    public void Create_ReturnsMatchingRulesForEachCategory()
    {
        _factory.Create(VehicleCategory.STANDARD).Should().BeOfType<StandardCategoryRules>();
        _factory.Create(VehicleCategory.ELECTRIC).Should().BeOfType<ElectricCategoryRules>();
        _factory.Create(VehicleCategory.COMMERCIAL).Should().BeOfType<CommercialCategoryRules>();
    }

    [Test]
    public void Create_EveryEnumValueIsSupportedAndReportsItsOwnCategory()
    {
        foreach (var category in Enum.GetValues<VehicleCategory>())
        {
            _factory.Create(category).Category.Should().Be(category);
        }
    }

    [Test]
    public void Create_UnknownCategory_ThrowsApiException()
    {
        var act = () => _factory.Create((VehicleCategory)999);

        act.Should().Throw<ApiException>();
    }

    [Test]
    public void Standard_HasNoDepositAndNoRateRule()
    {
        var rules = new StandardCategoryRules();

        rules.CalculateSecurityDeposit(100.0).Should().Be(0.0);
        rules.ValidateRates(100.0, null, null).Should().BeEmpty();
    }

    [Test]
    public void Electric_DepositIsTwiceTheDailyRate()
    {
        var rules = new ElectricCategoryRules();

        rules.CalculateSecurityDeposit(60.0).Should().Be(120.0);
        rules.ValidateRates(60.0, null, null).Should().BeEmpty();
    }

    [Test]
    public void Commercial_DepositIsFiveTimesTheDailyRate()
    {
        new CommercialCategoryRules().CalculateSecurityDeposit(80.0).Should().Be(400.0);
    }

    [Test]
    public void Commercial_RequiresWeeklyRate()
    {
        var rules = new CommercialCategoryRules();

        rules.ValidateRates(80.0, null, null).Should().ContainSingle();
        rules.ValidateRates(80.0, 500.0, null).Should().BeEmpty();
    }
}
