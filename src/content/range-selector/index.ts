/**
 * Range selector content script.
 * Shows a shadow-DOM overlay, lets user drag to select a rectangle,
 * and posts it back via chrome.runtime message.
 */

import type { RuntimeMessage } from '@/types/messaging';

interface StartMessage {
  type: 'RS_START';
}

let host: HTMLDivElement | null = null;

function teardown(): void {
  host?.remove();
  host = null;
}

function createOverlay(): {
  host: HTMLDivElement;
  root: ShadowRoot;
  selection: HTMLDivElement;
} {
  const container = document.createElement('div');
  container.style.cssText =
    'all:initial;position:fixed;inset:0;z-index:2147483647;cursor:crosshair;';
  const root = container.attachShadow({ mode: 'open' });
  const style = document.createElement('style');
  style.textContent = `
    :host, .mask { position: fixed; inset: 0; }
    .mask { background: rgba(0,0,0,0.35); }
    .selection {
      position: fixed;
      border: 2px solid #4A90D9;
      background: rgba(74,144,217,0.15);
      pointer-events: none;
      display: none;
    }
    .hint {
      position: fixed; top: 16px; left: 50%; transform: translateX(-50%);
      background: #222; color: #fff; padding: 6px 12px; border-radius: 4px;
      font-family: system-ui, sans-serif; font-size: 13px;
      pointer-events: none;
    }
  `;
  const mask = document.createElement('div');
  mask.className = 'mask';
  const selection = document.createElement('div');
  selection.className = 'selection';
  const hint = document.createElement('div');
  hint.className = 'hint';
  hint.textContent = 'ドラッグで範囲選択 / Esc でキャンセル';

  root.append(style, mask, selection, hint);
  document.documentElement.append(container);

  return { host: container, root, selection };
}

function startSelection(): void {
  if (host) teardown();
  const created = createOverlay();
  host = created.host;
  const { selection } = created;

  let startX = 0;
  let startY = 0;
  let dragging = false;

  const onDown = (e: MouseEvent): void => {
    dragging = true;
    startX = e.clientX;
    startY = e.clientY;
    selection.style.display = 'block';
    selection.style.left = `${startX}px`;
    selection.style.top = `${startY}px`;
    selection.style.width = '0px';
    selection.style.height = '0px';
  };

  const onMove = (e: MouseEvent): void => {
    if (!dragging) return;
    const x = Math.min(e.clientX, startX);
    const y = Math.min(e.clientY, startY);
    const w = Math.abs(e.clientX - startX);
    const h = Math.abs(e.clientY - startY);
    selection.style.left = `${x}px`;
    selection.style.top = `${y}px`;
    selection.style.width = `${w}px`;
    selection.style.height = `${h}px`;
  };

  const finish = (e: MouseEvent): void => {
    if (!dragging) return;
    dragging = false;
    const x = Math.min(e.clientX, startX);
    const y = Math.min(e.clientY, startY);
    const w = Math.abs(e.clientX - startX);
    const h = Math.abs(e.clientY - startY);
    cleanup();
    if (w < 2 || h < 2) {
      sendCancel();
      return;
    }
    const msg: RuntimeMessage = {
      type: 'REGION_SELECTED',
      rect: { x, y, width: w, height: h },
      devicePixelRatio: window.devicePixelRatio,
    };
    chrome.runtime.sendMessage(msg);
  };

  const onKey = (e: KeyboardEvent): void => {
    if (e.key === 'Escape') {
      cleanup();
      sendCancel();
    }
  };

  const sendCancel = (): void => {
    const msg: RuntimeMessage = { type: 'REGION_CANCELLED' };
    chrome.runtime.sendMessage(msg);
  };

  const cleanup = (): void => {
    host?.removeEventListener('mousedown', onDown);
    window.removeEventListener('mousemove', onMove);
    window.removeEventListener('mouseup', finish);
    window.removeEventListener('keydown', onKey);
    teardown();
  };

  host.addEventListener('mousedown', onDown);
  window.addEventListener('mousemove', onMove);
  window.addEventListener('mouseup', finish);
  window.addEventListener('keydown', onKey);
}

chrome.runtime.onMessage.addListener(
  (message: StartMessage, _sender, sendResponse: (response?: unknown) => void) => {
    if (message.type === 'RS_START') {
      startSelection();
      sendResponse({ ok: true });
    }
    return false;
  },
);
