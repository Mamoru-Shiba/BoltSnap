import { describe, expect, it } from 'vitest';
import { formatFilename } from '../formatFilename';

describe('formatFilename', () => {
  it('デフォルトのプレフィックス・拡張子で生成する', () => {
    // Given: 2026-04-23 15:30:45 を表す Date
    const date = new Date(2026, 3, 23, 15, 30, 45);
    // When
    const name = formatFilename(date);
    // Then
    expect(name).toBe('boltsnap-2026-04-23_15-30-45.png');
  });

  it('1 桁の月/日/時/分/秒をゼロ埋めする', () => {
    const date = new Date(2026, 0, 5, 3, 4, 5);
    expect(formatFilename(date)).toBe('boltsnap-2026-01-05_03-04-05.png');
  });

  it('プレフィックスと拡張子を指定できる', () => {
    const date = new Date(2026, 3, 23, 15, 30, 45);
    expect(formatFilename(date, 'shot', 'jpg')).toBe('shot-2026-04-23_15-30-45.jpg');
  });
});
