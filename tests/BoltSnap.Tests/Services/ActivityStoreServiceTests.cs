using BoltSnap.Core.Services;

namespace BoltSnap.Tests.Services;

public sealed class ActivityStoreServiceTests : IDisposable
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
    public void 同じ分を2回記録しても2回目は変更なしになる()
    {
        var store = new ActivityStoreService(_dir);

        Assert.True(store.Record(new DateTime(2026, 9, 24, 10, 3, 5)));
        Assert.False(store.Record(new DateTime(2026, 9, 24, 10, 3, 55)));
    }

    [Fact]
    public void 日付をまたぐと前日と翌日に別々に記録される()
    {
        var store = new ActivityStoreService(_dir);

        store.Record(new DateTime(2026, 9, 24, 23, 59, 0));
        store.Record(new DateTime(2026, 9, 25, 0, 0, 0));

        Assert.True(store.GetDay(new DateOnly(2026, 9, 24)).IsSet(1439));
        Assert.True(store.GetDay(new DateOnly(2026, 9, 25)).IsSet(0));
    }

    [Fact]
    public void 再起動後も記録が残っている()
    {
        new ActivityStoreService(_dir).Record(new DateTime(2026, 9, 24, 9, 0, 0));

        var day = new ActivityStoreService(_dir).GetDay(new DateOnly(2026, 9, 24));

        Assert.True(day.IsSet(540));
        Assert.Equal(1, day.Count);
    }

    [Fact]
    public void 壊れたファイルでも起動でき空として扱う()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllBytes(Path.Combine(_dir, "2026.bin"), [1, 2, 3]);

        var store = new ActivityStoreService(_dir);

        Assert.Equal(0, store.GetDay(new DateOnly(2026, 9, 24)).Count);
        Assert.True(store.Record(new DateTime(2026, 9, 24, 10, 0, 0)));
    }
}
