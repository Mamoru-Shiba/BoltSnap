import { dataUrlToBlob } from '@/engines/ImageStitcher';
import { clipboardService } from '@/services/ClipboardService';
import { useCaptureStore } from '@/stores/captureStore';
import type { RuntimeMessage } from '@/types/messaging';
import { getErrorMessage } from '@/utils/getErrorMessage';
import { useCallback } from 'react';

interface UseCaptureResult {
  capture: () => Promise<void>;
}

export function useCapture(): UseCaptureResult {
  const mode = useCaptureStore((s) => s.mode);
  const copyToClipboard = useCaptureStore((s) => s.copyToClipboard);
  const setProgress = useCaptureStore((s) => s.setProgress);

  const capture = useCallback(async (): Promise<void> => {
    setProgress({ phase: 'capturing', current: 0, total: 1 });
    try {
      const request: RuntimeMessage = {
        type: 'CAPTURE_REQUEST',
        mode,
        copyToClipboard,
      };
      const response = await chrome.runtime.sendMessage<
        RuntimeMessage,
        { ok: true; filename: string; dataUrl?: string } | { ok: false; message: string }
      >(request);
      if (!response.ok) {
        setProgress({ phase: 'error', message: response.message });
        return;
      }
      if (copyToClipboard && response.dataUrl) {
        const blob = await dataUrlToBlob(response.dataUrl);
        try {
          await clipboardService.writeImage(blob);
        } catch (err) {
          setProgress({
            phase: 'error',
            message: `保存は完了。コピー失敗: ${getErrorMessage(err)}`,
          });
          return;
        }
      }
      setProgress({ phase: 'done' });
    } catch (err) {
      setProgress({ phase: 'error', message: getErrorMessage(err) });
    }
  }, [mode, copyToClipboard, setProgress]);

  return { capture };
}
