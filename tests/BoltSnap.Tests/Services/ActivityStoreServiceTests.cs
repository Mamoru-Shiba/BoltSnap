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
    public void 記録した分は同じ日に読み出せる()
    {
        var store = new ActivityStoreService(_dir);

        var recorded = store.Record(new DateTime(2026, 9, 24, 10, 3, 20));

        Assert.True(recorded);
        var day = store.GetDay(new DateOnly(2026, 9, 24));
        Assert.True(day.IsSet(10 * 60 + 3));
        Assert.Equal(1, day.Count);
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
        Assert.Equal(1, store.GetDay(new DateOnly(2026, 9, 24)).Count);
    }

    [Fact]
    public void 再起動後も記録が残っている()
    {
        var first = new ActivityStoreService(_dir);
        first.Record(new DateTime(2026, 9, 24, 9, 0, 0));
        first.Record(new DateTime(2026, 9, 24, 9, 1, 0));

        var second = new ActivityStoreService(_dir);

        var day = second.GetDay(new DateOnly(2026, 9, 24));
        Assert.True(day.IsSet(540));
        Assert.True(day.IsSet(541));
        Assert.Equal(2, day.Count);
    }

    [Fact]
    public void 年間の稼働分数を日ごとに集計する()
    {
        var store = new ActivityStoreService(_dir);
        store.Record(new DateTime(2026, 1, 1, 8, 0, 0));
        store.Record(new DateTime(2026, 1, 1, 8, 1, 0));
        store.Record(new DateTime(2026, 1, 3, 8, 0, 0));

        var minutes = store.GetActiveMinutesByDay(2026);

        Assert.Equal(365, minutes.Length);
        Assert.Equal(2, minutes[0]);
        Assert.Equal(0, minutes[1]);
        Assert.Equal(1, minutes[2]);
    }

    [Fact]
    public void うるう年の年間集計は366日分になる()
    {
        var store = new ActivityStoreService(_dir);

        Assert.Equal(366, store.GetActiveMinutesByDay(2028).Length);
    }

    [Theory]
    [InlineData("truncated")]
    [InlineData("garbage")]
    public void 壊れたファイルでも起動でき空として扱う(string kind)
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "2026.bin");
        File.WriteAllBytes(path, kind == "truncated" ? [1, 2, 3] : new byte[66_000]);

        var store = new ActivityStoreService(_dir);

        Assert.Equal(0, store.GetDay(new DateOnly(2026, 9, 24)).Count);
        Assert.True(store.Record(new DateTime(2026, 9, 24, 10, 0, 0)));
        Assert.Equal(1, new ActivityStoreService(_dir).GetDay(new DateOnly(2026, 9, 24)).Count);
    }
}
