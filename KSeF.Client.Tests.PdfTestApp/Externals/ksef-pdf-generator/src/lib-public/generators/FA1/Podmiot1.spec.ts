import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Podmiot1 } from '../../types/fa1.types';
import { generatePodmiot1 } from './Podmiot1';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label: string) => [{ text: `HEADER:${label}` }]),
  createLabelText: vi.fn((label: string, value: any) => ({
    text: `LABEL:${label}${typeof value === 'object' && value?._text ? value._text : value}`,
  })),
  formatText: vi.fn((text: string, _args?: any) => ({ text: `FMT:${text}` })),
  getTable: vi.fn((data) => data || []),
  getValue: vi.fn((val) => val?._text || ''),
  hasValue: vi.fn((val) => Boolean(val && val._text)),
}));
vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn((adres: any, label: string) => ({ adr: `${label}` })),
}));
vi.mock('./PodmiotDaneIdentyfikacyjne', () => ({
  generateDaneIdentyfikacyjne: vi.fn((daneId: any) => [{ id: 'ID' }]),
}));
vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn((email: any, tel: any) => [{ contact: 'KONTAKT' }]),
}));

describe('generatePodmiot1', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders header and base labels', () => {
    const podmiot1: Podmiot1 = {
      NrEORI: { _text: 'EORI123' },
      PrefiksPodatnika: { _text: 'PL' },
    };
    const result = generatePodmiot1(podmiot1);

    expect(result).toEqual(
      expect.arrayContaining([
        { text: 'HEADER:Sprzedawca' },
        { text: 'LABEL:Numer EORI: EORI123' },
        { text: 'LABEL:Prefiks VAT: PL' },
      ])
    );
  });

  it('calls generateDaneIdentyfikacyjne when present', () => {
    const podmiot1: Podmiot1 = {
      DaneIdentyfikacyjne: { NIP: { _text: '1234' } },
      NrEORI: { _text: 'xxx' },
      PrefiksPodatnika: { _text: 'PL' },
    };
    const result = generatePodmiot1(podmiot1);

    expect(result).toEqual(expect.arrayContaining([{ id: 'ID' }]));
  });

  it('renders PodmiotAdres if adres present', () => {
    const podmiot1: Podmiot1 = {
      NrEORI: { _text: 'xxx' },
      PrefiksPodatnika: { _text: 'PL' },
      Adres: { AdresPol: { Miasto: { _text: 'Katowice' } } },
    };
    const result = generatePodmiot1(podmiot1);

    expect(result).toEqual(expect.arrayContaining([{ adr: 'Adres' }]));
  });

  it('renders contact section and status if Email/Telefon present', () => {
    const podmiot1: Podmiot1 = {
      NrEORI: { _text: 'xxx' },
      PrefiksPodatnika: { _text: 'PL' },
      Email: { _text: 'a@b.pl' },
      StatusInfoPodatnika: { _text: '2' },
    };
    const result = generatePodmiot1(podmiot1);

    expect(result).toEqual(
      expect.arrayContaining([
        { text: 'FMT:Dane kontaktowe' },
        { contact: 'KONTAKT' },
        { text: 'LABEL:Status podatnika: Postępowanie restrukturyzacyjne' },
      ])
    );
  });

  it('renders only status if no Email/Telefon but StatusInfoPodatnika present', () => {
    const podmiot1: Podmiot1 = {
      NrEORI: { _text: 'xxx' },
      PrefiksPodatnika: { _text: 'PL' },
      StatusInfoPodatnika: { _text: '1' },
    };
    const result = generatePodmiot1(podmiot1);

    expect(result).toEqual(expect.arrayContaining([{ text: 'LABEL:Status podatnika: Stan likwidacji' }]));
  });
});
