import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Podmiot1, Podmiot1K } from '../../types/fa1.types';
import { generatePodmiot1Podmiot1K } from './Podmiot1Podmiot1K';
import { Content } from 'pdfmake/interfaces';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label: string) => [{ text: `HEADER:${label}` }]),
  createLabelText: vi.fn((label: string, value: any) => ({
    text: `LABEL:${label}${value && value._text ? value._text : value}`,
  })),
  createSubHeader: vi.fn((label: string) => ({ text: `SUBHEADER:${label}` })),
  verticalSpacing: vi.fn((v: number) => ({ text: `SPACING:${v}` })),
  getTable: vi.fn((data) => data || []),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));
vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn((adres: any, label: string) => ({ adr: label })),
}));
vi.mock('./PodmiotDaneIdentyfikacyjne', () => ({
  generateDaneIdentyfikacyjne: vi.fn(() => [{ id: 'ID' }]),
}));
vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn(() => ({ contact: 'KONTAKT' })),
}));

describe('generatePodmiot1Podmiot1K', () => {
  beforeEach(() => vi.clearAllMocks());

  it('puts DaneIdentyfikacyjne & status in firstColumn', () => {
    const podmiot1: Podmiot1 = {
      NrEORI: { _text: 'EORI' },
      DaneIdentyfikacyjne: { NIP: { _text: '777' } },
      StatusInfoPodatnika: { _text: 'AKTYWNY' },
    };
    const podmiot1K: Podmiot1K = {};
    const result: any = generatePodmiot1Podmiot1K(podmiot1, podmiot1K);
    const firstCol: Content = result.find((r: any) => r.columns)?.columns[0];

    expect(firstCol).toEqual(
      expect.arrayContaining([
        { text: 'SUBHEADER:Dane identyfikacyjne' },
        { text: 'LABEL:Numer EORI: EORI' },
        { id: 'ID' },
        { text: 'LABEL:Status podatnika: AKTYWNY' },
      ])
    );
  });

  it('adds contact if Email present', () => {
    const podmiot1: Podmiot1 = {
      NrEORI: { _text: 'EORI' },
      Email: { _text: 'mail@ex.pl' },
    };
    const podmiot1K: Podmiot1K = {};
    const result: any = generatePodmiot1Podmiot1K(podmiot1, podmiot1K);
    const firstCol: Content = result.find((r: any) => r.columns)?.columns[0];

    expect(firstCol).toEqual(expect.arrayContaining([{ contact: 'KONTAKT' }]));
  });

  it('adds verticalSpacing at the end', () => {
    const podmiot1: Podmiot1 = { NrEORI: { _text: 'EORI' } };
    const podmiot1K: Podmiot1K = {};
    const result: Content[] = generatePodmiot1Podmiot1K(podmiot1, podmiot1K);

    expect(result[result.length - 1]).toEqual({ text: 'SPACING:1' });
  });
});
