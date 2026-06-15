import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Content } from 'pdfmake/interfaces';
import type { Podmiot2, Podmiot2K } from '../../types/fa2.types';
import { generateAdres } from './Adres';
import { generatePodmiot2Podmiot2K } from './Podmiot2Podmiot2k';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string): Content[] => [{ text, style: 'header' }]),
  createLabelText: vi.fn((label: string, value: any): Content[] => [{ text: `${label}${value ?? ''}` }]),
  formatText: vi.fn((text: string, style?: any): Content => ({ text, style })),
  getTable: vi.fn((data: any) => data || []),
  hasValue: vi.fn((value: any) => value !== undefined && value !== null),
  verticalSpacing: vi.fn((margin: number) => ({ margin })),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));

vi.mock('./Adres', () => ({
  generateAdres: vi.fn((adres: any): Content[] => [{ text: 'mockAddress' }]),
}));

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot1Dto', () => ({
  generateDaneIdentyfikacyjneTPodmiot1Dto: vi.fn((data: any): Content[] => [
    { text: 'mockDaneIdentyfikacyjne' },
  ]),
}));

vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn((data: any): Content[] => [{ text: 'mockDaneKontaktowe' }]),
}));

describe(generatePodmiot2Podmiot2K.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('generates header and first column with ID, contact, and client number', () => {
    const podmiot2: Podmiot2 = {
      NrEORI: 'EORI123',
      DaneKontaktowe: [{ Telefon: '123' }],
      NrKlienta: 'CL123',
    } as any;
    const podmiot2K: Podmiot2K = { IDNabywcy: 'ID123' } as any;
    const result = generatePodmiot2Podmiot2K(podmiot2, podmiot2K) as any;
    expect(result[0]).toEqual({ text: 'Nabywca', style: 'header' });

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
    const podmiot2: Podmiot2 = { NrEORI: 'EORI123', AdresKoresp: { Ulica: 'Test' } } as any;
    const podmiot2K: Podmiot2K = {} as any;
    generatePodmiot2Podmiot2K(podmiot2, podmiot2K);
    expect(generateAdres).toHaveBeenCalledWith(podmiot2.AdresKoresp);
  });

  it('generates corrected content columns', () => {
    const podmiot2: Podmiot2 = { NrEORI: 'EORI123' } as any;
    const podmiot2K: Podmiot2K = { IDNabywcy: 'ID123' } as any;
    const result = generatePodmiot2Podmiot2K(podmiot2, podmiot2K) as any;
    expect(Array.isArray(result[2].columns[0])).toBe(true);
    expect(Array.isArray(result[2].columns[1])).toBe(false);
    expect(result[2].columns[0].length).toBeGreaterThanOrEqual(0);
  });

  it('adds vertical spacing at the end', () => {
    const podmiot2: Podmiot2 = { NrEORI: 'EORI123' } as any;
    const podmiot2K: Podmiot2K = {} as any;
    const result = generatePodmiot2Podmiot2K(podmiot2, podmiot2K);

    expect(result[result.length - 1]).toEqual({ margin: 1 });
  });

  it('handles all fields together', () => {
    const podmiot2: Podmiot2 = {
      NrEORI: 'EORI123',
      DaneIdentyfikacyjne: { NIP: '123' } as any,
      DaneKontaktowe: [{ Telefon: '123' }],
      NrKlienta: 'CL123',
      AdresKoresp: { Ulica: 'Test' },
    } as any;
    const podmiot2K: Podmiot2K = { IDNabywcy: 'ID123' } as any;
    const result = generatePodmiot2Podmiot2K(podmiot2, podmiot2K) as any;
    expect(result.length).toBeGreaterThan(3);
    expect(result[0]).toEqual({ text: 'Nabywca', style: 'header' });

    expect(result[2]).toHaveProperty('columns');
    expect(Array.isArray(result[2].columns[0])).toBe(true);
    expect(result[2].columns[0].length).toBeGreaterThanOrEqual(0);

    expect(result[result.length - 1]).toHaveProperty('margin');
  });
});
