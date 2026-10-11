using FlemanApi.Models;
using FlemanApi.Util;
using FluentAssertions;

namespace FlemanApi.Tests.Util;

[TestFixture]
public class RentalRateCalculatorTests
{
    private static CarType MakeCarType(double daily = 100.0, double? weekly = 600.0, double? monthly = 2000.0) =>
        new()
        {
            CarTypeId = 1,
            CarTypeName = "Sedan",
            DailyRate = daily,
            WeeklyRate = weekly,
            MonthlyRate = monthly,
        };

    [Test]
    public void CalculateRentalAmount_FiveDays_UsesDailyRateOnly()
    {
        var carType = MakeCarType();

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 5);

        amount.Should().Be(5 * 100.0);
    }

    [Test]
    public void CalculateRentalAmount_TenDays_PacksOneWeekPlusThreeDays()
    {
        var carType = MakeCarType();

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 10);

        amount.Should().Be(600.0 + 3 * 100.0);
    }

    [Test]
    public void CalculateRentalAmount_ThirtyFiveDays_PacksOneMonthPlusFiveDays()
    {
        var carType = MakeCarType();

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 35);

        amount.Should().Be(2000.0 + 5 * 100.0);
    }

    [Test]
    public void CalculateRentalAmount_SixtyDays_PacksTwoMonths()
    {
        var carType = MakeCarType();

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 60);

        amount.Should().Be(2 * 2000.0);
    }

    [Test]
    public void CalculateRentalAmount_ZeroWeeklyRate_FallsThroughToDaily()
    {
        var carType = MakeCarType(weekly: 0.0, monthly: 0.0);

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 10);

        amount.Should().Be(10 * 100.0);
    }

    [Test]
    public void CalculateRentalAmount_NullWeeklyAndMonthlyRate_FallsThroughToDaily()
    {
        var carType = MakeCarType(weekly: null, monthly: null);

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 15);

        amount.Should().Be(15 * 100.0);
    }

    [Test]
    public void CalculateRentalAmount_ExactlySevenDays_UsesOneWeekOnly()
    {
        var carType = MakeCarType();

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 7);

        amount.Should().Be(600.0);
    }

    [Test]
    public void CalculateRentalAmount_ExactlyThirtyDays_UsesOneMonthOnly()
    {
        var carType = MakeCarType();

        var amount = RentalRateCalculator.CalculateRentalAmount(carType, 30);

        amount.Should().Be(2000.0);
    }
}
