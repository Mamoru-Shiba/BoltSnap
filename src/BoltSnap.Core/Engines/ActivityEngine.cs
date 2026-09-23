namespace BoltSnap.Core.Engines;

public static class ActivityEngine
{
    /// <summary>最後の入力からこの時間が経つと、稼働とみなさない。</summary>
    public static readonly TimeSpan IdleThreshold = TimeSpan.FromMinutes(5);

    public static bool IsActive(TimeSpan idle) => idle < IdleThreshold;

    public static int MinuteOfDay(DateTime time) => time.Hour * 60 + time.Minute;
}
