import { errorProgress, idleProgress } from '@/__tests__/testHelpers';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { CaptureMenuView } from '../CaptureMenuView';

describe('CaptureMenuView', () => {
  it('デフォルト選択のフルページが強調表示される', () => {
    // Given: mode=fullPage
    render(
      <CaptureMenuView
        mode="fullPage"
        copyToClipboard={false}
        progress={idleProgress}
        onModeChange={vi.fn()}
        onCopyToClipboardChange={vi.fn()}
        onCapture={vi.fn()}
      />,
    );
    // Then
    expect(screen.getByRole('button', { name: 'フルページ' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
  });

  it('キャプチャボタン押下で onCapture を呼ぶ', async () => {
    // Given
    const onCapture = vi.fn();
    render(
      <CaptureMenuView
        mode="fullPage"
        copyToClipboard={false}
        progress={idleProgress}
        onModeChange={vi.fn()}
        onCopyToClipboardChange={vi.fn()}
        onCapture={onCapture}
      />,
    );
    // When
    await userEvent.click(screen.getByRole('button', { name: 'キャプチャ' }));
    // Then
    expect(onCapture).toHaveBeenCalledTimes(1);
  });

  it('エラー進捗のときにメッセージを表示する', () => {
    render(
      <CaptureMenuView
        mode="visible"
        copyToClipboard={false}
        progress={errorProgress('このページはキャプチャできません')}
        onModeChange={vi.fn()}
        onCopyToClipboardChange={vi.fn()}
        onCapture={vi.fn()}
      />,
    );
    expect(screen.getByText('このページはキャプチャできません')).toBeInTheDocument();
  });
});
