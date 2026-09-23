using System.Runtime.InteropServices;

namespace BoltSnap.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MSG
{
    public nint Hwnd;
    public uint Message;
    public nint WParam;
    public nint LParam;
    public uint Time;
    public POINT Pt;
    public uint LPrivate;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct WNDCLASSEXW
{
    public uint CbSize;
    public uint Style;
    public delegate* unmanaged[Stdcall]<nint, uint, nint, nint, nint> LpfnWndProc;
    public int CbClsExtra;
    public int CbWndExtra;
    public nint HInstance;
    public nint HIcon;
    public nint HCursor;
    public nint HbrBackground;
    public nint LpszMenuName;
    public nint LpszClassName;
    public nint HIconSm;
}

[StructLayout(LayoutKind.Sequential)]
internal struct PAINTSTRUCT
{
    public nint Hdc;
    public int FErase;
    public RECT RcPaint;
    public int FRestore;
    public int FIncUpdate;
    public unsafe fixed byte RgbReserved[32];
}

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    public uint CbSize;
    public RECT RcMonitor;
    public RECT RcWork;
    public uint DwFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TRACKMOUSEEVENT
{
    public uint CbSize;
    public uint DwFlags;
    public nint HwndTrack;
    public uint DwHoverTime;
}

[StructLayout(LayoutKind.Sequential)]
internal struct LASTINPUTINFO
{
    public uint CbSize;
    public uint DwTime;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct NOTIFYICONDATAW
{
    public uint CbSize;
    public nint Hwnd;
    public uint UId;
    public uint UFlags;
    public uint UCallbackMessage;
    public nint HIcon;
    public fixed char SzTip[128];
    public uint DwState;
    public uint DwStateMask;
    public fixed char SzInfo[256];
    public uint UVersion;
    public fixed char SzInfoTitle[64];
    public uint DwInfoFlags;
    public Guid GuidItem;
    public nint HBalloonIcon;
}

internal static class NativeMethods
{
    // ウィンドウメッセージ
    public const uint WM_DESTROY = 0x0002;
    public const uint WM_DPICHANGED = 0x02E0;
    public const uint WM_PAINT = 0x000F;
    public const uint WM_ERASEBKGND = 0x0014;
    public const uint WM_MOUSEACTIVATE = 0x0021;
    public const uint WM_DISPLAYCHANGE = 0x007E;
    public const uint WM_TIMER = 0x0113;
    public const uint WM_TIMECHANGE = 0x001E;
    public const uint WM_MOUSEMOVE = 0x0200;
    public const uint WM_MOUSELEAVE = 0x02A3;
    public const uint WM_RBUTTONUP = 0x0205;
    public const uint TME_LEAVE = 0x00000002;
    public const uint WM_NULL = 0x0000;
    public const uint WM_TRAY = 0x0402;

    public const nint MA_NOACTIVATE = 3;

    // ウィンドウスタイル
    public const uint WS_POPUP = 0x80000000;
    public const uint WS_EX_TOPMOST = 0x00000008;
    public const uint WS_EX_TOOLWINDOW = 0x00000080;
    public const uint WS_EX_NOACTIVATE = 0x08000000;

    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const nint HWND_TOPMOST = -1;
    public const int QUNS_BUSY = 2;
    public const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;
    public const int QUNS_PRESENTATION_MODE = 4;

    public const int SW_HIDE = 0;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int GWLP_HWNDPARENT = -8;

    // トレイ
    public const uint NIM_ADD = 0;
    public const uint NIM_DELETE = 2;
    public const uint NIF_MESSAGE = 1;
    public const uint NIF_ICON = 2;
    public const uint NIF_TIP = 4;

    // メニュー
    public const uint MF_STRING = 0x0000;
    public const uint MF_CHECKED = 0x0008;
    public const uint MF_SEPARATOR = 0x0800;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_RETURNCMD = 0x0100;
    public const uint TPM_BOTTOMALIGN = 0x0020;

    // GDI
    public const int TRANSPARENT = 1;
    public const uint SRCCOPY = 0x00CC0020;
    public const uint DT_LEFT = 0x0000;
    public const uint DT_VCENTER = 0x0004;
    public const uint DT_SINGLELINE = 0x0020;
    public const uint DT_NOPREFIX = 0x0800;
    public const uint DT_END_ELLIPSIS = 0x8000;
    public const int FW_NORMAL = 400;
    public const uint DEFAULT_CHARSET = 1;
    public const uint CLEARTYPE_QUALITY = 5;

    public const int MONITOR_DEFAULTTOPRIMARY = 1;
    public const int IDC_ARROW = 32512;
    public const int IDI_APPLICATION = 32512;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern nint GetModuleHandleW(string? moduleName);

    [DllImport("kernel32.dll")]
    public static extern uint GetTickCount();

    [DllImport("user32.dll")]
    public static extern int GetMessageW(out MSG msg, nint hwnd, uint min, uint max);

