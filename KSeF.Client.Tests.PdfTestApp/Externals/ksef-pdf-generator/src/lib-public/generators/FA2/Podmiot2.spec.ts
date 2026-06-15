import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Content } from 'pdfmake/interfaces';
import type { FP, Podmiot2 } from '../../types/fa2.types';
import { generatePodmiot2 } from './Podmiot2';
import { createHeader, createLabelText, formatText } from '../../../shared/PDF-functions';
import { generateAdres } from './Adres';
import { generateDaneIdentyfikacyjneTPodmiot2Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot2Dto';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string): Content[] => [{ text, style: 'header' }]),
  createLabelText: vi.fn((label: string, value: any): Content[] => [{ text: `${label}${value ?? ''}` }]),
  formatText: vi.fn((text: string, style?: any): Content => ({ text, style })),
}));

vi.mock('./Adres', async () => {
  const actual = await vi.importActual('./Adres');

  return {
    ...actual,
    generateAdres: vi.fn((adres: any): Content[] => [{ text: 'mockAddress' }]),
  };
});

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot2Dto', async () => {
  const actual = await vi.importActual('./PodmiotDaneIdentyfikacyjneTPodmiot2Dto');

  return {
    ...actual,
    generateDaneIdentyfikacyjneTPodmiot2Dto: vi.fn((data: any): Content[] => [
      { text: 'mockDaneIdentyfikacyjne' },
    ]),
  };
});

vi.mock('./PodmiotDaneKontaktowe', async () => {
  const actual = await vi.importActual('./PodmiotDaneKontaktowe');

  return {
    ...actual,
    generateDaneKontaktowe: vi.fn((data: any): Content[] => [{ text: 'mockDaneKontaktowe' }]),
  };
});

describe(generatePodmiot2.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('generates basic header and identifiers', () => {
    const podmiot: Partial<Podmiot2> = { IDNabywcy: 'ID123' as FP, NrEORI: 'EORI123' as FP };
    const result = generatePodmiot2(podmiot as Podmiot2);

    expect(createHeader).toHaveBeenCalledWith('Nabywca');
    expect(createLabelText).toHaveBeenCalledWith('Identyfikator nabywcy: ', 'ID123');
    expect(createLabelText).toHaveBeenCalledWith('Numer EORI: ', 'EORI123');
    expect(result[0]).toEqual({ text: 'Nabywca', style: 'header' });
  });

  it('generates identification data if provided', () => {
    const podmiot: Partial<Podmiot2> = {
      DaneIdentyfikacyjne: { NIP: '123', Nazwa: 'Firma' } as any,
    };
    const result = generatePodmiot2(podmiot as Podmiot2);

    expect(generateDaneIdentyfikacyjneTPodmiot2Dto).toHaveBeenCalledWith(podmiot.DaneIdentyfikacyjne);
    expect(result.some((c: any) => c.text === 'mockDaneIdentyfikacyjne')).toBe(true);
  });

  it('generates address and correspondence address', () => {
    const podmiot: Partial<Podmiot2> = {
      Adres: { KodKraju: 'PL' } as any,
      AdresKoresp: { KodKraju: 'PL-K' } as any,
    };
    const result = generatePodmiot2(podmiot as Podmiot2);

    expect(generateAdres).toHaveBeenCalledWith(podmiot.Adres);
    expect(generateAdres).toHaveBeenCalledWith(podmiot.AdresKoresp);
    expect(formatText).toHaveBeenCalledWith('Adres', ['Label', 'LabelMargin']);
    expect(formatText).toHaveBeenCalledWith('Adres do korespondencji', ['Label', 'LabelMargin']);
    expect(result.some((c: any) => c.text === 'mockAddress')).toBe(true);
  });

  it('generates contact data and client number if provided', () => {
    const podmiot: Partial<Podmiot2> = {
      DaneKontaktowe: [{ Telefon: '123' }] as any,
      NrKlienta: 'CL123' as FP,
    };
    const result = generatePodmiot2(podmiot as Podmiot2);

    expect(generateDaneKontaktowe).toHaveBeenCalledWith(podmiot.DaneKontaktowe);
    expect(createLabelText).toHaveBeenCalledWith('Numer klienta: ', 'CL123');
    expect(result.some((c: any) => c.text === 'mockDaneKontaktowe')).toBe(true);
  });

  it('handles all fields together', () => {
    const podmiot: Partial<Podmiot2> = {
      DaneIdentyfikacyjne: { NIP: '123', Nazwa: 'Firma' } as any,
      Adres: { KodKraju: 'PL' } as any,
      AdresKoresp: { KodKraju: 'PL-K' } as any,
      DaneKontaktowe: [{ Telefon: '123' }] as any,
    };
    const result: Content = generatePodmiot2(podmiot as Podmiot2);

    expect(result[0]).toEqual({ text: 'Nabywca', style: 'header' });
    expect(result.some((c: any): boolean => c.text === 'mockDaneIdentyfikacyjne')).toBe(true);
    expect(
      result.filter((c: any): boolean => c.text === 'mockAddress').length + (result as any)[5].length
    ).toBe(2);
    expect(result.some((c: any): boolean => c.text === 'mockDaneKontaktowe')).toBe(true);
  });
});
