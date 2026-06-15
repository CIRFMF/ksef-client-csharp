import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Content } from 'pdfmake/interfaces';
import type { Podmiot3 } from '../../types/fa3.types';
import { generatePodmiot2 } from './Podmiot2';
import { createHeader, createLabelText, formatText } from '../../../shared/PDF-functions';
import { generateAdres } from './Adres';
import { generateDaneIdentyfikacyjneTPodmiot2Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot2Dto';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string): Content[] => [{ text, style: 'header' } as Content]),
  createLabelText: vi.fn((label: string, value: any): Content[] => [
    { text: `${label}${value ?? ''}` } as Content,
  ]),
  formatText: vi.fn((text: any, style?: any): Content => ({ text: String(text), style }) as Content),
  getTable: vi.fn((data: any) => (data ? (Array.isArray(data) ? data : [data]) : [])),
}));

vi.mock('./Adres', async () => {
  const actual = await vi.importActual('./Adres');

  return {
    ...actual,
    generateAdres: vi.fn((): Content[] => [{ text: 'mockAddress' } as Content]),
  };
});

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot2Dto', async () => {
  const actual = await vi.importActual('./PodmiotDaneIdentyfikacyjneTPodmiot2Dto');

  return {
    ...actual,
    generateDaneIdentyfikacyjneTPodmiot2Dto: vi.fn((): Content[] => [
      { text: 'mockDaneIdentyfikacyjne' } as Content,
    ]),
  };
});

vi.mock('./PodmiotDaneKontaktowe', async () => {
  const actual = await vi.importActual('./PodmiotDaneKontaktowe');

  return {
    ...actual,
    generateDaneKontaktowe: vi.fn((): Content[] => [{ text: 'mockDaneKontaktowe' } as Content]),
  };
});

describe(generatePodmiot2.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('generates basic header and identifiers', () => {
    const podmiot: Partial<Podmiot3> = { IDNabywcy: 'ID123' as any, NrEORI: 'EORI123' as any };
    const result = generatePodmiot2(podmiot as Podmiot3);

    expect(createHeader).toHaveBeenCalledWith('Nabywca');
    expect(createLabelText).toHaveBeenCalledWith('Identyfikator nabywcy: ', 'ID123');
    expect(createLabelText).toHaveBeenCalledWith('Numer EORI: ', 'EORI123');
    expect(result[0]).toEqual({ text: 'Nabywca', style: 'header' });
  });

  it('generates identification data if provided', () => {
    const podmiot: Partial<Podmiot3> = {
      DaneIdentyfikacyjne: { NIP: '123', Nazwa: 'Firma' } as any,
    };
    const result = generatePodmiot2(podmiot as Podmiot3);

    expect(generateDaneIdentyfikacyjneTPodmiot2Dto).toHaveBeenCalledWith(podmiot.DaneIdentyfikacyjne);
    expect(result.some((c: any) => c.text === 'mockDaneIdentyfikacyjne')).toBe(true);
  });

  it('generates address and correspondence address', () => {
    const podmiot: Partial<Podmiot3> = {
      Adres: { KodKraju: 'PL' } as any,
      AdresKoresp: { KodKraju: 'PL-K' } as any,
    };
    const result = generatePodmiot2(podmiot as Podmiot3);

    expect(generateAdres).toHaveBeenCalledWith(podmiot.Adres);
    expect(generateAdres).toHaveBeenCalledWith(podmiot.AdresKoresp);
    expect(formatText).toHaveBeenCalledWith('Adres', ['Label', 'LabelMargin']);
    expect(formatText).toHaveBeenCalledWith('Adres do korespondencji', ['Label', 'LabelMargin']);
    expect(result.some((c: any) => c.text === 'mockAddress')).toBe(true);
  });

  it('generates contact data and client number if provided', () => {
    const podmiot: Partial<Podmiot3> = {
      DaneKontaktowe: [{ Telefon: '123' }] as any,
      NrKlienta: 'CL123' as any,
    };
    const result = generatePodmiot2(podmiot as Podmiot3);

    expect(generateDaneKontaktowe).toHaveBeenCalledWith(podmiot.DaneKontaktowe);
    expect(createLabelText).toHaveBeenCalledWith('Numer klienta: ', 'CL123');
    expect(result.some((c: any) => c.text === 'mockDaneKontaktowe')).toBe(true);
  });

  it('handles all fields together', () => {
    const podmiot: Partial<Podmiot3> = {
      IDNabywcy: 'ID123' as any,
      NrEORI: 'EORI123' as any,
      DaneIdentyfikacyjne: { NIP: '123', Nazwa: 'Firma' } as any,
      Adres: { KodKraju: 'PL' } as any,
      AdresKoresp: { KodKraju: 'PL-K' } as any,
      DaneKontaktowe: [{ Telefon: '123' }] as any,
      NrKlienta: 'CL123' as any,
    };
    const result = generatePodmiot2(podmiot as Podmiot3) as any[];

    expect(result[0]).toEqual({ text: 'Nabywca', style: 'header' });
    expect(result.some((c: any) => c.text === 'mockDaneIdentyfikacyjne')).toBe(true);

    const addressCount = result.filter((c: any) => c.text === 'mockAddress').length;
    const fifthElementLength = Array.isArray(result[5]) ? result[5].length : 0;

    expect(addressCount + fifthElementLength).toBe(2);
    expect(result.some((c: any) => c.text === 'mockDaneKontaktowe')).toBe(true);
    expect(createLabelText).toHaveBeenCalledWith('Numer klienta: ', 'CL123');
  });
});
