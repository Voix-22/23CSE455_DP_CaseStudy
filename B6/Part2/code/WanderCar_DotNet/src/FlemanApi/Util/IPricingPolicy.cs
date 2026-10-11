
using FlemanApi.Models;

namespace FlemanApi.Util;

public interface IPricingPolicy
{
    string Name { get; }

    double CalculatePrice(
        CarType carType,
        DateTime pickupDate,
        DateTime returnDate);
}
