using BoltSnap.Core.Engines;

namespace BoltSnap.Core.Services;

public sealed class ActivityRecorder(IActivityStore store)
{
    /// <summary>無操作時間から稼働を判定し、稼働なら現在の分を記録する。記録が増えたときだけ true。</summary>
    public bool Update(DateTime now, TimeSpan idle)
    {
        return ActivityEngine.IsActive(idle) && store.Record(now);
    }
}
