using FlemanApi.Models;

namespace FlemanApi.Util;

// Shared by BookingService (booking-time estimate) and StaffService (the
// actual return invoice) so both price a rental the same way: pack whole
// months first, then whole weeks out of what's left, then bill the
// remaining days at the daily rate.
public static class RentalRateCalculator
{
    public static double CalculateRentalAmount(CarType carType, int days)
    {
        var dailyRate = carType.DailyRate;
        var weeklyRate = carType.WeeklyRate ?? 0.0;
        var monthlyRate = carType.MonthlyRate ?? 0.0;

        var remainingDays = days;
        var amount = 0.0;

        if (monthlyRate > 0 && remainingDays >= 30)
        {
            var months = remainingDays / 30;
            amount += months * monthlyRate;
            remainingDays %= 30;
        }
        if (weeklyRate > 0 && remainingDays >= 7)
        {
            var weeks = remainingDays / 7;
            amount += weeks * weeklyRate;
            remainingDays %= 7;
        }
        amount += remainingDays * dailyRate;

        return amount;
    }
}
