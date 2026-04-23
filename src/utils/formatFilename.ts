/**
 * boltsnap-2026-04-23_15-30-45.png のような安全なファイル名を生成する。
 * タイムゾーンはローカル時刻を使用する。
 */
export function formatFilename(date: Date, prefix = 'boltsnap', extension = 'png'): string {
  const pad = (n: number): string => String(n).padStart(2, '0');
  const y = date.getFullYear();
  const mo = pad(date.getMonth() + 1);
  const d = pad(date.getDate());
  const h = pad(date.getHours());
  const mi = pad(date.getMinutes());
  const s = pad(date.getSeconds());
  return `${prefix}-${y}-${mo}-${d}_${h}-${mi}-${s}.${extension}`;
}
