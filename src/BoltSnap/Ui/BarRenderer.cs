using BoltSnap.Core.Engines;
using BoltSnap.Core.Services;
using BoltSnap.Native;

namespace BoltSnap.Ui;

/// <summary>GDI で帯を描く。ブラシとフォントは作り置きし、描画のたびには生成しない。</summary>
internal sealed class BarRenderer : IDisposable
{
    private const uint TextColor = 0xD9D1C9; // #c9d1d9（COLORREF は 0x00BBGGRR）

    private readonly nint _background = Brush(13, 17, 23);
    private readonly nint _track = Brush(22, 27, 34);
    private readonly nint _tick = Brush(48, 54, 61);
    private readonly nint _active = Brush(57, 211, 83);
    private readonly nint _nowLine = Brush(240, 246, 252);
    private readonly nint _futureOutline = Brush(48, 54, 61);
    private readonly nint[] _levels =
    [
        Brush(22, 27, 34),
        Brush(14, 68, 41),
        Brush(0, 109, 50),
        Brush(38, 166, 65),
        Brush(57, 211, 83),
    ];

    private readonly nint _font;
    private readonly double _scale;

    public BarRenderer(double scale)
    {
        _scale = scale;
        _font = NativeMethods.CreateFontW(
            -(int)Math.Round(12 * scale), 0, 0, 0, NativeMethods.FW_NORMAL,
            0, 0, 0, NativeMethods.DEFAULT_CHARSET, 0, 0, NativeMethods.CLEARTYPE_QUALITY, 0, "Segoe UI");
    }

    public void Draw(nint hdc, int width, int height, BarModel model, GrassCell? hovered)
    {
        var whole = new RECT { Left = 0, Top = 0, Right = width, Bottom = height };
        NativeMethods.FillRect(hdc, ref whole, _background);

        var layout = BarLayoutEngine.Compute(width, height, _scale, model.GrassColumns);
        DrawText(hdc, layout, model, hovered);
        DrawTimeline(hdc, layout.Timeline, model);
        DrawGrass(hdc, layout, model, hovered);
    }

    public void Dispose()
    {
        foreach (var handle in new[] { _background, _track, _tick, _active, _nowLine, _futureOutline, _font })
        {
            NativeMethods.DeleteObject(handle);
        }

        foreach (var handle in _levels)
        {
            NativeMethods.DeleteObject(handle);
        }
    }

    private void DrawText(nint hdc, BarLayout layout, BarModel model, GrassCell? hovered)
    {
        // 草にカーソルがあるときは、その日の日付と稼働時間に切り替える
        var text = hovered is { } cell
            ? BarTextFormatter.FormatHover(cell)
            : BarTextFormatter.Format(model.Progress);
        var previousFont = NativeMethods.SelectObject(hdc, _font);
        NativeMethods.SetBkMode(hdc, NativeMethods.TRANSPARENT);
        NativeMethods.SetTextColor(hdc, TextColor);

        var area = new RECT { Left = layout.Text.X, Top = 0, Right = layout.Text.Right, Bottom = layout.Text.Height };
        const uint format = NativeMethods.DT_LEFT | NativeMethods.DT_VCENTER | NativeMethods.DT_SINGLELINE
            | NativeMethods.DT_NOPREFIX | NativeMethods.DT_END_ELLIPSIS;
        NativeMethods.DrawTextW(hdc, text, -1, ref area, format);

        NativeMethods.SelectObject(hdc, previousFont);
    }

    private void DrawTimeline(nint hdc, PixelRect timeline, BarModel model)
    {
        if (timeline.Width <= 0)
        {
            return;
        }

        Fill(hdc, timeline.X, timeline.Y, timeline.Width, timeline.Height, _track);

        foreach (var hour in new[] { 6, 12, 18 })
        {
            Fill(hdc, timeline.X + timeline.Width * hour / 24, timeline.Y, 1, timeline.Height, _tick);
        }

        foreach (var (start, length) in model.Today.Runs())
        {
            var x0 = timeline.X + (int)((long)timeline.Width * start / MinuteBitmap.MinutesPerDay);
            var x1 = timeline.X + (int)((long)timeline.Width * (start + length) / MinuteBitmap.MinutesPerDay);
            Fill(hdc, x0, timeline.Y, Math.Max(1, x1 - x0), timeline.Height, _active);
        }

        var extra = (int)Math.Round(2 * _scale);
        var nowX = timeline.X + (int)((long)timeline.Width * model.NowMinute / MinuteBitmap.MinutesPerDay);
        Fill(hdc, nowX, timeline.Y - extra, Math.Max(2, (int)Math.Round(_scale)), timeline.Height + extra * 2, _nowLine);
    }

    private void DrawGrass(nint hdc, BarLayout layout, BarModel model, GrassCell? hovered)
    {
        var step = layout.CellSize + layout.CellGap;
        foreach (var cell in model.Grass)
        {
            var rect = new RECT
            {
                Left = layout.Grass.X + cell.Column * step,
                Top = layout.Grass.Y + cell.Row * step,
            };
            rect.Right = rect.Left + layout.CellSize;
            rect.Bottom = rect.Top + layout.CellSize;

            if (cell.IsFuture)
            {
                NativeMethods.FrameRect(hdc, ref rect, hovered?.Date == cell.Date ? _nowLine : _futureOutline);
                continue;
            }

            NativeMethods.FillRect(hdc, ref rect, _levels[cell.Level]);
            if (cell.IsToday || hovered?.Date == cell.Date)
            {
                NativeMethods.FrameRect(hdc, ref rect, _nowLine);
            }
        }
    }

    private static void Fill(nint hdc, int x, int y, int width, int height, nint brush)
    {
        var rect = new RECT { Left = x, Top = y, Right = x + width, Bottom = y + height };
        NativeMethods.FillRect(hdc, ref rect, brush);
    }

    private static nint Brush(int r, int g, int b) =>
        NativeMethods.CreateSolidBrush((uint)(r | (g << 8) | (b << 16)));
}
