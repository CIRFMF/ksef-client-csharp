import { describe, expect, it } from 'vitest';

import {
  createLabelText,
  formatText,
  generateLine,
  generateQRCode,
  getKraj,
  getNumber,
  getNumberRounded,
  getValue,
  hasValue,
  verticalSpacing,
} from './PDF-functions';
import FormatTyp, { Position } from './enums/common.enum';

describe('formatText', () => {
  it('returns empty string for null or undefined value', () => {
    expect(formatText(null)).toBe('');
    expect(formatText(undefined)).toBe('');
  });

  it('formats text with style and options', () => {
    const content = formatText('100', FormatTyp.Currency, { bold: true }, 'PLN');

    expect(content).toEqual(
      expect.objectContaining({
        alignment: Position.RIGHT,
        text: '100,00 PLN',
        style: FormatTyp.Currency,
        bold: true,
      })
    );
  });
});

describe('hasValue', () => {
  it('returns false for undefined, empty object without _text', () => {
    expect(hasValue(undefined)).toBe(false);
    expect(hasValue({} as any)).toBe(false);
  });

  it('returns true for string or object with _text', () => {
    expect(hasValue('val')).toBe(true);
    expect(hasValue({ _text: '123' })).toBe(true);
  });
});

describe('getValue', () => {
  it('returns _text if object, else value as is', () => {
    expect(getValue({ _text: 'abc' })).toBe('abc');
    expect(getValue('abc')).toBe('abc');
    expect(getValue(42)).toBe(42);
  });
});

describe('getNumber and getNumberRounded', () => {
  it('parses number from strings or numbers and rounds correctly', () => {
    expect(getNumber('123.456')).toBeCloseTo(123.456);
    expect(getNumber(undefined)).toBe(0);
    expect(getNumber({ _text: '456.789' })).toBeCloseTo(456.789);

    expect(getNumberRounded('123.456')).toBe(123.46);
    expect(getNumberRounded(123.452)).toBe(123.45);
  });
});

describe('createLabelText', () => {
  it('returns empty array for null or object without _text', () => {
    expect(createLabelText('label', null)).toEqual([]);
    expect(createLabelText('label', {} as any)).toEqual([]);
  });

  it('returns formatted label and value for primitives and FP objects', () => {
    const fp = { _text: 'val' };
    const result1 = createLabelText('Label', fp);
    const result2 = createLabelText('Label', 'value');

    expect(
      typeof result1[0] === 'object' && 'text' in result1[0] && (result1[0] as any).text.length === 2
    ).toBe(true);
    expect(
      typeof result2[0] === 'object' && 'text' in result2[0] && (result2[0] as any).text.length === 2
    ).toBe(true);
  });
});

describe('generateQRCode', () => {
  it('returns undefined if no qrCode provided', () => {
    expect(generateQRCode()).toBeUndefined();
  });

  it('returns ContentQr object with expected properties', () => {
    const qr = generateQRCode('abc123');

    expect(qr).toMatchObject({
      qr: 'abc123',
      fit: 150,
      foreground: 'black',
      background: 'white',
      eccLevel: 'M',
    });
  });
});

describe('getKraj', () => {
  it('returns country name if code exists, else returns input code', () => {
    expect(getKraj('PL')).toBe('Polska');
    expect(getKraj('XYZ')).toBe('XYZ');
  });
});

describe('verticalSpacing', () => {
  it('returns ContentText with text as newline and correct fontSize', () => {
    const spacing = verticalSpacing(10);

    expect(spacing).toEqual({ text: '\n', fontSize: 10 });
  });
});

describe('generateLine', () => {
  it('returns Content with table with expected layout properties', () => {
    const lineContent = generateLine();

    expect(lineContent).toHaveProperty('table');
    expect(lineContent).toHaveProperty('layout');
  });
});
