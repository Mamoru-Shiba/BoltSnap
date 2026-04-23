import type { CaptureMode, Rect } from './capture';

export type RuntimeMessage =
  | { type: 'CAPTURE_REQUEST'; mode: CaptureMode; copyToClipboard: boolean }
  | { type: 'CAPTURE_VISIBLE_TAB' }
  | { type: 'REGION_SELECTED'; rect: Rect; devicePixelRatio: number }
  | { type: 'REGION_CANCELLED' }
  | { type: 'CAPTURE_COMPLETE'; filename: string }
  | { type: 'CAPTURE_ERROR'; message: string };

export interface CaptureVisibleTabResponse {
  dataUrl: string;
}
