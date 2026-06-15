import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Content } from 'pdfmake/interfaces';
import type { Podmiot1, Podmiot1K } from '../../types/fa3.types';
import { generatePodmiot1Podmiot1K } from './Podmiot1Podmiot1K';
import { generateAdres } from './Adres';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string): Content[] => [{ text, style: 'header' }]),
  createLabelText: vi.fn((label: string, value: any): Content[] => [{ text: `${label}${value ?? ''}` }]),
  getTable: vi.fn((data: any) => data || []),
  formatText: vi.fn((text: string, style?: any): Content => ({ text, style })),
  verticalSpacing: vi.fn((margin: number) => ({ margin })),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));

vi.mock('./Adres', () => ({
  generateAdres: vi.fn((adres: any): Content[] => [{ text: 'mockAddress' }]),
}));

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot1Dto', async () => {
  const actual = await vi.importActual('./PodmiotDaneIdentyfikacyjneTPodmiot1Dto');
  return {
    ...actual,
    generateDaneIdentyfikacyjneTPodmiot1Dto: vi.fn((data: any): Content[] => [
      { text: 'mockDaneIdentyfikacyjne' },
    ]),
  };
});

vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn((data: any): Content[] => [{ text: 'mockDaneKontaktowe' }]),
}));

vi.mock('./Podmiot1Podmiot1K', async () => {
  const original: any = await vi.importActual('./Podmiot1Podmiot1K');
  return {
    ...original,
    generateCorrectedContent: vi.fn((podmiot: any): Content[] => [
      { text: `mockCorrectedContent-${podmiot?.PrefiksPodatnika?._text || 'noPrefix'}` },
    ]),
  };
});

describe(generatePodmiot1Podmiot1K.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('generates main header and columns', () => {
    const podmiot1: Podmiot1 = { NrEORI: '123' } as any;
    const podmiot1K: Podmiot1K = {} as any;
    const result: any = generatePodmiot1Podmiot1K(podmiot1, podmiot1K);

    expect(result[0]).toEqual({ text: 'Sprzedawca', style: 'header' });

    expect(result[1]).toHaveProperty('columns');
    expect(Array.isArray(result[1].columns[0])).toBe(true);
    expect(Array.isArray(result[1].columns[1])).toBe(true);
    expect(result[1].columns[0].length).toBeGreaterThan(0);
    expect(result[1].columns[1].length).toBe(0);

    expect(result[2]).toHaveProperty('columns');
    expect(Array.isArray(result[2].columns[0])).toBe(true);
    expect(result[2].columns[0].length).toBeGreaterThan(0);

    expect(result[3]).toEqual({ margin: 1 });
  });

  it('calls generateAdres if AdresKoresp exists', () => {
    const podmiot1: Podmiot1 = { NrEORI: '123', AdresKoresp: { Ulica: 'Test' } } as any;
    const podmiot1K: Podmiot1K = {} as any;
    generatePodmiot1Podmiot1K(podmiot1, podmiot1K);
    expect(generateAdres).toHaveBeenCalledWith(podmiot1.AdresKoresp);
  });

  it('handles all fields together', () => {
    const podmiot1: Podmiot1 = {
      NrEORI: '123',
      DaneIdentyfikacyjne: { NIP: '123', Nazwa: 'Firma' } as any,
      DaneKontaktowe: [{ Telefon: '123' }] as any,
      StatusInfoPodatnika: 'active',
      AdresKoresp: { Ulica: 'Test' },
    } as any;
    const podmiot1K: Podmiot1K = { PrefiksPodatnika: { _text: 'PL' } } as any;
    const result: any = generatePodmiot1Podmiot1K(podmiot1, podmiot1K);

    expect(result.length).toBe(4);
    expect(result[0]).toEqual({ text: 'Sprzedawca', style: 'header' });
    expect(result[1].columns[0]).toBeInstanceOf(Array);
    expect(result[1].columns[1]).toBeInstanceOf(Array);
    expect(result[2].columns[0]).toBeInstanceOf(Array);
    expect(result[3]).toEqual({ margin: 1 });
  });
});
