using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BoltSnap.Core.Engines;
using BoltSnap.Core.Services;
using BoltSnap.Native;
using BoltSnap.Services;

namespace BoltSnap.Ui;

/// <summary>画面下端に AppBar として固定される帯のウィンドウ。</summary>
internal sealed unsafe class BarWindow
{
    private const string ClassName = "BoltSnapBar";
    private const nuint PollTimerId = 1;
    private const uint PollIntervalMs = 10_000;
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
    private uint _taskbarCreatedMessage;

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

        UpdateScale(NativeMethods.GetDpiForWindow(_hwnd));
        RegisterAppBar();
        Reposition();
        AddTrayIcon();
        Poll();
        RefreshModel(force: true);
        NativeMethods.SetTimer(_hwnd, PollTimerId, PollIntervalMs, 0);

        while (NativeMethods.GetMessageW(out var msg, 0, 0, 0) > 0)
        {
            NativeMethods.TranslateMessage(ref msg);
            NativeMethods.DispatchMessageW(ref msg);
        }

        _renderer?.Dispose();
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
            // エクスプローラーの再起動後に、トレイと AppBar を登録し直す
            AddTrayIcon();
            RegisterAppBar();
            Reposition();
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
            case NativeMethods.WM_TIMER:
                if ((nuint)wParam == PollTimerId)
                {
                    Poll();
                }

                return (true, 0);
            case NativeMethods.WM_TIMECHANGE:
                RefreshModel(force: true);
                return (true, 0);
            case NativeMethods.WM_DISPLAYCHANGE:
                UpdateScale(NativeMethods.GetDpiForWindow(_hwnd));
                Reposition();
                return (true, 0);
            case NativeMethods.WM_DPICHANGED:
                UpdateScale((uint)(wParam & 0xFFFF));
                Reposition();
                return (true, 0);
            case NativeMethods.WM_APPBAR:
                OnAppBarNotify((int)wParam);
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

        _lastMinuteStamp = stamp;
        _model = _modelService.Build(now);
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

            _renderer.Draw(memDc, width, height, _model);
            NativeMethods.BitBlt(hdc, 0, 0, width, height, memDc, 0, 0, NativeMethods.SRCCOPY);

            NativeMethods.SelectObject(memDc, previous);
            NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(memDc);
        }

        NativeMethods.EndPaint(_hwnd, ref ps);
    }

    private void RegisterAppBar()
    {
        var data = NewAppBarData();
        data.UCallbackMessage = NativeMethods.WM_APPBAR;
        NativeMethods.SHAppBarMessage(NativeMethods.ABM_NEW, ref data);
    }

    /// <summary>プライマリ画面の下端に、タスクバーと重ならない位置で領域を予約して配置する。</summary>
    private void Reposition()
    {
        var monitor = NativeMethods.MonitorFromPoint(default, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        var info = new MONITORINFO { CbSize = (uint)sizeof(MONITORINFO) };
        NativeMethods.GetMonitorInfoW(monitor, ref info);

        var height = BarLayoutEngine.BarHeight(_scale);
        var data = NewAppBarData();
        data.Rc = info.RcMonitor;
        NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref data);
        data.Rc.Top = data.Rc.Bottom - height;
        NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref data);

        NativeMethods.SetWindowPos(
            _hwnd, NativeMethods.HWND_TOPMOST,
            data.Rc.Left, data.Rc.Top, data.Rc.Right - data.Rc.Left, height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
        NativeMethods.InvalidateRect(_hwnd, 0, 0);
    }

    private void OnAppBarNotify(int code)
    {
        if (code == NativeMethods.ABN_POSCHANGED)
        {
            Reposition();
        }
    }

    private APPBARDATA NewAppBarData() => new()
    {
        CbSize = (uint)sizeof(APPBARDATA),
        Hwnd = _hwnd,
        UEdge = NativeMethods.ABE_BOTTOM,
    };

    private void AddTrayIcon()
    {
        var nid = NewTrayData();
        nid.UFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP;
        nid.UCallbackMessage = NativeMethods.WM_TRAY;
        nid.HIcon = NativeMethods.LoadIconW(0, NativeMethods.IDI_APPLICATION);
        CopyTip(&nid, "BoltSnap");
        NativeMethods.Shell_NotifyIconW(NativeMethods.NIM_ADD, ref nid);
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

    /// <summary>予約していた画面領域とトレイアイコンを解放してから終了する。</summary>
    private void Exit()
    {
        NativeMethods.KillTimer(_hwnd, PollTimerId);
        var data = NewAppBarData();
        NativeMethods.SHAppBarMessage(NativeMethods.ABM_REMOVE, ref data);
        RemoveTrayIcon();
        NativeMethods.DestroyWindow(_hwnd);
    }
}
