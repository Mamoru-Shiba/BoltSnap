export interface CaptureStep {
  /** このタイルを撮影するために `window.scrollTo(0, scrollY)` する */
  scrollY: number;
  /** 結合後キャンバスでの縦方向の貼り付け位置（CSS px） */
  destY: number;
  /** 撮影タイル内のどの位置から切り出すか（CSS px、overlap 対策） */
  sourceY: number;
  /** 切り出す高さ（CSS px） */
  height: number;
}

export interface PlanInput {
  pageHeight: number;
  viewportHeight: number;
}

/**
 * フルページキャプチャのスクロール計画を返す。
 * - ページ高さがビューポート以下 → 1 ステップ（ページ高さでクロップ）
 * - 倍数の場合 → フル高さのタイルを並べる
 * - 端数の場合 → 最終タイルは scrollY を最大位置に固定し、sourceY で overlap を除く
 */
export function planScrollCapture({ pageHeight, viewportHeight }: PlanInput): CaptureStep[] {
  if (pageHeight <= 0 || viewportHeight <= 0) return [];

  if (pageHeight <= viewportHeight) {
    return [{ scrollY: 0, destY: 0, sourceY: 0, height: pageHeight }];
  }

  const steps: CaptureStep[] = [];
  let destY = 0;

  while (destY + viewportHeight <= pageHeight) {
    steps.push({ scrollY: destY, destY, sourceY: 0, height: viewportHeight });
    destY += viewportHeight;
  }

  if (destY < pageHeight) {
    const scrollY = pageHeight - viewportHeight;
    const sourceY = destY - scrollY;
    const height = pageHeight - destY;
    steps.push({ scrollY, destY, sourceY, height });
  }

  return steps;
}
