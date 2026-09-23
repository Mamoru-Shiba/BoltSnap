using BoltSnap.Core.Engines;

namespace BoltSnap.Tests.Engines;

public class ActivityEngineTests
{
    [Theory]
    [InlineData(299, true)]
    [InlineData(300, false)]
    public void 無操作5分未満は稼働とみなす(int idleSeconds, bool expected)
    {
        Assert.Equal(expected, ActivityEngine.IsActive(TimeSpan.FromSeconds(idleSeconds)));
    }
}
