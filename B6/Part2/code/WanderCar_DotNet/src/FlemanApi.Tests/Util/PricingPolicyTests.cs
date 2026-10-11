
using FlemanApi.Models;
using FlemanApi.Util;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace FlemanApi.Tests.Util;

[TestFixture]
public class PricingPolicyTests
{
    private static CarType MakeCarType() => new()
    {
        CarTypeId = 1,
        CarTypeName = "Sedan",
        DailyRate = 100,
        WeeklyRate = 600,
        MonthlyRate = 2000
    };

    private static IOptions<PricingSettings> MakeOptions() =>
        Options.Create(new PricingSettings
        {
            WeekendSurchargePercent = 20,
            SeasonalSurchargePercent = 30,
            PeakMonths = [12, 1, 4]
        });

    [Test]
    public void WeekendPolicy_AddsSurcharge_WhenRentalIncludesWeekend()
    {
        var policy = new WeekendPricingPolicy(MakeOptions());
        var car = MakeCarType();

        // Friday to Monday: three rental days, including Saturday and Sunday.
        var pickup = new DateTime(2026, 10, 9);
        var returnDate = new DateTime(2026, 10, 12);

        var actual = policy.CalculatePrice(car, pickup, returnDate);

        actual.Should().Be(360); // Standard 300 + 20% surcharge.
    }

    [Test]
    public void WeekendPolicy_DoesNotAddSurcharge_WhenNoWeekendIncluded()
    {
        var policy = new WeekendPricingPolicy(MakeOptions());
        var car = MakeCarType();

        // Monday to Thursday.
        var pickup = new DateTime(2026, 10, 5);
        var returnDate = new DateTime(2026, 10, 8);

        var actual = policy.CalculatePrice(car, pickup, returnDate);

        actual.Should().Be(300);
    }

    [Test]
    public void SeasonalPolicy_AddsSurcharge_WhenPickupMonthIsPeak()
    {
        var policy = new SeasonalPricingPolicy(MakeOptions());
        var car = MakeCarType();

        var pickup = new DateTime(2026, 12, 7);
        var returnDate = new DateTime(2026, 12, 10);

        var actual = policy.CalculatePrice(car, pickup, returnDate);

        actual.Should().Be(390); // Standard 300 + 30% surcharge.
    }

    [Test]
    public void SeasonalPolicy_DoesNotAddSurcharge_WhenPickupMonthIsNotPeak()
    {
        var policy = new SeasonalPricingPolicy(MakeOptions());
        var car = MakeCarType();

        var pickup = new DateTime(2026, 10, 5);
        var returnDate = new DateTime(2026, 10, 8);

        var actual = policy.CalculatePrice(car, pickup, returnDate);

        actual.Should().Be(300);
    }

    [Test]
    public void Selector_ChoosesSeasonalBeforeWeekend()
    {
        var options = MakeOptions();

        var selector = new PricingPolicySelector(
            new StandardPricingPolicy(),
            new WeekendPricingPolicy(options),
            new SeasonalPricingPolicy(options),
            options);

        // Peak month and weekend: seasonal takes priority.
        var pickup = new DateTime(2026, 12, 11);
        var returnDate = new DateTime(2026, 12, 14);

        var actual = selector.SelectPolicy(
            MakeCarType(), pickup, returnDate);

        actual.Name.Should().Be("Seasonal");
    }

    [Test]
    public void Selector_ChoosesWeekend_WhenNotPeakButWeekendIncluded()
    {
        var options = MakeOptions();

        var selector = new PricingPolicySelector(
            new StandardPricingPolicy(),
            new WeekendPricingPolicy(options),
            new SeasonalPricingPolicy(options),
            options);

        var pickup = new DateTime(2026, 10, 9);
        var returnDate = new DateTime(2026, 10, 12);

        var actual = selector.SelectPolicy(
            MakeCarType(), pickup, returnDate);

        actual.Name.Should().Be("Weekend");
    }

    [Test]
    public void Selector_ChoosesStandard_WhenNeitherConditionApplies()
    {
        var options = MakeOptions();

        var selector = new PricingPolicySelector(
            new StandardPricingPolicy(),
            new WeekendPricingPolicy(options),
            new SeasonalPricingPolicy(options),
            options);

        var pickup = new DateTime(2026, 10, 5);
        var returnDate = new DateTime(2026, 10, 8);

        var actual = selector.SelectPolicy(
            MakeCarType(), pickup, returnDate);

        actual.Name.Should().Be("Standard");
    }
}
