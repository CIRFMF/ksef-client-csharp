import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Podmiot2 } from '../../types/fa1.types';
import { generatePodmiot2 } from './Podmiot2';

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
  generatePodmiotAdres: vi.fn((adres: any, label: string) => ({ adr: label })),
}));
vi.mock('./PodmiotDaneIdentyfikacyjne', () => ({
  generateDaneIdentyfikacyjne: vi.fn(() => [{ id: 'ID' }]),
}));
vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn(() => [{ contact: 'KONTAKT' }]),
}));

describe('generatePodmiot2', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders header and base fields', () => {
    const podmiot2: Podmiot2 = {
      NrEORI: { _text: 'EORI777' },
    };
    const result = generatePodmiot2(podmiot2);

    expect(result).toEqual(
      expect.arrayContaining([{ text: 'HEADER:Nabywca' }, { text: 'LABEL:Numer EORI: EORI777' }])
    );
  });

  it('adds PrefiksNabywcy if hasValue true', () => {
    const podmiot2: Podmiot2 = {
      NrEORI: { _text: 'EORI1' },
      PrefiksNabywcy: { _text: 'PN1' },
    };
    const result = generatePodmiot2(podmiot2);

    expect(result).toEqual(expect.arrayContaining([{ text: 'LABEL:Prefiks VAT: PN1' }]));
  });

  it('includes contact info when Email or Telefon present', () => {
    const podmiot2: Podmiot2 = {
      NrEORI: { _text: 'EORI4' },
      Email: { _text: 'test@mail.com' },
    };
    const result = generatePodmiot2(podmiot2);

    expect(result).toEqual(expect.arrayContaining([{ text: 'FMT:Dane kontaktowe' }, { contact: 'KONTAKT' }]));
  });

  it('includes NrKlienta label if present', () => {
    const podmiot2: Podmiot2 = {
      NrEORI: { _text: 'EORI5' },
      NrKlienta: { _text: 'NR1234' },
    };
    const result = generatePodmiot2(podmiot2);

    expect(result).toEqual(expect.arrayContaining([{ text: 'LABEL:Numer klienta: NR1234' }]));
  });
});
