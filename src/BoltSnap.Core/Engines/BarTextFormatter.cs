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

    private static readonly string[] DayNames = ["日", "月", "火", "水", "木", "金", "土"];

    /// <summary>草にカーソルを重ねたときの 2 行。日付と稼働時間。</summary>
    public static (string Line1, string Line2) FormatHover(GrassCell cell)
    {
        var date = $"{cell.Date.Month}月{cell.Date.Day}日 ({DayNames[(int)cell.Date.DayOfWeek]})";
        if (cell.IsFuture)
        {
            return (date, "これから");
        }

        return (date, cell.Minutes == 0 ? "稼働なし" : $"稼働 {FormatDuration(cell.Minutes)}");
    }

    private static string FormatDuration(int minutes)
    {
        var hours = minutes / 60;
        var rest = minutes % 60;
        return hours == 0 ? $"{rest}分" : rest == 0 ? $"{hours}時間" : $"{hours}時間{rest}分";
    }
}
