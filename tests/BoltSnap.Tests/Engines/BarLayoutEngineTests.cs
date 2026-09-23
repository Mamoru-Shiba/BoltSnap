using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class BarLayoutEngineTests
{
    [Fact]
    public void 帯の高さはDPI倍率に比例する()
    {
        Assert.Equal(32, BarLayoutEngine.BarHeight(1.0));
        Assert.Equal(48, BarLayoutEngine.BarHeight(1.5));
    }

    [Fact]
    public void テキスト_時間帯_草が画面内で重ならずに並ぶ()
    {
        var layout = BarLayoutEngine.Compute(1920, 32, 1.0, 53);

        Assert.True(layout.Text.Right <= layout.Timeline.X);
        Assert.True(layout.Timeline.Right <= layout.Grass.X);
        Assert.True(layout.Grass.Right <= 1920 && layout.Grass.Y >= 0 && layout.Grass.Bottom <= 32);
    }

    [Fact]
    public void 画面が極端に狭くても時間帯の幅は負にならない()
    {
        Assert.Equal(0, BarLayoutEngine.Compute(300, 32, 1.0, 53).Timeline.Width);
    }
}
