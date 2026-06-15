import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { parseXML, stripPrefixes } from './XML-parser';

describe('stripPrefixes', () => {
  it('removes prefixes from object keys', () => {
    const input = {
      'ns:name': 'test',
      other: {
        'x:value': 10,
        array: [{ 'a:item': 1 }, { 'a:item': 2 }],
      },
    };

    const expected = {
      name: 'test',
      other: {
        value: 10,
        array: [{ item: 1 }, { item: 2 }],
      },
    };

    expect(stripPrefixes(input)).toEqual(expected);
  });

  it('returns primitives unchanged', () => {
    expect(stripPrefixes('string')).toBe('string');
    expect(stripPrefixes(123)).toBe(123);
    expect(stripPrefixes(null)).toBeNull();
    expect(stripPrefixes(undefined)).toBeUndefined();
  });

  it('recursively processes arrays', () => {
    const input = [{ 'ns:key1': 'value1' }, { 'ns:key2': 'value2' }];

    const expected = [{ key1: 'value1' }, { key2: 'value2' }];

    expect(stripPrefixes(input)).toEqual(expected);
  });
});

describe('parseXML', () => {
  const xmlSample = `<root><ns:name>test</ns:name></root>`;
  const file = new File([xmlSample], 'test.xml', { type: 'text/xml' });

  beforeEach(() => {
    vi.stubGlobal(
      'FileReader',
      class {
        onload: ((this: FileReader, ev: ProgressEvent<FileReader>) => any) | null = null;
        public readAsText(): void {
          const event = { target: { result: xmlSample } } as ProgressEvent<FileReader>;

          setTimeout(
            () =>
              (this.onload as ((this: FileReader, ev: ProgressEvent<FileReader>) => void) | null)?.call(
                this as unknown as FileReader,
                event
              ),
            0
          );
        }
      }
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('correctly parses XML into a JSON object', async () => {
    const result: any = await parseXML(file);

    expect(result).toHaveProperty('root');
    expect(Object.keys(result.root)).toContain('name');
  });

  it('rejects the promise with an error on invalid XML', async () => {
    vi.stubGlobal(
      'FileReader',
      class {
        onload: ((this: FileReader, ev: ProgressEvent<FileReader>) => any) | null = null;
        public readAsText(): void {
          const event = { target: { result: '<root><unclosed>' } } as ProgressEvent<FileReader>;

          setTimeout(
            () =>
              (this.onload as ((this: FileReader, ev: ProgressEvent<FileReader>) => void) | null)?.call(
                this as unknown as FileReader,
                event
              ),
            0
          );
        }
      }
    );
    await expect(parseXML(file)).rejects.toThrow();
  });
});
