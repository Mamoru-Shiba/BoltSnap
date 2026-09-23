using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class GrassLayoutEngineTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(59, 1)]
    [InlineData(60, 2)]
    [InlineData(360, 4)]
    public void 稼働分数から濃度を決める(int minutes, int expectedLevel)
    {
        Assert.Equal(expectedLevel, GrassLayoutEngine.LevelFor(minutes));
    }

    [Fact]
    public void 月曜始まりの週_曜日で配置される()
    {
        // 2026-01-01 は木曜。次の月曜は次の列の 0 行目
        var cells = GrassLayoutEngine.Build(2026, new DateOnly(2026, 9, 24), []);

        Assert.Equal((0, 3), (cells[0].Column, cells[0].Row));
        Assert.Equal((1, 0), (cells[4].Column, cells[4].Row));
        Assert.Equal(365, cells.Count);
    }

    [Fact]
    public void 未来の日は空マスで今日だけが今日として印付けされる()
    {
        var today = new DateOnly(2026, 9, 24);

        var cells = GrassLayoutEngine.Build(2026, today, Enumerable.Repeat(480, 365).ToArray());

        Assert.All(cells.Where(c => c.Date > today), c => Assert.True(c.IsFuture && c.Level == 0));
        Assert.Equal(today, cells.Single(c => c.IsToday).Date);
    }

    [Fact]
    public void 座標から日のマスを見つけ_年の外や草の外は何にも当たらない()
    {
        var cells = GrassLayoutEngine.Build(2026, new DateOnly(2026, 9, 24), []);
        var layout = BarLayoutEngine.Compute(1920, 32, 1.0, GrassLayoutEngine.ColumnCount(2026));
        var stepX = layout.CellWidth + layout.CellGap;
        var stepY = layout.CellHeight + layout.CellGap;
        var target = cells.Single(c => c.Date == new DateOnly(2026, 3, 10));

        var hit = GrassLayoutEngine.HitTest(
            layout, cells, layout.Grass.X + target.Column * stepX + 1, layout.Grass.Y + target.Row * stepY + 1);

        Assert.Equal(target.Date, hit?.Date);
        // 最初の列の月曜（行 0）は年の外
        Assert.Null(GrassLayoutEngine.HitTest(layout, cells, layout.Grass.X + 1, layout.Grass.Y + 1));
        Assert.Null(GrassLayoutEngine.HitTest(layout, cells, layout.Grass.Right, layout.Grass.Y + 5));
    }
}
