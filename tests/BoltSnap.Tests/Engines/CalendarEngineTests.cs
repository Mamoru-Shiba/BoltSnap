using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class CalendarEngineTests
{
    [Fact]
    public void 通常の日は週_通算日_残り日数_経過率を返す()
    {
        // 2026-09-24（木）
        var progress = CalendarEngine.GetProgress(new DateOnly(2026, 9, 24));

        Assert.Equal((39, 267, 98, 73), (progress.IsoWeek, progress.DayOfYear, progress.RemainingDays, progress.ElapsedPercent));
    }

    [Fact]
    public void 大晦日は残り0日で経過率100になる()
    {
        var progress = CalendarEngine.GetProgress(new DateOnly(2026, 12, 31));

        Assert.Equal((365, 0, 100, 53), (progress.DayOfYear, progress.RemainingDays, progress.ElapsedPercent, progress.IsoWeek));
    }

    [Fact]
    public void うるう年は366日として扱う()
    {
        var progress = CalendarEngine.GetProgress(new DateOnly(2028, 3, 1));

        Assert.Equal((366, 61, 305), (progress.DaysInYear, progress.DayOfYear, progress.RemainingDays));
    }

    [Fact]
    public void 前年末の日でもISO週が翌年の第1週になる()
    {
        Assert.Equal(1, CalendarEngine.GetProgress(new DateOnly(2025, 12, 29)).IsoWeek);
    }
}
