export interface DownloadService {
  download(blob: Blob, filename: string): Promise<number>;
}

export const downloadService: DownloadService = {
  async download(blob, filename) {
    const url = URL.createObjectURL(blob);
    try {
      return await chrome.downloads.download({ url, filename, saveAs: false });
    } finally {
      // Revoke after a tick to ensure the browser has started reading.
      setTimeout(() => URL.revokeObjectURL(url), 10_000);
    }
  },
};
