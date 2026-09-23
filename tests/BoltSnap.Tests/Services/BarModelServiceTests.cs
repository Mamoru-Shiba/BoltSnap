using BoltSnap.Core.Services;

namespace BoltSnap.Tests.Services;

public sealed class BarModelServiceTests : IDisposable
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
    public void 記録が空でも帯の表示に必要な情報がそろう()
    {
        var service = new BarModelService(new ActivityStoreService(_dir));

        var model = service.Build(new DateTime(2026, 9, 24, 12, 0, 0));

        Assert.Equal(267, model.Progress.DayOfYear);
        Assert.Equal(365, model.Grass.Count);
        Assert.Equal(53, model.GrassColumns);
        Assert.Equal(0, model.Today.Count);
        Assert.Equal(720, model.NowMinute);
    }

    [Fact]
    public void 今日の稼働が時間帯と草の今日のマスに反映される()
    {
        var store = new ActivityStoreService(_dir);
        for (var minute = 0; minute < 90; minute++)
        {
            store.Record(new DateTime(2026, 9, 24, 9, 0, 0).AddMinutes(minute));
        }

        var model = new BarModelService(store).Build(new DateTime(2026, 9, 24, 12, 0, 0));

        Assert.Equal(90, model.Today.Count);
        var todayCell = model.Grass.Single(c => c.IsToday);
        Assert.Equal(2, todayCell.Level);
    }
}
