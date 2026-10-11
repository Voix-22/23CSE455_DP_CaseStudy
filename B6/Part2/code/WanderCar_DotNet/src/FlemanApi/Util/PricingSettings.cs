
namespace FlemanApi.Util;

public class PricingSettings
{
    public const string SectionName = "PricingSettings";

    // Extra percentage applied to rentals that include weekend days.
    public double WeekendSurchargePercent { get; set; } = 20.0;

    // Extra percentage applied when the pickup date is in a peak month.
    public double SeasonalSurchargePercent { get; set; } = 30.0;

    // Calendar months considered peak season (1 = January, 12 = December).
    public int[] PeakMonths { get; set; } = [12, 1, 4];
}
