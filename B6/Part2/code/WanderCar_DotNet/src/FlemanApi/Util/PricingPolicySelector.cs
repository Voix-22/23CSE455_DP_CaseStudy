
using FlemanApi.Models;
using Microsoft.Extensions.Options;

namespace FlemanApi.Util;

public sealed class PricingPolicySelector : IPricingPolicySelector
{
    private readonly IPricingPolicy _standard;
    private readonly IPricingPolicy _weekend;
    private readonly IPricingPolicy _seasonal;
    private readonly PricingSettings _settings;

    public PricingPolicySelector(
        StandardPricingPolicy standard,
        WeekendPricingPolicy weekend,
        SeasonalPricingPolicy seasonal,
        IOptions<PricingSettings> options)
    {
        _standard = standard;
        _weekend = weekend;
        _seasonal = seasonal;
        _settings = options.Value;
    }

    public IPricingPolicy SelectPolicy(
        CarType carType,
        DateTime pickupDate,
        DateTime returnDate)
    {
        if (_settings.PeakMonths.Contains(pickupDate.Month))
        {
            return _seasonal;
        }

        for (var date = pickupDate.Date; date < returnDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                return _weekend;
            }
        }

        return _standard;
    }
}