    [DllImport("user32.dll")]
    public static extern int TranslateMessage(ref MSG msg);

    [DllImport("user32.dll")]
    public static extern nint DispatchMessageW(ref MSG msg);

    [DllImport("user32.dll")]
    public static extern void PostQuitMessage(int exitCode);

    [DllImport("user32.dll")]
    public static extern nint DefWindowProcW(nint hwnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    public static extern ushort RegisterClassExW(ref WNDCLASSEXW wc);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern nint CreateWindowExW(
        uint exStyle, nint className, string windowName, uint style,
        int x, int y, int width, int height,
        nint parent, nint menu, nint instance, nint param);

    [DllImport("user32.dll")]
    public static extern int DestroyWindow(nint hwnd);

    [DllImport("user32.dll")]
    public static extern int SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern nint FindWindowW(string className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern nint FindWindowExW(nint parent, nint childAfter, string className, string? windowName);

    [DllImport("user32.dll")]
    public static extern int GetWindowRect(nint hwnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern int ShowWindow(nint hwnd, int command);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    public static extern nint SetWindowLongPtrW(nint hwnd, int index, nint value);

    [DllImport("user32.dll")]
    public static extern int GetClientRect(nint hwnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern int InvalidateRect(nint hwnd, nint rect, int erase);

    [DllImport("user32.dll")]
    public static extern nint BeginPaint(nint hwnd, out PAINTSTRUCT ps);

    [DllImport("user32.dll")]
    public static extern int EndPaint(nint hwnd, ref PAINTSTRUCT ps);

    [DllImport("user32.dll")]
    public static extern nuint SetTimer(nint hwnd, nuint id, uint elapseMs, nint proc);

    [DllImport("user32.dll")]
    public static extern int KillTimer(nint hwnd, nuint id);

    [DllImport("user32.dll")]
    public static extern nint LoadCursorW(nint instance, nint cursorName);

    [DllImport("user32.dll")]
    public static extern int TrackMouseEvent(ref TRACKMOUSEEVENT track);

    [DllImport("user32.dll")]
    public static extern nint LoadIconW(nint instance, nint iconName);

    [DllImport("user32.dll")]
    public static extern int DestroyIcon(nint icon);

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(nint hwnd);

    [DllImport("user32.dll")]
    public static extern nint MonitorFromPoint(POINT pt, int flags);

    [DllImport("user32.dll")]
    public static extern int GetMonitorInfoW(nint monitor, ref MONITORINFO info);

    [DllImport("user32.dll")]
    public static extern int GetLastInputInfo(ref LASTINPUTINFO info);

    [DllImport("user32.dll")]
    public static extern int FillRect(nint hdc, ref RECT rect, nint brush);

    [DllImport("user32.dll")]
    public static extern int FrameRect(nint hdc, ref RECT rect, nint brush);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int DrawTextW(nint hdc, string text, int count, ref RECT rect, uint format);

    [DllImport("user32.dll")]
    public static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int AppendMenuW(nint menu, uint flags, nuint id, string? text);

    [DllImport("user32.dll")]
    public static extern int TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint hwnd, nint rect);

    [DllImport("user32.dll")]
    public static extern int DestroyMenu(nint menu);

    [DllImport("user32.dll")]
    public static extern int GetCursorPos(out POINT pt);

    [DllImport("user32.dll")]
    public static extern int SetForegroundWindow(nint hwnd);

    [DllImport("user32.dll")]
    public static extern int PostMessageW(nint hwnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern uint RegisterWindowMessageW(string message);

    [DllImport("gdi32.dll")]
    public static extern nint CreateSolidBrush(uint color);

    [DllImport("gdi32.dll")]
    public static extern nint CreateCompatibleDC(nint hdc);

    [DllImport("gdi32.dll")]
    public static extern nint CreateCompatibleBitmap(nint hdc, int width, int height);

    [DllImport("gdi32.dll")]
    public static extern nint SelectObject(nint hdc, nint obj);

    [DllImport("gdi32.dll")]
    public static extern int DeleteObject(nint obj);

    [DllImport("gdi32.dll")]
    public static extern int DeleteDC(nint hdc);

    [DllImport("gdi32.dll")]
    public static extern int BitBlt(nint dest, int x, int y, int w, int h, nint src, int sx, int sy, uint rop);

    [DllImport("gdi32.dll")]
    public static extern int SetBkMode(nint hdc, int mode);

    [DllImport("gdi32.dll")]
    public static extern uint SetTextColor(nint hdc, uint color);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    public static extern nint CreateFontW(
        int height, int width, int escapement, int orientation, int weight,
        uint italic, uint underline, uint strikeOut, uint charSet,
        uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string faceName);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern uint ExtractIconExW(string file, int index, out nint large, out nint small, uint count);

    [DllImport("shell32.dll")]
    public static extern int SHQueryUserNotificationState(out int state);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int Shell_NotifyIconW(uint message, ref NOTIFYICONDATAW data);
}
