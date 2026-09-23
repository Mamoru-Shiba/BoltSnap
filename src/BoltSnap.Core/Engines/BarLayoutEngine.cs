namespace BoltSnap.Core.Engines;

public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;

    public int Bottom => Y + Height;
}

public readonly record struct BarLayout(
    PixelRect Text,
    PixelRect Timeline,
    PixelRect Grass,
    int CellSize,
    int CellGap);

/// <summary>帯の中の、テキスト・時間帯バー・草の配置を計算する。左から順に並べる。</summary>
public static class BarLayoutEngine
{
    public const int BaseHeight = 32;

    private const int BaseMargin = 8;
    private const int BasePadding = 2;
    private const int BaseTextWidth = 120;
    private const int BaseTimelineHeight = 12;

    public static int BarHeight(double scale) => Scale(BaseHeight, scale);

    public static BarLayout Compute(int width, int height, double scale, int columns)
    {
        var margin = Scale(BaseMargin, scale);
        var padding = Scale(BasePadding, scale);
        var gap = Math.Max(1, Scale(1, scale));
        var cell = Math.Max(2, (height - 2 * padding - (GrassLayoutEngine.RowCount - 1) * gap) / GrassLayoutEngine.RowCount);

        var grassWidth = columns * (cell + gap) - gap;
        var grassHeight = GrassLayoutEngine.RowCount * (cell + gap) - gap;
        var grass = new PixelRect(width - margin - grassWidth, (height - grassHeight) / 2, grassWidth, grassHeight);

        var textWidth = Scale(BaseTextWidth, scale);
        var text = new PixelRect(margin, 0, textWidth, height);

        var timelineX = text.Right + margin;
        var timelineHeight = Scale(BaseTimelineHeight, scale);
        var timeline = new PixelRect(
            timelineX,
            (height - timelineHeight) / 2,
            Math.Max(0, grass.X - margin - timelineX),
            timelineHeight);

        return new BarLayout(text, timeline, grass, cell, gap);
    }

    private static int Scale(int value, double scale) => (int)Math.Round(value * scale);
}
