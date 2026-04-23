export interface DownloadService {
  /**
   * @param dataUrl `data:image/png;base64,...` 形式の URL。
   *                MV3 service worker では URL.createObjectURL が使えないため data URL を使う。
   */
  download(dataUrl: string, filename: string): Promise<number>;
}

export const downloadService: DownloadService = {
  async download(dataUrl, filename) {
    return chrome.downloads.download({ url: dataUrl, filename, saveAs: false });
  },
};
