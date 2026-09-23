using System.Runtime.InteropServices;
using BoltSnap.Native;

namespace BoltSnap.Services;

/// <summary>最後のキーボード・マウス入力からの経過時間を返す。入力内容は取得しない。</summary>
internal static class InputIdleService
{
    public static TimeSpan GetIdle()
    {
        var info = new LASTINPUTINFO { CbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (NativeMethods.GetLastInputInfo(ref info) == 0)
        {
            // 取得できないときは稼働とみなさない
            return TimeSpan.MaxValue;
        }

        var elapsedMs = unchecked(NativeMethods.GetTickCount() - info.DwTime);
        return TimeSpan.FromMilliseconds(elapsedMs);
    }
}
