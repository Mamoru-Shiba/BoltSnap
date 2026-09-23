using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class BarTextFormatterTests
{
    [Fact]
    public void 週_日_残りを2行の文言にする()
    {
        var progress = CalendarEngine.GetProgress(new DateOnly(2026, 9, 24));

        var (line1, line2) = BarTextFormatter.Format(progress);

        Assert.Equal("第39週 267日目", line1);
        Assert.Equal("残り98日 (73%)", line2);
    }

    [Fact]
    public void どの日でも各行は30文字以内になる()
    {
        for (var date = new DateOnly(2028, 1, 1); date.Year == 2028; date = date.AddDays(1))
        {
            var (line1, line2) = BarTextFormatter.Format(CalendarEngine.GetProgress(date));

            Assert.InRange(line1.Length, 1, 30);
            Assert.InRange(line2.Length, 1, 30);
        }
    }
}
