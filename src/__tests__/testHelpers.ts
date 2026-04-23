import type { CaptureProgress } from '@/types/capture';

export const idleProgress: CaptureProgress = { phase: 'idle' };
export const errorProgress = (message: string): CaptureProgress => ({ phase: 'error', message });
