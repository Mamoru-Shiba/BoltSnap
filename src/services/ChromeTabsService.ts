export interface ChromeTabsService {
  getActiveTab(): Promise<chrome.tabs.Tab>;
  captureVisibleTab(windowId: number): Promise<string>;
  sendMessage<T, R>(tabId: number, message: T): Promise<R>;
}

export const chromeTabsService: ChromeTabsService = {
  async getActiveTab() {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tab) throw new Error('No active tab found');
    return tab;
  },
  async captureVisibleTab(windowId) {
    return chrome.tabs.captureVisibleTab(windowId, { format: 'png' });
  },
  async sendMessage(tabId, message) {
    return chrome.tabs.sendMessage(tabId, message);
  },
};
