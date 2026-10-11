
using FlemanApi.Models;

namespace FlemanApi.Util;

public interface IPricingPolicySelector
{
    IPricingPolicy SelectPolicy(
        CarType carType,
        DateTime pickupDate,
        DateTime returnDate);
}
