using FlemanApi.Models;

namespace FlemanApi.Util;

public sealed class StandardPricingPolicy : IPricingPolicy
{
    public string Name => "Standard";

    public double CalculatePrice(
        CarType carType,
        DateTime pickupDate,
        DateTime returnDate)
    {
        if (returnDate <= pickupDate)
        {
            return 0.0;
        }

        var days = RentalDurationCalculator.DaysBetween(
            pickupDate,
            returnDate);

        return RentalRateCalculator.CalculateRentalAmount(
            carType,
            days);
    }
}