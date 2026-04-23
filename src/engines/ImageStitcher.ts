import type { Rect } from '@/types/capture';
import type { CaptureStep } from './CaptureEngine';

export interface DrawOp {
  tileIndex: number;
  sx: number;
  sy: number;
  sw: number;
  sh: number;
  dx: number;
  dy: number;
  dw: number;
  dh: number;
}

export interface DrawContext {
  viewportWidth: number;
  devicePixelRatio: number;
}

/** CaptureStep 配列から物理 px ベースの描画オペレーションを生成する（純粋関数） */
export function computeDrawOps(steps: CaptureStep[], ctx: DrawContext): DrawOp[] {
  const dpr = ctx.devicePixelRatio;
  return steps.map((step, index) => ({
    tileIndex: index,
    sx: 0,
    sy: Math.round(step.sourceY * dpr),
    sw: Math.round(ctx.viewportWidth * dpr),
    sh: Math.round(step.height * dpr),
    dx: 0,
    dy: Math.round(step.destY * dpr),
    dw: Math.round(ctx.viewportWidth * dpr),
    dh: Math.round(step.height * dpr),
  }));
}

/** 単一矩形クロップ用の描画オペレーションを返す（純粋関数） */
export function cropRect(rect: Rect, devicePixelRatio: number): Omit<DrawOp, 'tileIndex'> {
  const dpr = devicePixelRatio;
  return {
    sx: Math.round(rect.x * dpr),
    sy: Math.round(rect.y * dpr),
    sw: Math.round(rect.width * dpr),
    sh: Math.round(rect.height * dpr),
    dx: 0,
    dy: 0,
    dw: Math.round(rect.width * dpr),
    dh: Math.round(rect.height * dpr),
  };
}

/** DataURL を Blob に変換する（Service Worker / Document の両方で動作） */
export async function dataUrlToBlob(dataUrl: string): Promise<Blob> {
  const response = await fetch(dataUrl);
  return response.blob();
}

/**
 * 複数タイル DataURL を結合し PNG Blob を返す（OffscreenCanvas ベース）。
 * Service Worker / Document の両方で利用可能。
 */
export async function stitchDataUrls(
  tileDataUrls: string[],
  steps: CaptureStep[],
  ctx: DrawContext,
  totalHeight: number,
): Promise<Blob> {
  if (tileDataUrls.length !== steps.length) {
    throw new Error('tileDataUrls and steps length mismatch');
  }
  const dpr = ctx.devicePixelRatio;
  const width = Math.round(ctx.viewportWidth * dpr);
  const height = Math.round(totalHeight * dpr);
  const canvas = new OffscreenCanvas(width, height);
  const ctx2d = canvas.getContext('2d');
  if (!ctx2d) throw new Error('Cannot get 2d context');

  const ops = computeDrawOps(steps, ctx);
  const blobs = await Promise.all(tileDataUrls.map(dataUrlToBlob));
  const bitmaps = await Promise.all(blobs.map((b) => createImageBitmap(b)));
  try {
    for (const op of ops) {
      const bitmap = bitmaps[op.tileIndex];
      if (!bitmap) continue;
      ctx2d.drawImage(bitmap, op.sx, op.sy, op.sw, op.sh, op.dx, op.dy, op.dw, op.dh);
    }
    return canvas.convertToBlob({ type: 'image/png' });
  } finally {
    for (const b of bitmaps) b.close?.();
  }
}

/** 単一画像から矩形をクロップして PNG Blob を返す */
export async function cropDataUrl(
  dataUrl: string,
  rect: Rect,
  devicePixelRatio: number,
): Promise<Blob> {
  const blob = await dataUrlToBlob(dataUrl);
  const bitmap = await createImageBitmap(blob);
  const op = cropRect(rect, devicePixelRatio);
  const canvas = new OffscreenCanvas(op.dw, op.dh);
  const ctx2d = canvas.getContext('2d');
  if (!ctx2d) {
    bitmap.close?.();
    throw new Error('Cannot get 2d context');
  }
  try {
    ctx2d.drawImage(bitmap, op.sx, op.sy, op.sw, op.sh, op.dx, op.dy, op.dw, op.dh);
    return canvas.convertToBlob({ type: 'image/png' });
  } finally {
    bitmap.close?.();
  }
}
