import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generatePodsumowanieStawekPodatkuVat, getSummaryTaxRate } from './PodsumowanieStawekPodatkuVat';
import {
  createHeader,
  createSection,
  formatText,
  getNumberRounded,
  getValue,
  hasValue,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string) => [{ text }]),
  createSection: vi.fn((content: any[], flag: boolean) => ({ content, flag })),
  formatText: vi.fn((value: any, type: FormatTyp) => ({ value, type })),
  getNumberRounded: vi.fn((val: any) => Number(val ?? 0)),
  getValue: vi.fn((val: any) => val?._text ?? val ?? 0),
  hasValue: vi.fn((val: any) => val !== null && val !== undefined && val !== 0),
}));

import type { Faktura, Fa, FP } from '../../types/fa2.types';

describe(generatePodsumowanieStawekPodatkuVat.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns empty array if no values in Fa', () => {
    const invoice: Faktura = { Fa: {} } as any;
    const result = generatePodsumowanieStawekPodatkuVat(invoice);
    expect(result).toEqual([]);
  });

  it('creates table when Fa has values', () => {
    const faData: Faktura['Fa'] = {
      P_13_1: 100,
      P_14_1: 23,
      P_14_1W: 23,
      P_13_2: 200,
      P_14_2: 16,
      P_14_2W: 16,
    } as any;

    const invoice: Faktura = { Fa: faData } as any;

    const result = generatePodsumowanieStawekPodatkuVat(invoice) as any;

    expect(createHeader).toHaveBeenCalledWith('Podsumowanie stawek podatku', [0, 0, 0, 8]);
    expect(createSection).toHaveBeenCalled();
    expect(result.flag).toBe(false);
    expect(result.content.length).toBeGreaterThan(0);
  });

  it('getSummaryTaxRate computes correct summary', () => {
    const fa: Fa = {
      P_13_1: 100,
      P_14_1: 23,
      P_14_1W: 23,
      P_13_2: 200,
      P_14_2: 16,
      P_14_2W: 16,
      P_13_5: 50,
      P_14_5: 0,
      P_13_6_1: 0,
      P_13_6_2: 0,
      P_13_6_3: 0,
      P_13_7: 0,
      P_13_8: 0,
      P_13_9: 0,
      P_13_10: 0,
      P_13_11: 0,
    } as any;

    const summary = getSummaryTaxRate(fa);

    expect(summary).toHaveLength(3);

    expect(summary[0].net).toBe('100.00');
    expect(summary[0].tax).toBe('23.00');
    expect(summary[0].gross).toBe('123.00');
    expect(summary[0].taxPLN).toBe('23.00');
    expect(summary[0].taxRateString).toBe('23% lub 22%');

    expect(summary[1].taxRateString).toBe('8% lub 7%');
    expect(summary[2].taxRateString).toBe('');
  });

  it('calls helper functions correctly', () => {
    const fa: Fa = { P_13_1: 100, P_14_1: 23, P_14_1W: 23 } as any;
    getSummaryTaxRate(fa);
    expect(hasValue).toHaveBeenCalled();
    expect(getValue).toHaveBeenCalled();
    expect(getNumberRounded).toHaveBeenCalled();
    expect(formatText).not.toHaveBeenCalled();
  });

  it('generates full content with formatting', () => {
    const faData: Faktura['Fa'] = {
      P_13_1: 100,
      P_14_1: 23,
      P_14_1W: 23,
    } as any;
    const invoice: Faktura = { Fa: faData } as any;
    const result = generatePodsumowanieStawekPodatkuVat(invoice) as any;
    const table = result.content[1];
    expect(table.table.body[0][0]).toEqual({ text: 'Lp.', style: FormatTyp.GrayBoldTitle });
    expect(table.table.body[1][0]).toEqual(1);
  });

  it('handles all P_13/P_14 combinations including zero and PLN', () => {
    const fa: Fa = {
      P_13_1: 100,
      P_14_1: 23,
      P_14_1W: 23,
      P_13_2: 200,
      P_14_2: 16,
      P_14_2W: 16,
      P_13_3: 50,
      P_14_3: 2,
      P_14_3W: 2,
      P_13_4: 40,
      P_14_4: 1,
      P_14_4W: 1,
      P_13_5: 30,
      P_14_5: 0,
      P_13_6_1: 10,
      P_13_6_2: 20,
      P_13_6_3: 30,
      P_13_7: 5,
      P_13_8: 6,
      P_13_9: 7,
      P_13_10: 8,
      P_13_11: 9,
    } as any;

    const summary = getSummaryTaxRate(fa);

    expect(summary).toHaveLength(13);
    expect(summary[0].taxRateString).toBe('23% lub 22%');
    expect(summary[1].taxRateString).toBe('8% lub 7%');
    expect(summary[2].taxRateString).toBe('5%');
    expect(summary[3].taxRateString).toBe('4% lub 3% lub oo');
    expect(summary[4].taxRateString).toBe('');
    expect(summary[5].taxRateString).toBe('0% - krajowe');
    expect(summary[6].taxRateString).toBe('0% - wdt');
    expect(summary[7].taxRateString).toBe('0% - eksport');
    expect(summary[8].taxRateString).toBe('zwolnione z opodatkowania');
    expect(summary[9].taxRateString).toBe('np z wyłączeniem art. 100 ust 1 pkt 4 ustawy');
    expect(summary[10].taxRateString).toBe('np na podstawie art. 100 ust. 1 pkt 4 ustawy');
  });

  it('handles reverse charge and margin', () => {
    const fa: Fa = {
      P_13_10: 123,
      P_13_11: 456,
    } as any;

    const summary = getSummaryTaxRate(fa);
    expect(summary).toHaveLength(2);
    expect(summary[0].taxRateString).toBe('odwrotne obciążenie');
    expect(summary[1].taxRateString).toBe('marża');
  });
});
