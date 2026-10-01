import { describe, it, expect } from 'vitest';
import { toSafeHttpUrl } from './safeUrl';

describe('toSafeHttpUrl', () => {
  it('accepts http and https URLs', () => {
    expect(toSafeHttpUrl('https://example.blob.core.windows.net/a.pdf?sig=abc')).toBe(
      'https://example.blob.core.windows.net/a.pdf?sig=abc',
    );
    expect(toSafeHttpUrl('http://example.com/')).toBe('http://example.com/');
  });

  it.each(['javascript:alert(1)', 'data:text/html;base64,AAAA', 'ftp://example.com', '/relative/path', 'not a url', ''])(
    'rejects %s',
    (value) => {
      expect(toSafeHttpUrl(value)).toBeNull();
    },
  );

  it('rejects null and undefined', () => {
    expect(toSafeHttpUrl(null)).toBeNull();
    expect(toSafeHttpUrl(undefined)).toBeNull();
  });
});
