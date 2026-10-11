using FlemanApi.Models;
using Microsoft.Extensions.Options;

namespace FlemanApi.Util;

public sealed class WeekendPricingPolicy : IPricingPolicy
{
    private readonly PricingSettings _settings;

    public WeekendPricingPolicy(IOptions<PricingSettings> options)
    {
        _settings = options.Value;
    }

    public string Name => "Weekend";

    public double CalculatePrice(
        CarType carType,
        DateTime pickupDate,
        DateTime returnDate)
    {
        var days = RentalDurationCalculator.DaysBetween(
            pickupDate,
            returnDate);

        var standardPrice =
            RentalRateCalculator.CalculateRentalAmount(carType, days);

        var weekendDays = 0;
        var currentDate = pickupDate.Date;

        while (currentDate < returnDate.Date)
        {
            if (currentDate.DayOfWeek is DayOfWeek.Saturday
                or DayOfWeek.Sunday)
            {
                weekendDays++;
            }

            currentDate = currentDate.AddDays(1);
        }

        if (weekendDays == 0)
        {
            return standardPrice;
        }

        return standardPrice *
               (1 + _settings.WeekendSurchargePercent / 100.0);
    }
}