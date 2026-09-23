using System.Runtime.InteropServices;
using BoltSnap.Native;

namespace BoltSnap.Services;

/// <summary>タスクバーの、アイコンの右端から次の部品（ウィジェット・トレイ）の左端までの空き。</summary>
internal readonly record struct FreeRegion(int Left, int Right);

internal enum ProbeState
{
    Pending,
    Done,
    Failed,
}

/// <summary>
/// UI Automation でタスクバーのボタンの位置を読み、アイコンの後ろの空きを求める。
/// .NET の UI Automation クラスは AOT で使えないため、COM を直接呼ぶ。
/// 旧来のウィンドウ（MSTaskSwWClass など）の位置は実際とずれるので使わない。
///
/// UI Automation の DLL は常駐するアプリの中には読み込まず、測定のたびに短時間だけ動く
/// 別プロセス（自分自身を --probe で起動）に任せる。結果は終了コード（左端 16 ビット + 右端 16 ビット）で返る。
/// </summary>
internal static unsafe class TaskbarProbeService
{
    private const string TaskListButtonClass = "Taskbar.TaskListButtonAutomationPeer";
    private const string ToggleButtonClass = "ToggleButton";
    private const int TreeScopeDescendants = 4;
    private const uint ClsctxInprocServer = 1;
    private const uint CoinitApartmentThreaded = 2;

    private static Guid s_clsidAutomation = new("ff48dba4-60ef-4201-aa87-54103eef594e");
    private static Guid s_iidAutomation = new("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee");

    private static nint s_automation;

    public const string ProbeArgument = "--probe";

    private static nint s_processHandle;

    /// <summary>測定用の別プロセスとして起動されたときの処理。終了コードで結果を返す。</summary>
    public static int RunProbe(string taskbarHandle, string trayLeft)
    {
        CoInitializeEx(0, CoinitApartmentThreaded);
        if (!nint.TryParse(taskbarHandle, out var taskbar) || !int.TryParse(trayLeft, out var tray)
            || !Measure(taskbar, tray, out var region)
            || region.Left is < 0 or > 0xFFFF || region.Right is < 0 or > 0xFFFF)
        {
            return 0;
        }

        return unchecked((int)(((uint)region.Left << 16) | (uint)region.Right));
    }

