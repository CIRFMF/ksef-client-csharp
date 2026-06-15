import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Fa, Faktura, FP } from '../../types/fa1.types';
import { generatePodsumowanieStawekPodatkuVat, getSummaryTaxRate } from './PodsumowanieStawekPodatkuVat';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label) => [{ text: `HEADER:${label}` }]),
  createSection: vi.fn((input) => input),
  formatText: vi.fn((text, style) => ({ text, style })),
  getValue: vi.fn((val) =>
    val && val._text ? (typeof val._text === 'string' ? val._text : val._text.toString()) : ''
  ),
  hasValue: vi.fn((val) => !!(val && val._text)),
  getNumberRounded: vi.fn((val) => (typeof val === 'number' ? val : parseFloat(val?._text ?? '0') || 0)),
}));

describe('generatePodsumowanieStawekPodatkuVat', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty array if no relevant Fa data', () => {
    const faktura: Faktura = { Fa: {} as Fa };

    expect(generatePodsumowanieStawekPodatkuVat(faktura)).toEqual([]);
  });

  it('creates table with header and data rows', () => {
    const fa: Fa = {
      P_13_1: { _text: '100' },
      P_14_1: { _text: '23' },
      P_14_1W: { _text: '23' },
    };
    const faktura: Faktura = { Fa: fa };

    const result: any = generatePodsumowanieStawekPodatkuVat(faktura);

    expect(result.some((r: any) => r.table && r.table.body)).toBeTruthy();
    expect(result[0].text).toContain('Podsumowanie stawek podatku'); // header present
  });
});

describe('getSummaryTaxRate', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty summarization if no values', () => {
    const fa: Fa = {};

    expect(getSummaryTaxRate(fa)).toEqual([]);
  });

  it('returns correct summary entries for each tax rate', () => {
    const floatText = (num: number): FP => ({ _text: num.toString() });

    const fa: Fa = {
      P_13_1: floatText(100),
      P_14_1: floatText(23),
      P_14_1W: floatText(23),
      P_13_2: floatText(200),
      P_14_2: floatText(16),
      P_14_2W: floatText(16),
      P_13_3: floatText(300),
      P_14_3: floatText(15),
      P_14_3W: floatText(15),
      P_13_4: floatText(400),
      P_14_4: floatText(12),
      P_14_4W: floatText(12),
      P_13_5: floatText(500),
      P_14_5: floatText(0),
      P_13_7: floatText(600),
    };

    const summary = getSummaryTaxRate(fa);

    expect(summary.length).toBe(6);
    expect(summary[0]).toMatchObject({
      no: 1,
      taxRateString: '23% lub 22%',
      net: '100.00',
      tax: '23.00',
      taxPLN: '23.00',
      gross: '123.00',
    });
    expect(summary[1].taxRateString).toBe('8% lub 7%');
    expect(summary[4].taxRateString).toBe('');
    expect(summary[5].taxRateString).toBe('zwolnione z opodatkowania');
  });

  it('includes only tax rates with non-zero values', () => {
    const fa: Fa = {
      P_13_1: { _text: '0' },
      P_14_1: { _text: '0' },
      P_14_1W: { _text: '0' },
    };

    expect(getSummaryTaxRate(fa)).toHaveLength(0);
  });
});
