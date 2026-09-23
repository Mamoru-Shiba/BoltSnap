using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class GrassLayoutEngineTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(59, 1)]
    [InlineData(60, 2)]
    [InlineData(179, 2)]
    [InlineData(180, 3)]
    [InlineData(359, 3)]
    [InlineData(360, 4)]
    [InlineData(1440, 4)]
    public void 稼働分数から濃度を決める(int minutes, int expectedLevel)
    {
        Assert.Equal(expectedLevel, GrassLayoutEngine.LevelFor(minutes));
    }

    [Fact]
    public void 年の全日が1マスずつ並ぶ()
    {
        var cells = GrassLayoutEngine.Build(2026, new DateOnly(2026, 9, 24), []);

        Assert.Equal(365, cells.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), cells[0].Date);
        Assert.Equal(new DateOnly(2026, 12, 31), cells[^1].Date);
    }

    [Fact]
    public void うるう年は366マスになる()
    {
        var cells = GrassLayoutEngine.Build(2028, new DateOnly(2028, 3, 1), []);

        Assert.Equal(366, cells.Count);
    }

    [Fact]
    public void 月曜始まりの週_曜日で配置される()
    {
        // Given: 2026-01-01 は木曜
        var cells = GrassLayoutEngine.Build(2026, new DateOnly(2026, 9, 24), []);

        // Then: 木曜は 3 行目（月曜=0）、次の月曜は次の列の 0 行目
        Assert.Equal((0, 3), (cells[0].Column, cells[0].Row));
        Assert.Equal((1, 0), (cells[4].Column, cells[4].Row));
        Assert.Equal(new DateOnly(2026, 1, 5), cells[4].Date);
    }

    [Fact]
    public void 列数は年の日数と元日の曜日から決まる()
    {
        Assert.Equal(53, GrassLayoutEngine.ColumnCount(2026));
        Assert.Equal(53, GrassLayoutEngine.ColumnCount(2028));
        Assert.Equal(GrassLayoutEngine.Build(2026, new DateOnly(2026, 1, 1), []).Max(c => c.Column) + 1,
            GrassLayoutEngine.ColumnCount(2026));
    }

    [Fact]
    public void 未来の日は空マスで今日だけが今日として印付けされる()
    {
        var today = new DateOnly(2026, 9, 24);
        var minutes = Enumerable.Repeat(480, 365).ToArray();

        var cells = GrassLayoutEngine.Build(2026, today, minutes);

        Assert.All(cells.Where(c => c.Date > today), c =>
        {
            Assert.True(c.IsFuture);
            Assert.Equal(0, c.Level);
        });
        Assert.All(cells.Where(c => c.Date <= today), c => Assert.False(c.IsFuture));
        Assert.Single(cells, c => c.IsToday);
        Assert.Equal(today, cells.Single(c => c.IsToday).Date);
    }

    [Fact]
    public void 記録がない過去の日は濃度0になる()
    {
        var cells = GrassLayoutEngine.Build(2026, new DateOnly(2026, 9, 24), [60]);

        Assert.Equal(2, cells[0].Level);
        Assert.Equal(0, cells[1].Level);
    }
}
