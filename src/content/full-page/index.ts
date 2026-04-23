/**
 * Full-page capture content script.
 * Responds to scroll/measure requests from the service worker.
 */

type FullPageMessage =
  | { type: 'FP_MEASURE' }
  | { type: 'FP_SCROLL_TO'; y: number }
  | { type: 'FP_RESTORE' };

interface MeasureResult {
  pageHeight: number;
  pageWidth: number;
  viewportHeight: number;
  viewportWidth: number;
  devicePixelRatio: number;
}

let savedScrollY = 0;
let savedOverflow = '';

function measurePage(): MeasureResult {
  const doc = document.documentElement;
  const body = document.body;
  const pageHeight = Math.max(
    doc.scrollHeight,
    body?.scrollHeight ?? 0,
    doc.offsetHeight,
    body?.offsetHeight ?? 0,
    doc.clientHeight,
  );
  const pageWidth = Math.max(
    doc.scrollWidth,
    body?.scrollWidth ?? 0,
    doc.offsetWidth,
    body?.offsetWidth ?? 0,
    doc.clientWidth,
  );
  return {
    pageHeight,
    pageWidth,
    viewportHeight: window.innerHeight,
    viewportWidth: window.innerWidth,
    devicePixelRatio: window.devicePixelRatio,
  };
}

function waitForSettle(delayMs = 150): Promise<void> {
  return new Promise((resolve) => {
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        setTimeout(resolve, delayMs);
      });
    });
  });
}

chrome.runtime.onMessage.addListener(
  (message: FullPageMessage, _sender, sendResponse: (response?: unknown) => void) => {
    if (message.type === 'FP_MEASURE') {
      savedScrollY = window.scrollY;
      savedOverflow = document.documentElement.style.overflow;
      document.documentElement.style.overflow = 'hidden';
      sendResponse(measurePage());
      return false;
    }
    if (message.type === 'FP_SCROLL_TO') {
      window.scrollTo({ top: message.y, left: 0, behavior: 'instant' as ScrollBehavior });
      waitForSettle().then(() => sendResponse({ ok: true }));
      return true;
    }
    if (message.type === 'FP_RESTORE') {
      document.documentElement.style.overflow = savedOverflow;
      window.scrollTo({ top: savedScrollY, left: 0, behavior: 'instant' as ScrollBehavior });
      sendResponse({ ok: true });
      return false;
    }
    return false;
  },
);
