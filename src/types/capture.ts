export type CaptureMode = 'fullPage' | 'visible' | 'region';

export interface CaptureOptions {
  mode: CaptureMode;
  copyToClipboard: boolean;
}

export interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface CaptureTile {
  dataUrl: string;
  /** Tile の配置先（結合用キャンバス上の座標、CSS px 単位） */
  offsetX: number;
  offsetY: number;
  /** 元ビューポートの CSS px サイズ */
  width: number;
  height: number;
}

export interface CaptureResult {
  /** 合成済み PNG の Blob */
  blob: Blob;
  /** 合成済みキャンバスの物理 px サイズ */
  width: number;
  height: number;
}

export type CaptureProgress =
  | { phase: 'idle' }
  | { phase: 'preparing' }
  | { phase: 'capturing'; current: number; total: number }
  | { phase: 'stitching' }
  | { phase: 'saving' }
  | { phase: 'done' }
  | { phase: 'error'; message: string };
