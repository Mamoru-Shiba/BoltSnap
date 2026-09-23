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
    [Fact]
    public void 記録のある日のマスは稼働分数を持ち未来は0になる()
    {
        var cells = GrassLayoutEngine.Build(2026, new DateOnly(2026, 1, 3), [135, 0, 45, 999]);

        Assert.Equal(135, cells[0].Minutes);
        Assert.Equal(45, cells[2].Minutes);
        Assert.Equal(0, cells[3].Minutes);
    }

    [Fact]
    public void 座標から該当する日のマスを見つける()
    {
        // Given
        var today = new DateOnly(2026, 9, 24);
        var cells = GrassLayoutEngine.Build(2026, today, []);
        var layout = BarLayoutEngine.Compute(1920, 32, 1.0, GrassLayoutEngine.ColumnCount(2026));
        var step = layout.CellSize + layout.CellGap;
        var target = cells.Single(c => c.Date == new DateOnly(2026, 3, 10));

        // When: そのマスの中央を指す
        var hit = GrassLayoutEngine.HitTest(
            layout, cells,
            layout.Grass.X + target.Column * step + 1,
            layout.Grass.Y + target.Row * step + 1);

        // Then
        Assert.Equal(target.Date, hit?.Date);
    }

    [Fact]
    public void 元日より前の空きマスと草の外側は何にも当たらない()
    {
        var cells = GrassLayoutEngine.Build(2026, new DateOnly(2026, 9, 24), []);
        var layout = BarLayoutEngine.Compute(1920, 32, 1.0, GrassLayoutEngine.ColumnCount(2026));

        // 2026-01-01 は木曜なので、最初の列の月曜（行 0）は年の外
        Assert.Null(GrassLayoutEngine.HitTest(layout, cells, layout.Grass.X + 1, layout.Grass.Y + 1));
        Assert.Null(GrassLayoutEngine.HitTest(layout, cells, layout.Grass.X - 1, layout.Grass.Y + 5));
        Assert.Null(GrassLayoutEngine.HitTest(layout, cells, layout.Grass.Right, layout.Grass.Y + 5));
        Assert.Null(GrassLayoutEngine.HitTest(layout, cells, layout.Grass.X + 5, layout.Grass.Bottom));
    }
}
