import { describe, expect, it } from 'vitest';
import { planScrollCapture } from '../CaptureEngine';

describe('planScrollCapture', () => {
  it('pageHeight=0 のときは空配列を返す', () => {
    // Given
    const input = { pageHeight: 0, viewportHeight: 400 };
    // When
    const steps = planScrollCapture(input);
    // Then
    expect(steps).toEqual([]);
  });

  it('ページがビューポート以下なら単一ステップ（ページ高さで切り詰め）', () => {
    // Given: ページ=300、ビューポート=400
    const input = { pageHeight: 300, viewportHeight: 400 };
    // When
    const steps = planScrollCapture(input);
    // Then: 1 枚、高さ 300 でクロップ
    expect(steps).toEqual([{ scrollY: 0, destY: 0, sourceY: 0, height: 300 }]);
  });

  it('ページがビューポートの倍数ならフル高さのタイルが並ぶ', () => {
    // Given: ページ=800、ビューポート=400
    const steps = planScrollCapture({ pageHeight: 800, viewportHeight: 400 });
    // Then
    expect(steps).toEqual([
      { scrollY: 0, destY: 0, sourceY: 0, height: 400 },
      { scrollY: 400, destY: 400, sourceY: 0, height: 400 },
    ]);
  });

  it('最終タイルがビューポート内に収まる端数の場合は overlap をクロップする', () => {
    // Given: ページ=1000、ビューポート=400
    // Then: 3 枚、最後は scrollY=600 (max) + sourceY=200 で 200px のみ描画
    const steps = planScrollCapture({ pageHeight: 1000, viewportHeight: 400 });
    expect(steps).toEqual([
      { scrollY: 0, destY: 0, sourceY: 0, height: 400 },
      { scrollY: 400, destY: 400, sourceY: 0, height: 400 },
      { scrollY: 600, destY: 800, sourceY: 200, height: 200 },
    ]);
  });

  it('全ステップの height の合計が pageHeight と一致する', () => {
    // 不変条件：合成結果はページ全体をカバーする
    const cases = [
      { pageHeight: 1000, viewportHeight: 400 },
      { pageHeight: 1234, viewportHeight: 500 },
      { pageHeight: 400, viewportHeight: 400 },
      { pageHeight: 401, viewportHeight: 400 },
    ];
    for (const input of cases) {
      const steps = planScrollCapture(input);
      const total = steps.reduce((acc, s) => acc + s.height, 0);
      expect(total).toBe(input.pageHeight);
    }
  });

  it('全ステップの destY が連続し、重複しない', () => {
    const steps = planScrollCapture({ pageHeight: 1234, viewportHeight: 500 });
    let cursor = 0;
    for (const s of steps) {
      expect(s.destY).toBe(cursor);
      cursor += s.height;
    }
  });

  it('viewportHeight=0 や負の入力でも例外を投げずに空を返す', () => {
    expect(planScrollCapture({ pageHeight: 500, viewportHeight: 0 })).toEqual([]);
    expect(planScrollCapture({ pageHeight: -100, viewportHeight: 400 })).toEqual([]);
  });
});
