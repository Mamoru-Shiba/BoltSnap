using System.Text;
using BoltSnap.Core.Engines;

namespace BoltSnap.Core.Services;

/// <summary>
/// 年ごとに 1 ファイル（{年}.bin）で稼働を保存する。
/// 形式: "BSNP"(4) + 版(2) + 年(2) + 366 日 × 180 バイト。新規の記録は 1 バイトだけ書き足す。
/// </summary>
public sealed class ActivityStoreService : IActivityStore
{
    private const int MaxDays = 366;
    private const int HeaderLength = 8;
    private const short FormatVersion = 1;
    private const int FileLength = HeaderLength + MaxDays * MinuteBitmap.ByteLength;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("BSNP");

    private readonly string _directory;
    private readonly Dictionary<int, byte[]> _years = [];
    private readonly HashSet<int> _needsFullWrite = [];

    public ActivityStoreService(string directory)
    {
        _directory = directory;
    }

    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BoltSnap");

    public bool Record(DateTime moment)
    {
        var data = LoadYear(moment.Year);
        var minute = ActivityEngine.MinuteOfDay(moment);
        var byteOffset = HeaderLength + (moment.DayOfYear - 1) * MinuteBitmap.ByteLength + (minute >> 3);
        var mask = (byte)(1 << (minute & 7));
        if ((data[byteOffset] & mask) != 0)
        {
            return false;
        }

        data[byteOffset] |= mask;
        Persist(moment.Year, data, byteOffset);
        return true;
    }

    public MinuteBitmap GetDay(DateOnly date)
    {
        var data = LoadYear(date.Year);
        var start = HeaderLength + (date.DayOfYear - 1) * MinuteBitmap.ByteLength;
        return new MinuteBitmap(data.AsSpan(start, MinuteBitmap.ByteLength));
    }

    public int[] GetActiveMinutesByDay(int year)
    {
        var data = LoadYear(year);
        var days = DateTime.IsLeapYear(year) ? 366 : 365;
        var result = new int[days];
        for (var day = 0; day < days; day++)
        {
            var start = HeaderLength + day * MinuteBitmap.ByteLength;
            result[day] = MinuteBitmap.CountSetBits(data.AsSpan(start, MinuteBitmap.ByteLength));
        }

        return result;
    }

    private string PathFor(int year) => Path.Combine(_directory, $"{year}.bin");

    private byte[] LoadYear(int year)
    {
        if (_years.TryGetValue(year, out var cached))
        {
            return cached;
        }

        var data = ReadOrCreate(year);
        _years[year] = data;
        return data;
    }

    private byte[] ReadOrCreate(int year)
    {
        var path = PathFor(year);
        if (!File.Exists(path))
        {
            return NewYear(year);
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            if (IsValid(bytes, year))
            {
                return bytes;
            }

            // 壊れたファイルは退避して、空の年から始める
            File.Move(path, path + ".corrupt", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 読めないときは、そのセッションだけ空として扱う（既存ファイルは触らない）
            return NewYear(year);
        }

        return NewYear(year);
    }

    private static bool IsValid(byte[] bytes, int year)
    {
        return bytes.Length == FileLength
            && bytes.AsSpan(0, Magic.Length).SequenceEqual(Magic)
            && BitConverter.ToInt16(bytes, 4) == FormatVersion
            && BitConverter.ToInt16(bytes, 6) == year;
    }

    private static byte[] NewYear(int year)
    {
        var data = new byte[FileLength];
        Magic.CopyTo(data, 0);
        BitConverter.TryWriteBytes(data.AsSpan(4, 2), FormatVersion);
        BitConverter.TryWriteBytes(data.AsSpan(6, 2), (short)year);
        return data;
    }

    private void Persist(int year, byte[] data, int byteOffset)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            var path = PathFor(year);
            if (_needsFullWrite.Contains(year) || !File.Exists(path))
            {
                File.WriteAllBytes(path, data);
                _needsFullWrite.Remove(year);
                return;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
            stream.Seek(byteOffset, SeekOrigin.Begin);
            stream.WriteByte(data[byteOffset]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 書けなかった分はメモリに残し、次回の記録で全体を書き直す
            _needsFullWrite.Add(year);
        }
    }
}
