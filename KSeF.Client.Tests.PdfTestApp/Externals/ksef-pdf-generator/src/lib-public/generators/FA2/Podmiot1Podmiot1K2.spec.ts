import { generateCorrectedContent } from './Podmiot1Podmiot1K';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { Content } from 'pdfmake/interfaces';
import { FP, Podmiot1K } from '../../types/fa2.types';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string): Content[] => [{ text, style: 'header' }]),
  createLabelText: vi.fn((label: string, value: any): Content[] => [{ text: `${label}${value ?? ''}` }]),
  formatText: vi.fn((text: string, style?: any): Content => ({ text, style })),
  verticalSpacing: vi.fn().mockImplementation((size) => ({ margin: size })),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));

vi.mock('./Adres', () => ({
  generateAdres: vi.fn((adres: any): Content[] => [{ text: 'mockAdres' }]),
}));

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot1Dto', () => ({
  generateDaneIdentyfikacyjneTPodmiot1Dto: vi.fn((data: any): Content[] => [
    { text: 'mockDaneIdentyfikacyjne' },
  ]),
}));

vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn((data: any): Content[] => [{ text: 'mockDaneKontaktowe' }]),
}));

describe(generateCorrectedContent.name, () => {
  const header = 'Treść korygowana';

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should generate corrected content base values ', () => {
    const podmiot: Podmiot1K = {};
    const result: Content[] = generateCorrectedContent(podmiot, header);

    expect((result[0] as any).some((c: any) => c.text === header)).toBe(true);
  });

  it('should generate corrected content with prefixPodatnika ', () => {
    const podmiot: Podmiot1K = { PrefiksPodatnika: { _text: 'tekst' } };
    const result: Content[] = generateCorrectedContent(podmiot, header);

    expect(result.length).equal(2);
    expect((result[0] as any).some((c: any) => c.text === header)).toBe(true);
    expect((result[1] as any).some((c: any) => c.text.includes('Prefiks VAT: '))).toBe(true);
    expect((result[1] as any).some((c: any) => c.text.includes('[object Object]'))).toBe(true);
  });

  it('should generate corrected content with id data ', () => {
    const podmiot: Podmiot1K = { DaneIdentyfikacyjne: { NIP: 'NIP' as FP, Nazwa: 'nazwa' as FP } };
    const result: Content[] = generateCorrectedContent(podmiot, header);

    expect(result.length).equal(2);
    expect((result[0] as any).some((c: any) => c.text === header)).toBe(true);
    expect((result[1] as any).text.includes('mockDaneIdentyfikacyjne')).toBe(true);
  });
  it('should generate corrected content with address ', () => {
    const podmiot: Podmiot1K = { Adres: { KodKraju: 'PL' as FP } };
    const result: any = generateCorrectedContent(podmiot, header);

    expect(result.length).equal(3);
    expect((result[0] as any).some((c: any) => c.text === header)).toBe(true);
    expect((result[1] as any).text.includes('Adres')).toBe(true);
    expect((result[2] as any).some((c: { text: string }) => c.text === 'mockAdres')).toBe(true);
  });

  it('handles all fields together', () => {
    const podmiot: Podmiot1K = {
      PrefiksPodatnika: { _text: 'PL' },
      DaneIdentyfikacyjne: { NIP: '123' as FP, Nazwa: 'Firma' as FP },
      Adres: { KodKraju: 'PL' as FP },
    };
    const result: any = generateCorrectedContent(podmiot, header);

    expect((result[0] as any).some((c: any) => c.text === header)).toBe(true);
    expect((result[1] as any).some((c: any) => c.text.includes('Prefiks VAT: '))).toBe(true);
    expect((result[1] as any).some((c: any) => c.text.includes('[object Object]'))).toBe(true);
    expect((result[2] as any).text.includes('mockDaneIdentyfikacyjne')).toBe(true);
    expect((result[3] as any).text.includes('Adres')).toBe(true);
    expect((result[4] as any).some((c: { text: string }) => c.text === 'mockAdres')).toBe(true);

    expect(result.length).toBe(5);
  });
});
