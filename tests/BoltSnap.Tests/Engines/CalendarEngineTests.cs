using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class CalendarEngineTests
{
    [Fact]
    public void 通常の日は週_通算日_残り日数_経過率を返す()
    {
        // Given: 2026-09-24（木）
        var date = new DateOnly(2026, 9, 24);

        // When
        var progress = CalendarEngine.GetProgress(date);

        // Then
        Assert.Equal(39, progress.IsoWeek);
        Assert.Equal(267, progress.DayOfYear);
        Assert.Equal(98, progress.RemainingDays);
        Assert.Equal(73, progress.ElapsedPercent);
    }

    [Fact]
    public void 元日は1日目で残り364日になる()
    {
        var progress = CalendarEngine.GetProgress(new DateOnly(2026, 1, 1));

        Assert.Equal(1, progress.DayOfYear);
        Assert.Equal(364, progress.RemainingDays);
    }

    [Fact]
    public void 大晦日は残り0日で経過率100になる()
    {
        var progress = CalendarEngine.GetProgress(new DateOnly(2026, 12, 31));

        Assert.Equal(365, progress.DayOfYear);
        Assert.Equal(0, progress.RemainingDays);
        Assert.Equal(100, progress.ElapsedPercent);
        Assert.Equal(53, progress.IsoWeek);
    }

    [Fact]
    public void うるう年は366日として扱う()
    {
        var progress = CalendarEngine.GetProgress(new DateOnly(2028, 3, 1));

        Assert.Equal(366, progress.DaysInYear);
        Assert.Equal(61, progress.DayOfYear);
        Assert.Equal(305, progress.RemainingDays);
    }

    [Fact]
    public void 前年末の日でもISO週が翌年の第1週になる()
    {
        var progress = CalendarEngine.GetProgress(new DateOnly(2025, 12, 29));

        Assert.Equal(1, progress.IsoWeek);
        Assert.Equal(2025, progress.Year);
    }
}
