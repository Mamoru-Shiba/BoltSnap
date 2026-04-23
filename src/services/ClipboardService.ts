export interface ClipboardService {
  writeImage(blob: Blob): Promise<void>;
}

export const clipboardService: ClipboardService = {
  async writeImage(blob) {
    if (typeof ClipboardItem === 'undefined' || !navigator.clipboard?.write) {
      throw new Error('Clipboard API is not available');
    }
    const item = new ClipboardItem({ [blob.type]: blob });
    await navigator.clipboard.write([item]);
  },
};
