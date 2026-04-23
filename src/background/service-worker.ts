import { planScrollCapture } from '@/engines/CaptureEngine';
import { cropDataUrl, stitchDataUrls } from '@/engines/ImageStitcher';
import { downloadService } from '@/services/DownloadService';
import type { CaptureMode, Rect } from '@/types/capture';
import type { RuntimeMessage } from '@/types/messaging';
import { formatFilename } from '@/utils/formatFilename';
import { getErrorMessage } from '@/utils/getErrorMessage';

interface PageMetrics {
  pageHeight: number;
  pageWidth: number;
  viewportHeight: number;
  viewportWidth: number;
  devicePixelRatio: number;
}

async function sendToTab<T, R>(tabId: number, message: T): Promise<R> {
  return chrome.tabs.sendMessage<T, R>(tabId, message);
}

async function captureVisibleTab(windowId: number): Promise<string> {
  // chrome.tabs.captureVisibleTab rate-limits to ~2/sec; caller should throttle.
  return chrome.tabs.captureVisibleTab(windowId, { format: 'png' });
}

async function throttle(ms: number): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, ms));
}

async function captureVisible(tab: chrome.tabs.Tab): Promise<Blob> {
  if (tab.windowId === undefined) throw new Error('Tab has no windowId');
  const dataUrl = await captureVisibleTab(tab.windowId);
  const res = await fetch(dataUrl);
  return res.blob();
}

async function captureFullPage(tab: chrome.tabs.Tab): Promise<Blob> {
  if (tab.id === undefined || tab.windowId === undefined) {
    throw new Error('Tab missing id/windowId');
  }
  const metrics = await sendToTab<{ type: string }, PageMetrics>(tab.id, { type: 'FP_MEASURE' });
  const steps = planScrollCapture({
    pageHeight: metrics.pageHeight,
    viewportHeight: metrics.viewportHeight,
  });
  const tiles: string[] = [];
  try {
    for (const [i, step] of steps.entries()) {
      await sendToTab(tab.id, { type: 'FP_SCROLL_TO', y: step.scrollY });
      // captureVisibleTab rate limit: ~2 calls/sec. Throttle after the first.
      if (i > 0) await throttle(550);
      const dataUrl = await captureVisibleTab(tab.windowId);
      tiles.push(dataUrl);
    }
  } finally {
    await sendToTab(tab.id, { type: 'FP_RESTORE' }).catch(() => undefined);
  }
  return stitchDataUrls(
    tiles,
    steps,
    { viewportWidth: metrics.viewportWidth, devicePixelRatio: metrics.devicePixelRatio },
    metrics.pageHeight,
  );
}

const pendingRegion = new Map<
  number,
  { resolve: (rect: Rect & { devicePixelRatio: number }) => void; reject: (e: Error) => void }
>();

async function captureRegion(tab: chrome.tabs.Tab): Promise<Blob> {
  if (tab.id === undefined || tab.windowId === undefined) {
    throw new Error('Tab missing id/windowId');
  }
  const tabId = tab.id;
  const regionPromise = new Promise<Rect & { devicePixelRatio: number }>((resolve, reject) => {
    pendingRegion.set(tabId, { resolve, reject });
  });
  await sendToTab(tabId, { type: 'RS_START' });
  const { x, y, width, height, devicePixelRatio } = await regionPromise;
  const dataUrl = await captureVisibleTab(tab.windowId);
  return cropDataUrl(dataUrl, { x, y, width, height }, devicePixelRatio);
}

async function runCapture(mode: CaptureMode): Promise<Blob> {
  const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
  if (!tab) throw new Error('アクティブなタブが見つかりません');
  if (tab.url?.startsWith('chrome://') || tab.url?.startsWith('edge://')) {
    throw new Error('このページはキャプチャできません');
  }
  if (mode === 'visible') return captureVisible(tab);
  if (mode === 'fullPage') return captureFullPage(tab);
  return captureRegion(tab);
}

async function blobToDataUrl(blob: Blob): Promise<string> {
  const buffer = await blob.arrayBuffer();
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (let i = 0; i < bytes.byteLength; i += 1) {
    binary += String.fromCharCode(bytes[i] ?? 0);
  }
  const base64 = btoa(binary);
  return `data:${blob.type};base64,${base64}`;
}

async function handleCaptureRequest(
  mode: CaptureMode,
  copyToClipboard: boolean,
): Promise<{ filename: string; dataUrl?: string }> {
  const blob = await runCapture(mode);
  const filename = formatFilename(new Date());
  await downloadService.download(blob, filename);
  let dataUrl: string | undefined;
  if (copyToClipboard) {
    // Clipboard write is performed by the popup (service worker lacks clipboard access).
    dataUrl = await blobToDataUrl(blob);
  }
  return dataUrl === undefined ? { filename } : { filename, dataUrl };
}

chrome.runtime.onMessage.addListener((message: RuntimeMessage, sender, sendResponse) => {
  if (message.type === 'CAPTURE_REQUEST') {
    handleCaptureRequest(message.mode, message.copyToClipboard)
      .then((result) => sendResponse({ ok: true, ...result }))
      .catch((err: unknown) => sendResponse({ ok: false, message: getErrorMessage(err) }));
    return true;
  }
  if (message.type === 'REGION_SELECTED') {
    const tabId = sender.tab?.id;
    if (tabId !== undefined) {
      const pending = pendingRegion.get(tabId);
      if (pending) {
        pendingRegion.delete(tabId);
        pending.resolve({ ...message.rect, devicePixelRatio: message.devicePixelRatio });
      }
    }
    return false;
  }
  if (message.type === 'REGION_CANCELLED') {
    const tabId = sender.tab?.id;
    if (tabId !== undefined) {
      const pending = pendingRegion.get(tabId);
      if (pending) {
        pendingRegion.delete(tabId);
        pending.reject(new Error('範囲選択がキャンセルされました'));
      }
    }
    return false;
  }
  return false;
});
