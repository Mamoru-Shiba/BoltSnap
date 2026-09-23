using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class ActivityEngineTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(299, true)]
    [InlineData(300, false)]
    [InlineData(3600, false)]
    public void 無操作5分未満は稼働とみなす(int idleSeconds, bool expected)
    {
        var active = ActivityEngine.IsActive(TimeSpan.FromSeconds(idleSeconds));

        Assert.Equal(expected, active);
    }

    [Fact]
    public void 時刻から1日の中の分を求める()
    {
        Assert.Equal(0, ActivityEngine.MinuteOfDay(new DateTime(2026, 1, 1, 0, 0, 30)));
        Assert.Equal(1439, ActivityEngine.MinuteOfDay(new DateTime(2026, 1, 1, 23, 59, 59)));
    }
}
