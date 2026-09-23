using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class MinuteBitmapTests
{
    [Fact]
    public void 連続した稼働は1つの区間にまとまる()
    {
        var bitmap = new MinuteBitmap();
        foreach (var minute in new[] { 540, 541, 542, 600, 1439 })
        {
            bitmap.Set(minute);
        }

        Assert.Equal([(540, 3), (600, 1), (1439, 1)], bitmap.Runs());
    }

    [Fact]
    public void 範囲外の分は拒否する()
    {
        var bitmap = new MinuteBitmap();

        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Set(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Set(1440));
    }
}
