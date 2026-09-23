using System.Numerics;

namespace BoltSnap.Core.Engines;

/// <summary>1 日 1440 分それぞれの稼働有無を持つビットマップ（180 バイト）。</summary>
public sealed class MinuteBitmap
{
    public const int MinutesPerDay = 1440;
    public const int ByteLength = MinutesPerDay / 8;

    private readonly byte[] _bits;

    public MinuteBitmap()
    {
        _bits = new byte[ByteLength];
    }

    public MinuteBitmap(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != ByteLength)
        {
            throw new ArgumentException($"長さは {ByteLength} バイトである必要があります。", nameof(bytes));
        }

        _bits = bytes.ToArray();
    }

    public int Count
    {
        get
        {
            var total = 0;
            foreach (var b in _bits)
            {
                total += BitOperations.PopCount(b);
            }

            return total;
        }
    }

    public bool IsSet(int minute)
    {
        ValidateMinute(minute);
        return (_bits[minute >> 3] & (1 << (minute & 7))) != 0;
    }

    /// <summary>指定の分を稼働にする。値が変わったときだけ true を返す。</summary>
    public bool Set(int minute)
    {
        ValidateMinute(minute);
        var mask = (byte)(1 << (minute & 7));
        if ((_bits[minute >> 3] & mask) != 0)
        {
            return false;
        }

        _bits[minute >> 3] |= mask;
        return true;
    }

    /// <summary>稼働が連続している区間（開始分, 長さ）を昇順で返す。</summary>
    public IReadOnlyList<(int Start, int Length)> Runs()
    {
        var runs = new List<(int Start, int Length)>();
        var start = -1;
        for (var minute = 0; minute < MinutesPerDay; minute++)
        {
            if (IsSet(minute))
            {
                if (start < 0)
                {
                    start = minute;
                }
            }
            else if (start >= 0)
            {
                runs.Add((start, minute - start));
                start = -1;
            }
        }

        if (start >= 0)
        {
            runs.Add((start, MinutesPerDay - start));
        }

        return runs;
    }

    public ReadOnlySpan<byte> AsSpan() => _bits;

    private static void ValidateMinute(int minute)
    {
        if ((uint)minute >= MinutesPerDay)
        {
            throw new ArgumentOutOfRangeException(nameof(minute), minute, "分は 0〜1439 で指定してください。");
        }
    }
}
