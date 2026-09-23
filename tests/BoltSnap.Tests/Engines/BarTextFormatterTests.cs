using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class BarTextFormatterTests
{
    [Fact]
    public void 週_日_残りを2行の文言にする()
    {
        var text = BarTextFormatter.Format(CalendarEngine.GetProgress(new DateOnly(2026, 9, 24)));

        Assert.Equal("第39週 267日目  残り98日 (73%)", text);
    }

    [Fact]
    public void どの日でも文言は30文字以内になる()
    {
        for (var date = new DateOnly(2028, 1, 1); date.Year == 2028; date = date.AddDays(1))
        {
            var text = BarTextFormatter.Format(CalendarEngine.GetProgress(date));

            Assert.InRange(text.Length, 1, 30);
        }
    }

    [Fact]
    public void ホバー時は日付と稼働時間_未来はこれからと表示する()
    {
        var date = new DateOnly(2026, 9, 24); // 木曜
        GrassCell Cell(int minutes, bool future = false) => new(date, 38, 3, 2, minutes, future, false);

        Assert.Equal("9月24日 (木)  稼働なし", BarTextFormatter.FormatHover(Cell(0)));
        Assert.Equal("9月24日 (木)  稼働 2時間15分", BarTextFormatter.FormatHover(Cell(135)));
        Assert.Equal("9月24日 (木)  これから", BarTextFormatter.FormatHover(Cell(0, future: true)));
    }
}
