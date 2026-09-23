using BoltSnap.Core.Services;

namespace BoltSnap.Tests.Services;

public sealed class ActivityRecorderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "boltsnap-tests-" + Guid.NewGuid());
    private readonly ActivityStoreService _store;
    private readonly ActivityRecorder _recorder;

    public ActivityRecorderTests()
    {
        _store = new ActivityStoreService(_dir);
        _recorder = new ActivityRecorder(_store);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void 操作から3分後は稼働として記録する()
    {
        var recorded = _recorder.Update(new DateTime(2026, 9, 24, 10, 3, 0), TimeSpan.FromMinutes(3));

        Assert.True(recorded);
        Assert.True(_store.GetDay(new DateOnly(2026, 9, 24)).IsSet(603));
    }

    [Fact]
    public void 操作から5分後は記録しない()
    {
        var recorded = _recorder.Update(new DateTime(2026, 9, 24, 10, 5, 0), TimeSpan.FromMinutes(5));

        Assert.False(recorded);
        Assert.Equal(0, _store.GetDay(new DateOnly(2026, 9, 24)).Count);
    }

    [Fact]
    public void 無操作4分59秒はまだ稼働として記録する()
    {
        var recorded = _recorder.Update(
            new DateTime(2026, 9, 24, 10, 4, 59),
            new TimeSpan(0, 4, 59));

        Assert.True(recorded);
        Assert.True(_store.GetDay(new DateOnly(2026, 9, 24)).IsSet(604));
    }
}
