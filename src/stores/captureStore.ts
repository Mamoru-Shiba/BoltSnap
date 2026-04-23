import type { CaptureMode, CaptureProgress } from '@/types/capture';
import { create } from 'zustand';

interface CaptureState {
  mode: CaptureMode;
  copyToClipboard: boolean;
  progress: CaptureProgress;
  setMode: (mode: CaptureMode) => void;
  setCopyToClipboard: (value: boolean) => void;
  setProgress: (progress: CaptureProgress) => void;
  reset: () => void;
}

const initialState = {
  mode: 'fullPage' as const,
  copyToClipboard: false,
  progress: { phase: 'idle' } as CaptureProgress,
};

export const useCaptureStore = create<CaptureState>((set) => ({
  ...initialState,
  setMode: (mode): void => set({ mode }),
  setCopyToClipboard: (value): void => set({ copyToClipboard: value }),
  setProgress: (progress): void => set({ progress }),
  reset: (): void => set(initialState),
}));
