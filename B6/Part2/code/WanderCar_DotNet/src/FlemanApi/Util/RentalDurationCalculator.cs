
namespace FlemanApi.Util;

public static class RentalDurationCalculator
{
    public static int DaysBetween(DateTime pickup, DateTime returnDate)
    {
        var hours = (returnDate - pickup).TotalHours;
        var fullDays = (long)Math.Ceiling(hours / 24.0);

        return (int)Math.Max(1, fullDays);
    }
}
