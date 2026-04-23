import { describe, expect, it } from 'vitest';
import { computeDrawOps, cropRect } from '../ImageStitcher';

describe('computeDrawOps', () => {
  it('CaptureStep から DPR スケール後の描画オペレーションに変換する', () => {
    // Given: 2 タイル、dpr=2
    const steps = [
      { scrollY: 0, destY: 0, sourceY: 0, height: 400 },
      { scrollY: 400, destY: 400, sourceY: 0, height: 400 },
    ];
    const viewportWidth = 300;
    const dpr = 2;
    // When
    const ops = computeDrawOps(steps, { viewportWidth, devicePixelRatio: dpr });
    // Then: 物理 px 単位の sx/sy/sw/sh/dx/dy/dw/dh
    expect(ops).toEqual([
      { tileIndex: 0, sx: 0, sy: 0, sw: 600, sh: 800, dx: 0, dy: 0, dw: 600, dh: 800 },
      { tileIndex: 1, sx: 0, sy: 0, sw: 600, sh: 800, dx: 0, dy: 800, dw: 600, dh: 800 },
    ]);
  });

  it('overlap クロップがある最終タイルで sy がゼロ以外になる', () => {
    const steps = [
      { scrollY: 0, destY: 0, sourceY: 0, height: 400 },
      { scrollY: 200, destY: 400, sourceY: 200, height: 200 },
    ];
    const ops = computeDrawOps(steps, { viewportWidth: 100, devicePixelRatio: 1 });
    expect(ops[1]).toEqual({
      tileIndex: 1,
      sx: 0,
      sy: 200,
      sw: 100,
      sh: 200,
      dx: 0,
      dy: 400,
      dw: 100,
      dh: 200,
    });
  });
});

describe('cropRect', () => {
  it('PNG データ URL を Image にロードし矩形をクロップする座標系を返す', () => {
    // Given
    const rect = { x: 10, y: 20, width: 100, height: 50 };
    const dpr = 2;
    // When
    const op = cropRect(rect, dpr);
    // Then: 物理 px へスケール
    expect(op).toEqual({ sx: 20, sy: 40, sw: 200, sh: 100, dx: 0, dy: 0, dw: 200, dh: 100 });
  });
});
