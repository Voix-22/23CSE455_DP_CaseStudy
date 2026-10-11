using FlemanApi.Models;
using Microsoft.Extensions.Options;

namespace FlemanApi.Util;

public sealed class SeasonalPricingPolicy : IPricingPolicy
{
    private readonly PricingSettings _settings;

    public SeasonalPricingPolicy(IOptions<PricingSettings> options)
    {
        _settings = options.Value;
    }

    public string Name => "Seasonal";

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

        if (!_settings.PeakMonths.Contains(pickupDate.Month))
        {
            return standardPrice;
        }

        return standardPrice *
               (1 + _settings.SeasonalSurchargePercent / 100.0);
    }
}