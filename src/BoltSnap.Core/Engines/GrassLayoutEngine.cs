namespace BoltSnap.Core.Engines;

/// <summary>草の 1 マス。Column は週（0 始まり）、Row は月曜 0 〜 日曜 6。</summary>
public readonly record struct GrassCell(
    DateOnly Date,
    int Column,
    int Row,
    int Level,
    bool IsFuture,
    bool IsToday);

public static class GrassLayoutEngine
{
    public const int MaxLevel = 4;
    public const int RowCount = 7;

    /// <summary>濃度 1〜4 になる最小の稼働分数。</summary>
    private static readonly int[] LevelMinMinutes = [1, 60, 180, 360];

    public static int LevelFor(int activeMinutes)
    {
        var level = 0;
        foreach (var min in LevelMinMinutes)
        {
            if (activeMinutes >= min)
            {
                level++;
            }
        }

        return level;
    }

    public static int ColumnCount(int year)
    {
        var jan1 = new DateOnly(year, 1, 1);
        var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        return (RowOf(jan1) + daysInYear + RowCount - 1) / RowCount;
    }

    /// <param name="activeMinutesByDay">その年の通算日 - 1 をインデックスとする稼働分数。足りない日は 0。</param>
    public static IReadOnlyList<GrassCell> Build(
        int year,
        DateOnly today,
        IReadOnlyList<int> activeMinutesByDay)
    {
        var jan1 = new DateOnly(year, 1, 1);
        var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        var offset = RowOf(jan1);
        var cells = new GrassCell[daysInYear];

        for (var i = 0; i < daysInYear; i++)
        {
            var date = jan1.AddDays(i);
            var minutes = i < activeMinutesByDay.Count ? activeMinutesByDay[i] : 0;
            var isFuture = date > today;
            cells[i] = new GrassCell(
                date,
                (offset + i) / RowCount,
                (offset + i) % RowCount,
                isFuture ? 0 : LevelFor(minutes),
                isFuture,
                date == today);
        }

        return cells;
    }

    private static int RowOf(DateOnly date) => ((int)date.DayOfWeek + 6) % 7;
}
