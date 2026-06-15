import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateDaneIdentyfikacyjneTPodmiot3Dto } from './Podmiot3Podmiot2k';
import { Podmiot3Podmiot2KDto } from '../../types/fa1-additional-types';
import { Content } from 'pdfmake/interfaces';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label) => [{ text: `HEADER:${label}` }]),
  createLabelText: vi.fn((label, value) => ({
    text: `LABEL:${label}${value && value._text ? value._text : (value ?? '')}`,
  })),
  createSubHeader: vi.fn((label) => [{ text: `SUBHEADER:${label}` }]),
  generateTwoColumns: vi.fn((left, right) => ({ type: '2COL', left, right })),
  getTable: vi.fn((arr) => arr || []),
  getValue: vi.fn((val) => (val && val._text ? val._text : '')),
  hasValue: vi.fn((val) => !!(val && val._text)),
}));
vi.mock('../../../shared/generators/common/functions', () => ({
  getRolaString: vi.fn((rola) => (rola && rola._text ? 'SPRZEDAWCA' : '')),
}));
vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn((adres, label) => ({ adr: label })),
}));
vi.mock('./PodmiotDaneIdentyfikacyjne', () => ({
  generateDaneIdentyfikacyjne: vi.fn(() => [{ id: 'ID' }]),
}));
vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn(() => ({ contact: 'KONTAKT' })),
}));

describe('generateDaneIdentyfikacyjneTPodmiot3Dto', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty array for undefined input', () => {
    expect(generateDaneIdentyfikacyjneTPodmiot3Dto(undefined, 0)).toEqual([]);
  });

  it('creates header and main fields for full identification', () => {
    const podmiot2KDto: Podmiot3Podmiot2KDto = {
      fakturaPodmiotNDto: {
        NrEORI: { _text: 'EXX' },
        Rola: { _text: 'SPRZEDAWCA' },
        OpisRoli: { _text: 'rola inna' },
        Udzial: { _text: '51%' },
        Email: { _text: 'a@b.pl' },
        NrKlienta: { _text: 'KLIENTX' },
        DaneIdentyfikacyjne: { NrID: { _text: 'TID' } },
        Adres: { AdresPol: { Miasto: { _text: 'Warsaw' } } },
        AdresKoresp: { AdresPol: { Miasto: { _text: 'Gdansk' } } },
      },
      podmiot2KDto: {
        DaneIdentyfikacyjne: { NrID: { _text: 'IDK' } },
        Adres: { AdresZagr: { Kraj: { _text: 'PL' } } },
      },
    };
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(podmiot2KDto, 1);

    expect(result).toEqual(
      expect.arrayContaining([
        { text: 'HEADER:Podmiot inny 2' },
        { text: 'SUBHEADER:Dane identyfikacyjne' },
        { text: 'LABEL:Numer EORI: EXX' },
        { text: 'LABEL:Rola: SPRZEDAWCA' },
        { text: 'LABEL:Rola inna: rola inna' },
        { text: 'LABEL:Udział: 51%' },
        { contact: 'KONTAKT' },
        { text: 'LABEL:Numer klienta: KLIENTX' },
      ])
    );
  });

  it('renders Adres korespondencyjny last in right column', () => {
    const podmiot2KDto: Podmiot3Podmiot2KDto = {
      fakturaPodmiotNDto: {
        AdresKoresp: { AdresPol: { Miasto: { _text: 'Gdynia' } } },
      },
      podmiot2KDto: {},
    };
    const result: Content[] = generateDaneIdentyfikacyjneTPodmiot3Dto(podmiot2KDto, 2);
    const twoCol: any = result.find((r: any): boolean => r.type === '2COL');

    expect(twoCol.right[twoCol.right.length - 1]).toEqual({ adr: 'Adres korespondencyjny' });
  });
});
