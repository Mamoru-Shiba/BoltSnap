using BoltSnap.Core.Engines;

namespace BoltSnap.Core.Services;

public interface IActivityStore
{
    /// <summary>指定の時刻の分を稼働として記録する。新しく記録したときだけ true。</summary>
    bool Record(DateTime moment);

    MinuteBitmap GetDay(DateOnly date);

    /// <summary>その年の日ごとの稼働分数。通算日 - 1 がインデックス。</summary>
    int[] GetActiveMinutesByDay(int year);
}
