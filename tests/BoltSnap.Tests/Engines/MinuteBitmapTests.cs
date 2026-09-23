using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class MinuteBitmapTests
{
    [Fact]
    public void 立てた分だけが稼働になり件数が数えられる()
    {
        var bitmap = new MinuteBitmap();

        bitmap.Set(0);
        bitmap.Set(1439);

        Assert.True(bitmap.IsSet(0));
        Assert.True(bitmap.IsSet(1439));
        Assert.False(bitmap.IsSet(1));
        Assert.Equal(2, bitmap.Count);
    }

    [Fact]
    public void 同じ分を再度立てても変更なしになる()
    {
        var bitmap = new MinuteBitmap();

        Assert.True(bitmap.Set(600));
        Assert.False(bitmap.Set(600));
        Assert.Equal(1, bitmap.Count);
    }

    [Fact]
    public void 連続した稼働は1つの区間にまとまる()
    {
        var bitmap = new MinuteBitmap();
        foreach (var minute in new[] { 540, 541, 542, 600, 1439 })
        {
            bitmap.Set(minute);
        }

        var runs = bitmap.Runs();

        Assert.Equal([(540, 3), (600, 1), (1439, 1)], runs);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1440)]
    public void 範囲外の分は拒否する(int minute)
    {
        var bitmap = new MinuteBitmap();

        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Set(minute));
    }

    [Fact]
    public void バイト列から復元できる()
    {
        var original = new MinuteBitmap();
        original.Set(123);

        var restored = new MinuteBitmap(original.AsSpan());

        Assert.True(restored.IsSet(123));
        Assert.Equal(1, restored.Count);
    }
}
