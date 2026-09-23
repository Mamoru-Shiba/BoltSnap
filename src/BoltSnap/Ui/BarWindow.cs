using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BoltSnap.Core.Engines;
using BoltSnap.Core.Services;
using BoltSnap.Native;
using BoltSnap.Services;

namespace BoltSnap.Ui;

/// <summary>タスクバーの通知領域の左に重ねて表示される帯のウィンドウ。</summary>
internal sealed unsafe class BarWindow
{
    private const string ClassName = "BoltSnapBar";
    private const nuint PollTimerId = 1;
    private const uint PollIntervalMs = 10_000;
    private const nuint PlaceTimerId = 2;
    private const uint PlaceIntervalMs = 2_000;
    private const uint TrayIconId = 1;
    private const nuint MenuAutoStart = 1;
    private const nuint MenuExit = 2;

    private static BarWindow? s_instance;

    private readonly ActivityRecorder _recorder;
    private readonly BarModelService _modelService;
    private BarRenderer? _renderer;
    private BarModel? _model;
    private nint _hwnd;
    private double _scale = 1.0;
    private long _lastMinuteStamp = -1;
    private GrassCell? _hovered;
    private bool _trackingMouse;
    private nint _trayIcon;
    private uint _taskbarCreatedMessage;
    private PixelRect _placement;
    private bool _shown;
    private FreeRegion? _freeRegion;
    private int _placeTicks;

    public BarWindow(IActivityStore store)
    {
        _recorder = new ActivityRecorder(store);
        _modelService = new BarModelService(store);
    }

    public void Run()
    {
        s_instance = this;
        _taskbarCreatedMessage = NativeMethods.RegisterWindowMessageW("TaskbarCreated");

        var instance = NativeMethods.GetModuleHandleW(null);
        var className = Marshal.StringToHGlobalUni(ClassName);
        var wc = new WNDCLASSEXW
        {
            CbSize = (uint)sizeof(WNDCLASSEXW),
            LpfnWndProc = &WndProc,
            HInstance = instance,
            HCursor = NativeMethods.LoadCursorW(0, NativeMethods.IDC_ARROW),
            LpszClassName = className,
        };
        NativeMethods.RegisterClassExW(ref wc);

        _hwnd = NativeMethods.CreateWindowExW(
            NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_TOPMOST | NativeMethods.WS_EX_NOACTIVATE,
            className, "BoltSnap", NativeMethods.WS_POPUP,
            0, 0, 0, 0, 0, 0, instance, 0);
        if (_hwnd == 0)
        {
            return;
        }

        Reposition(force: true);
        AddTrayIcon();
        Poll();
        RefreshModel(force: true);
        NativeMethods.SetTimer(_hwnd, PollTimerId, PollIntervalMs, 0);
        NativeMethods.SetTimer(_hwnd, PlaceTimerId, PlaceIntervalMs, 0);

        while (NativeMethods.GetMessageW(out var msg, 0, 0, 0) > 0)
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessageW(ref msg);
        }

        _renderer?.Dispose();
        if (_trayIcon != 0)
        {
            NativeMethods.DestroyIcon(_trayIcon);
        }

