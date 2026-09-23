namespace BoltSnap.Core.Engines;

/// <summary>帯に出す文言。1 行 30 文字以内に収める。</summary>
public static class BarTextFormatter
{
    public static (string Line1, string Line2) Format(YearProgress progress)
    {
        return (
            $"第{progress.IsoWeek}週 {progress.DayOfYear}日目",
            $"残り{progress.RemainingDays}日 ({progress.ElapsedPercent}%)");
    }
}
