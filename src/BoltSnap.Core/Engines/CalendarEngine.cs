using System.Globalization;

namespace BoltSnap.Core.Engines;

/// <summary>ある日付の、年の中での進み具合。</summary>
public readonly record struct YearProgress(int Year, int IsoWeek, int DayOfYear, int DaysInYear)
{
    public int RemainingDays => DaysInYear - DayOfYear;

    public int ElapsedPercent =>
        (int)Math.Round(DayOfYear * 100.0 / DaysInYear, MidpointRounding.AwayFromZero);
}

public static class CalendarEngine
{
    public static YearProgress GetProgress(DateOnly date)
    {
        return new YearProgress(
            date.Year,
            ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue)),
            date.DayOfYear,
            DateTime.IsLeapYear(date.Year) ? 366 : 365);
    }
}
