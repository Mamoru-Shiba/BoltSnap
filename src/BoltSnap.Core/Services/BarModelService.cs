using BoltSnap.Core.Engines;

namespace BoltSnap.Core.Services;

/// <summary>帯の描画に必要なデータ一式。</summary>
public sealed record BarModel(
    YearProgress Progress,
    IReadOnlyList<GrassCell> Grass,
    int GrassColumns,
    MinuteBitmap Today,
    int NowMinute);

public sealed class BarModelService(IActivityStore store)
{
    public BarModel Build(DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        return new BarModel(
            CalendarEngine.GetProgress(today),
            GrassLayoutEngine.Build(today.Year, today, store.GetActiveMinutesByDay(today.Year)),
            GrassLayoutEngine.ColumnCount(today.Year),
            store.GetDay(today),
            ActivityEngine.MinuteOfDay(now));
    }
}
