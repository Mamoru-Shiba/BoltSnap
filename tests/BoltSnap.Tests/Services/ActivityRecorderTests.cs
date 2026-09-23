using BoltSnap.Core.Services;

namespace BoltSnap.Tests.Services;

public sealed class ActivityRecorderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "boltsnap-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void 無操作4分59秒は記録し_5分では記録しない()
    {
        var store = new ActivityStoreService(_dir);
        var recorder = new ActivityRecorder(store);

        Assert.True(recorder.Update(new DateTime(2026, 9, 24, 10, 4, 59), new TimeSpan(0, 4, 59)));
        Assert.False(recorder.Update(new DateTime(2026, 9, 24, 10, 5, 0), TimeSpan.FromMinutes(5)));

        var day = store.GetDay(new DateOnly(2026, 9, 24));
        Assert.True(day.IsSet(604));
        Assert.False(day.IsSet(605));
    }
}