        Marshal.FreeHGlobal(className);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint WndProc(nint hwnd, uint msg, nint wParam, nint lParam)
    {
        try
        {
            var self = s_instance;
            if (self is not null && self._hwnd == hwnd)
            {
                var (handled, result) = self.HandleMessage(msg, wParam, lParam);
                if (handled)
                {
                    return result;
                }
            }
        }
        catch (Exception)
        {
            // 例外をネイティブ側に漏らさない。既定の処理へ回す
        }

        return NativeMethods.DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private (bool Handled, nint Result) HandleMessage(uint msg, nint wParam, nint lParam)
    {
        if (msg == _taskbarCreatedMessage && msg != 0)
        {
            // エクスプローラーの再起動後に、トレイアイコンを登録し直して配置を取り直す
            AddTrayIcon();
            Reposition(force: true);
            return (true, 0);
        }

        switch (msg)
        {
            case NativeMethods.WM_PAINT:
                OnPaint();
                return (true, 0);
            case NativeMethods.WM_ERASEBKGND:
                return (true, 1);
            case NativeMethods.WM_MOUSEACTIVATE:
                return (true, NativeMethods.MA_NOACTIVATE);
            case NativeMethods.WM_MOUSEMOVE:
                OnMouseMove((short)(lParam & 0xFFFF), (short)((lParam >> 16) & 0xFFFF));
                return (true, 0);
            case NativeMethods.WM_MOUSELEAVE:
                _trackingMouse = false;
                SetHovered(null);
                return (true, 0);
            case NativeMethods.WM_TIMER:
                if ((nuint)wParam == PollTimerId)
                {
                    Poll();
                }
                else if ((nuint)wParam == PlaceTimerId)
                {
                    // タスクバーが前面に出ても帯が隠れないよう、位置と前後関係を保つ
                    Reposition(force: false);
                }

                return (true, 0);
            case NativeMethods.WM_TIMECHANGE:
                RefreshModel(force: true);
                return (true, 0);
            case NativeMethods.WM_DISPLAYCHANGE:
            case NativeMethods.WM_DPICHANGED:
                Reposition(force: true);
                return (true, 0);
            case NativeMethods.WM_TRAY:
                if ((uint)(lParam & 0xFFFF) == NativeMethods.WM_RBUTTONUP)
                {
                    ShowMenu();
                }

                return (true, 0);
            case NativeMethods.WM_RBUTTONUP:
                ShowMenu();
                return (true, 0);
            case NativeMethods.WM_DESTROY:
                NativeMethods.PostQuitMessage(0);
                return (true, 0);
            default:
                return (false, 0);
        }
    }

    private void Poll()
    {
        var now = DateTime.Now;
        var recorded = _recorder.Update(now, InputIdleService.GetIdle());
        RefreshModel(force: recorded, now);
    }

    /// <summary>分が変わったとき（または強制時）だけモデルを作り直して再描画する。</summary>
    private void RefreshModel(bool force, DateTime? at = null)
    {
        var now = at ?? DateTime.Now;
        var stamp = now.Ticks / TimeSpan.TicksPerMinute;
        if (!force && stamp == _lastMinuteStamp)
        {
            return;
        }

        var minuteChanged = stamp != _lastMinuteStamp;
        _lastMinuteStamp = stamp;
        _model = _modelService.Build(now);
        NativeMethods.InvalidateRect(_hwnd, 0, 0);

        if (minuteChanged)
        {
            // 常駐アプリなので、1 分ごとに不要なデータを回収し、使っていないページを OS に返す
            GC.Collect();
            NativeMethods.SetProcessWorkingSetSize(NativeMethods.GetCurrentProcess(), -1, -1);
        }
    }

    private void OnMouseMove(int x, int y)
    {
        if (!_trackingMouse)
        {
            var track = new TRACKMOUSEEVENT
            {
                CbSize = (uint)sizeof(TRACKMOUSEEVENT),
                DwFlags = NativeMethods.TME_LEAVE,
                HwndTrack = _hwnd,
            };
            _trackingMouse = NativeMethods.TrackMouseEvent(ref track) != 0;
        }

        if (_model is null)
        {
            return;
        }

        NativeMethods.GetClientRect(_hwnd, out var client);
        var layout = BarLayoutEngine.Compute(client.Right - client.Left, client.Bottom - client.Top, _scale, _model.GrassColumns);
        SetHovered(GrassLayoutEngine.HitTest(layout, _model.Grass, x, y));
    }

    private void SetHovered(GrassCell? cell)
    {
        if (_hovered?.Date == cell?.Date)
        {
            return;
        }

        _hovered = cell;
        NativeMethods.InvalidateRect(_hwnd, 0, 0);
    }

    private void UpdateScale(uint dpi)
    {
        var scale = Math.Max(1.0, dpi / 96.0);
        if (Math.Abs(scale - _scale) < 0.001 && _renderer is not null)
        {
            return;
        }

        _scale = scale;
        _renderer?.Dispose();
        _renderer = new BarRenderer(scale);
    }

    private void OnPaint()
    {
        var hdc = NativeMethods.BeginPaint(_hwnd, out var ps);
        NativeMethods.GetClientRect(_hwnd, out var client);
        var width = client.Right - client.Left;
        var height = client.Bottom - client.Top;

        if (_renderer is not null && _model is not null && width > 0 && height > 0)
        {
            // ちらつき防止のため、メモリ上に描いてから一度に転送する
            var memDc = NativeMethods.CreateCompatibleDC(hdc);
            var bitmap = NativeMethods.CreateCompatibleBitmap(hdc, width, height);
            var previous = NativeMethods.SelectObject(memDc, bitmap);

            _renderer.Draw(memDc, width, height, _model, _hovered);
            NativeMethods.BitBlt(hdc, 0, 0, width, height, memDc, 0, 0, NativeMethods.SRCCOPY);

            NativeMethods.SelectObject(memDc, previous);
            NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(memDc);
        }

        NativeMethods.EndPaint(_hwnd, ref ps);
    }

    /// <summary>
    /// タスクバーの通知領域の左に重ねて配置する。
    /// タスクバーが見つからない・縦置きのときは、画面下端の右に置く。
    /// </summary>
    private void Reposition(bool force)
    {
        var (taskbar, trayLeft, dpi, taskbarHwnd) = LocateTaskbar();
        UpdateScale(dpi);

        // アイコンの並びが変わることがあるので、空きは 16 秒ごと（2 秒タイマーの 8 回に 1 回）に測り直す。
        // 測定は別プロセスで行い、結果が出ていれば取り込む
        if (taskbarHwnd == 0)
        {
            _freeRegion = null;
        }
        else
        {
            if (force || _placeTicks++ % 8 == 0)
            {
                TaskbarProbeService.BeginProbe(taskbarHwnd, trayLeft);
                if (force)
                {
                    // 起動時などは、最初の位置がずれて見えないよう少しだけ待つ
                    ApplyProbe(TaskbarProbeService.WaitForResult(1500, out var first), first);
                }
            }

            ApplyProbe(TaskbarProbeService.Collect(out var region), region);
        }

        var target = _freeRegion is { } free
            ? BarLayoutEngine.PlaceInRegion(taskbar, free.Left, free.Right, _scale)
            : BarLayoutEngine.PlaceOnTaskbar(taskbar, trayLeft, _scale);
        var moved = force || target != _placement;
        _placement = target;

        if (IsFullScreenAppRunning())
        {
            if (_shown)
            {
                NativeMethods.ShowWindow(_hwnd, NativeMethods.SW_HIDE);
                _shown = false;
            }

            return;
        }

        var flags = NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW;
        if (!moved && _shown)
        {
            flags |= NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE;
        }

        NativeMethods.SetWindowPos(
            _hwnd, NativeMethods.HWND_TOPMOST, target.X, target.Y, target.Width, target.Height, flags);
        _shown = true;
        if (moved)
        {
            NativeMethods.InvalidateRect(_hwnd, 0, 0);
        }
    }

    private void ApplyProbe(ProbeState state, FreeRegion region)
    {
        switch (state)
        {
            case ProbeState.Done:
                _freeRegion = region;
                break;
            case ProbeState.Failed:
                _freeRegion = null;
                break;
        }
    }

    private (PixelRect Taskbar, int TrayLeft, uint Dpi, nint TaskbarHwnd) LocateTaskbar()
    {
        var taskbar = NativeMethods.FindWindowW("Shell_TrayWnd", null);
        if (taskbar != 0
            && NativeMethods.GetWindowRect(taskbar, out var rect) != 0
            && rect.Right - rect.Left > rect.Bottom - rect.Top)
        {
            var trayLeft = rect.Right;
            var notify = NativeMethods.FindWindowExW(taskbar, 0, "TrayNotifyWnd", null);
            if (notify != 0 && NativeMethods.GetWindowRect(notify, out var tray) != 0 && tray.Left > rect.Left)
            {
                trayLeft = tray.Left;
            }

            var dpi = NativeMethods.GetDpiForWindow(taskbar);
            return (
                new PixelRect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                trayLeft,
                dpi == 0 ? 96 : dpi,
                taskbar);
        }

        var monitor = NativeMethods.MonitorFromPoint(default, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { CbSize = (uint)sizeof(MONITORINFO) };
        NativeMethods.GetMonitorInfoW(monitor, ref info);
        var ownDpi = NativeMethods.GetDpiForWindow(_hwnd);
        var height = BarLayoutEngine.BarHeight((ownDpi == 0 ? 96 : ownDpi) / 96.0);
        var bottom = info.RcMonitor.Bottom;
        return (
            new PixelRect(info.RcMonitor.Left, bottom - height, info.RcMonitor.Right - info.RcMonitor.Left, height),
            info.RcMonitor.Right,
            ownDpi == 0 ? 96 : ownDpi,
            0);
    }

    /// <summary>全画面のアプリやプレゼン中は、帯が上に被さらないよう隠す。</summary>
    private static bool IsFullScreenAppRunning()
    {
        return NativeMethods.SHQueryUserNotificationState(out var state) == 0
            && state is NativeMethods.QUNS_BUSY or NativeMethods.QUNS_RUNNING_D3D_FULL_SCREEN or NativeMethods.QUNS_PRESENTATION_MODE;
    }

    private void AddTrayIcon()
    {
        var nid = NewTrayData();
        nid.UFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP;
        nid.UCallbackMessage = NativeMethods.WM_TRAY;
        nid.HIcon = GetTrayIcon();
        CopyTip(&nid, "BoltSnap");
        NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_ADD, ref nid);
    }

    /// <summary>exe に埋め込んだアイコンを取り出す。取れなければ Windows 標準のアイコンにする。</summary>
    private nint GetTrayIcon()
    {
        if (_trayIcon == 0 && Environment.ProcessPath is { } path
            && NativeMethods.ExtractIconExW(path, 0, out var large, out var small, 1) > 0)
        {
            if (large != 0)
            {
                NativeMethods.DestroyIcon(large);
            }

            _trayIcon = small;
        }

        return _trayIcon != 0 ? _trayIcon : NativeMethods.LoadIconW(0, NativeMethods.IDI_APPLICATION);
    }

    private void RemoveTrayIcon()
    {
        var nid = NewTrayData();
        NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_DELETE, ref nid);
    }