    /// <summary>測定用の別プロセスを起動する。前回のものが動いている間は何もしない。</summary>
    public static void BeginProbe(nint taskbar, int trayLeft)
    {
        if (s_processHandle != 0 || Environment.ProcessPath is not { } exe)
        {
            return;
        }

        var commandLine = Marshal.StringToHGlobalUni($"\"{exe}\" {ProbeArgument} {taskbar} {trayLeft}");
        try
        {
            var startup = new STARTUPINFOW { Cb = (uint)sizeof(STARTUPINFOW) };
            if (NativeMethods.CreateProcessW(
                    null, commandLine, 0, 0, 0, NativeMethods.CREATE_NO_WINDOW, 0, null, ref startup, out var info) != 0)
            {
                NativeMethods.CloseHandle(info.Thread);
                s_processHandle = info.Process;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(commandLine);
        }
    }

    /// <summary>
    /// 測定の結果を受け取る。まだ動いているときは Pending、失敗は Failed。
    /// 進行中の測定が無いときも Pending を返す（前回の値をそのまま使う）。
    /// </summary>
    public static ProbeState Collect(out FreeRegion region)
    {
        region = default;
        if (s_processHandle == 0)
        {
            return ProbeState.Pending;
        }

        if (NativeMethods.WaitForSingleObject(s_processHandle, 0) != NativeMethods.WAIT_OBJECT_0)
        {
            return ProbeState.Pending;
        }

        NativeMethods.GetExitCodeProcess(s_processHandle, out var code);
        NativeMethods.CloseHandle(s_processHandle);
        s_processHandle = 0;
        if (code == 0)
        {
            return ProbeState.Failed;
        }

        region = new FreeRegion((int)(code >> 16), (int)(code & 0xFFFF));
        return ProbeState.Done;
    }

    /// <summary>起動時などに、測定の完了を最大 timeoutMs だけ待つ。</summary>
    public static ProbeState WaitForResult(int timeoutMs, out FreeRegion region)
    {
        if (s_processHandle != 0)
        {
            NativeMethods.WaitForSingleObject(s_processHandle, (uint)timeoutMs);
        }

        return Collect(out region);
    }

    /// <summary>
    /// 最後のアイコンの右端と、その右にある最初の部品の左端を返す。
    /// 取得できない（古いタスクバーなど）ときは false。
    /// </summary>
    private static bool Measure(nint taskbar, int trayLeft, out FreeRegion region)
    {
        region = default;
        nint root = 0;
        nint condition = 0;
        nint array = 0;
        try
        {
            if (s_automation == 0
                && (CoCreateInstance(ref s_clsidAutomation, 0, ClsctxInprocServer, ref s_iidAutomation, out s_automation) < 0
                    || s_automation == 0))
            {
                s_automation = 0;
                return false;
            }

            var automation = s_automation;

            // IUIAutomation: 6 = ElementFromHandle, 21 = CreateTrueCondition
            if (((delegate* unmanaged<nint, nint, nint*, int>)Vtable(automation)[6])(automation, taskbar, &root) < 0
                || root == 0
                || ((delegate* unmanaged<nint, nint*, int>)Vtable(automation)[21])(automation, &condition) < 0
                || condition == 0)
            {
                return false;
            }

            // IUIAutomationElement: 6 = FindAll
            if (((delegate* unmanaged<nint, int, nint, nint*, int>)Vtable(root)[6])(
                    root, TreeScopeDescendants, condition, &array) < 0
                || array == 0)
            {
                return false;
            }

            return MeasureButtons(array, trayLeft, out region);
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            Release(array);
            Release(condition);
            Release(root);
        }
    }

    private static bool MeasureButtons(nint array, int trayLeft, out FreeRegion region)
    {
        region = default;
        var length = 0;
        // IUIAutomationElementArray: 3 = get_Length, 4 = GetElement
        if (((delegate* unmanaged<nint, int*, int>)Vtable(array)[3])(array, &length) < 0)
        {
            return false;
        }

        var iconsRight = int.MinValue;
        var toggleLefts = new List<int>();
        for (var i = 0; i < length; i++)
        {
            nint element = 0;
            if (((delegate* unmanaged<nint, int, nint*, int>)Vtable(array)[4])(array, i, &element) < 0 || element == 0)
            {
                continue;
            }

            try
            {
                var className = ReadClassName(element);
                if (className is not (TaskListButtonClass or ToggleButtonClass))
                {
                    continue;
                }

                RECT rect;
                // IUIAutomationElement: 43 = get_CurrentBoundingRectangle
                if (((delegate* unmanaged<nint, RECT*, int>)Vtable(element)[43])(element, &rect) < 0
                    || rect.Right <= rect.Left)
                {
                    continue;
                }

                if (className == TaskListButtonClass)
                {
                    iconsRight = Math.Max(iconsRight, rect.Right);
                }
                else
                {
                    toggleLefts.Add(rect.Left);
                }
            }
            finally
            {
                Release(element);
            }
        }

        if (iconsRight == int.MinValue)
        {
            return false;
        }

        // アイコンより右にあるボタン（天気ウィジェットなど）と通知領域のうち、最も左のものまでが空き
        var rightLimit = trayLeft;
        foreach (var left in toggleLefts)
        {
            if (left >= iconsRight && left < rightLimit)
            {
                rightLimit = left;
            }
        }

        region = new FreeRegion(iconsRight, rightLimit);
        return true;
    }

    private static string? ReadClassName(nint element)
    {
        nint bstr = 0;
        // IUIAutomationElement: 30 = get_CurrentClassName
        if (((delegate* unmanaged<nint, nint*, int>)Vtable(element)[30])(element, &bstr) < 0 || bstr == 0)
        {
            return null;
        }

        try
        {
            return Marshal.PtrToStringBSTR(bstr);
        }
        finally
        {
            Marshal.FreeBSTR(bstr);
        }
    }

    private static void** Vtable(nint instance) => *(void***)instance;

    private static void Release(nint instance)
    {
        if (instance != 0)
        {
            ((delegate* unmanaged<nint, uint>)Vtable(instance)[2])(instance);
        }
    }

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(nint reserved, uint coInit);

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid clsid, nint outer, uint context, ref Guid iid, out nint instance);
}
