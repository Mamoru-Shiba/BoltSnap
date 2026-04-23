import { useCaptureStore } from '@/stores/captureStore';
import { CaptureMenuView } from './CaptureMenuView';
import { useCapture } from './useCapture';

export function CaptureMenu(): JSX.Element {
  const mode = useCaptureStore((s) => s.mode);
  const copyToClipboard = useCaptureStore((s) => s.copyToClipboard);
  const progress = useCaptureStore((s) => s.progress);
  const setMode = useCaptureStore((s) => s.setMode);
  const setCopyToClipboard = useCaptureStore((s) => s.setCopyToClipboard);
  const { capture } = useCapture();

  return (
    <CaptureMenuView
      mode={mode}
      copyToClipboard={copyToClipboard}
      progress={progress}
      onModeChange={setMode}
      onCopyToClipboardChange={setCopyToClipboard}
      onCapture={capture}
    />
  );
}