    private NOTIFYICONDATAW NewTrayData() => new()
    {
        CbSize = (uint)sizeof(NOTIFYICONDATAW),
        Hwnd = _hwnd,
        UId = TrayIconId,
    };

    private static void CopyTip(NOTIFYICONDATAW* nid, string text)
    {
        var length = Math.Min(text.Length, 127);
        for (var i = 0; i < length; i++)
        {
            nid->SzTip[i] = text[i];
        }

        nid->SzTip[length] = '\0';
    }

    private void ShowMenu()
    {
        var menu = NativeMethods.CreatePopupMenu();
        var autoStartFlags = NativeMethods.MF_STRING | (AutoStartService.IsEnabled() ? NativeMethods.MF_CHECKED : 0);
        NativeMethods.AppendMenuW(menu, autoStartFlags, MenuAutoStart, "ログイン時に自動起動");
        NativeMethods.AppendMenuW(menu, NativeMethods.MF_SEPARATOR, 0, null);
        NativeMethods.AppendMenuW(menu, NativeMethods.MF_STRING, MenuExit, "終了");

        NativeMethods.GetCursorPos(out var point);
        NativeMethods.SetForegroundWindow(_hwnd);
        var command = NativeMethods.TrackPopupMenu(
            menu,
            NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_RIGHTBUTTON | NativeMethods.TPM_BOTTOMALIGN,
            point.X, point.Y, 0, _hwnd, 0);
        NativeMethods.PostMessageW(_hwnd, NativeMethods.WM_NULL, 0, 0);
        NativeMethods.DestroyMenu(menu);

        switch ((nuint)command)
        {
            case MenuAutoStart:
                AutoStartService.SetEnabled(!AutoStartService.IsEnabled());
                break;
            case MenuExit:
                Exit();
                break;
        }
    }

    /// <summary>トレイアイコンを外してから終了する。</summary>
    private void Exit()
    {
        NativeMethods.KillTimer(_hwnd, PollTimerId);
        NativeMethods.KillTimer(_hwnd, PlaceTimerId);
        RemoveTrayIcon();
        NativeMethods.DestroyWindow(_hwnd);
    }
}
