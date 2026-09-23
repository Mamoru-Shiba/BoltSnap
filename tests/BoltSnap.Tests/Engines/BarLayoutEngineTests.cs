using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class BarLayoutEngineTests
{
    [Theory]
    [InlineData(1.0, 32)]
    [InlineData(1.5, 48)]
    [InlineData(2.0, 64)]
    public void 帯の高さはDPI倍率に比例する(double scale, int expected)
    {
        Assert.Equal(expected, BarLayoutEngine.BarHeight(scale));
    }

    [Fact]
    public void 草は右端にはみ出さず7行が高さに収まる()
    {
        var layout = BarLayoutEngine.Compute(1920, 32, 1.0, 53);

        Assert.True(layout.Grass.Right <= 1920);
        Assert.True(layout.Grass.Y >= 0);
        Assert.True(layout.Grass.Bottom <= 32);
        Assert.Equal(3, layout.CellSize);
    }

    [Fact]
    public void テキスト_時間帯_草が左から重ならずに並ぶ()
    {
        var layout = BarLayoutEngine.Compute(1920, 32, 1.0, 53);

        Assert.True(layout.Text.Right <= layout.Timeline.X);
        Assert.True(layout.Timeline.Right <= layout.Grass.X);
        Assert.True(layout.Timeline.Width > 0);
    }

    [Fact]
    public void 画面が極端に狭くても時間帯の幅は負にならない()
    {
        var layout = BarLayoutEngine.Compute(300, 32, 1.0, 53);

        Assert.Equal(0, layout.Timeline.Width);
    }
}
