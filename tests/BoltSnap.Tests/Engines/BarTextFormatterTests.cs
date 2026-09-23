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
    [Theory]
    [InlineData(0, "稼働なし")]
    [InlineData(45, "稼働 45分")]
    [InlineData(120, "稼働 2時間")]
    [InlineData(135, "稼働 2時間15分")]
    public void ホバー時は日付と稼働時間を表示する(int minutes, string expectedLine2)
    {
        // Given: 2026-09-24（木）
        var cell = new GrassCell(new DateOnly(2026, 9, 24), 38, 3, 2, minutes, IsFuture: false, IsToday: false);

        var (line1, line2) = BarTextFormatter.FormatHover(cell);

        Assert.Equal("9月24日 (木)", line1);
        Assert.Equal(expectedLine2, line2);
    }

    [Fact]
    public void ホバーした未来の日はこれからと表示する()
    {
        var cell = new GrassCell(new DateOnly(2026, 12, 31), 52, 3, 0, 0, IsFuture: true, IsToday: false);

        var (_, line2) = BarTextFormatter.FormatHover(cell);

        Assert.Equal("これから", line2);
    }
}
