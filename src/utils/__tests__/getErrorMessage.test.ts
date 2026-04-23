import { describe, expect, it } from 'vitest';
import { getErrorMessage } from '../getErrorMessage';

describe('getErrorMessage', () => {
  it('Error インスタンスの message を返す', () => {
    // Given
    const err = new Error('boom');
    // When
    const msg = getErrorMessage(err);
    // Then
    expect(msg).toBe('boom');
  });

  it('string をそのまま返す', () => {
    expect(getErrorMessage('failed')).toBe('failed');
  });

  it('オブジェクトは JSON 文字列化する', () => {
    expect(getErrorMessage({ code: 42 })).toBe('{"code":42}');
  });

  it('循環参照の場合は "Unknown error" を返す', () => {
    const circular: { self?: unknown } = {};
    circular.self = circular;
    expect(getErrorMessage(circular)).toBe('Unknown error');
  });
});
