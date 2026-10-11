
using FlemanApi.Models;
using FlemanApi.Util;
using FluentAssertions;
using NUnit.Framework;

namespace FlemanApi.Tests.Util;

[TestFixture]
public class StandardPricingPolicyTests
{
    private readonly StandardPricingPolicy _policy = new();

    private static CarType MakeCarType(
        double daily = 100.0,
        double? weekly = 600.0,
        double? monthly = 2000.0)
    {
        return new CarType
        {
            CarTypeId = 1,
            CarTypeName = "Sedan",
            DailyRate = daily,
            WeeklyRate = weekly,
            MonthlyRate = monthly
        };
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(10)]
    [TestCase(29)]
    [TestCase(30)]
    [TestCase(35)]
    [TestCase(37)]
    [TestCase(60)]
    [TestCase(90)]
    public void StandardPricing_MatchesBaseline(int days)
    {
        var carType = MakeCarType();
        var pickup = new DateTime(2026, 10, 5, 10, 0, 0);
        var returnDate = pickup.AddDays(days);

        var expected =
            RentalRateCalculator.CalculateRentalAmount(carType, days);

        var actual = _policy.CalculatePrice(carType, pickup, returnDate);

        actual.Should().Be(expected);
    }

    [Test]
    public void StandardPricing_WithNullRates_MatchesBaseline()
    {
        var carType = MakeCarType(weekly: null, monthly: null);
        var pickup = new DateTime(2026, 10, 5);
        var returnDate = pickup.AddDays(15);

        var expected =
            RentalRateCalculator.CalculateRentalAmount(carType, 15);

        var actual = _policy.CalculatePrice(carType, pickup, returnDate);

        actual.Should().Be(expected);
    }

    [Test]
    public void StandardPricing_WithZeroRates_MatchesBaseline()
    {
        var carType = MakeCarType(weekly: 0, monthly: 0);
        var pickup = new DateTime(2026, 10, 5);
        var returnDate = pickup.AddDays(10);

        var expected =
            RentalRateCalculator.CalculateRentalAmount(carType, 10);

        var actual = _policy.CalculatePrice(carType, pickup, returnDate);

        actual.Should().Be(expected);
    }

    [Test]
    public void StandardPricing_HasCorrectName()
    {
        _policy.Name.Should().Be("Standard");
    }
}
