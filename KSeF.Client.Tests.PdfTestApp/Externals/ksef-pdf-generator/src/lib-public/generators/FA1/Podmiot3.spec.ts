import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Podmiot3 } from '../../types/fa1.types';
import { generatePodmiot3 } from './Podmiot3';
import { Content } from 'pdfmake/interfaces';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label) => [{ text: `HEADER:${label}` }]),
  createLabelText: vi.fn((label, value) => ({
    text: `LABEL:${label}${value && value._text ? value._text : (value ?? '')}`,
  })),
  createSection: vi.fn((arr) => arr),
  formatText: vi.fn((txt, _args) => ({ text: `FMT:${txt}` })),
  generateTwoColumns: vi.fn((left, right) => ({ type: '2COL', left, right })),
  getTable: vi.fn((arr) => arr || []),
  getValue: vi.fn((val) => (val && val._text ? val._text : '')),
  hasValue: vi.fn((val) => !!(val && val._text)),
}));
vi.mock('../../../shared/generators/common/functions', () => ({
  getRolaString: vi.fn((rola, idx) => (rola && rola._text ? 'SPRZEDAWCA' : '')),
}));
vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn((adres, label) => ({ adr: label })),
}));
vi.mock('./PodmiotDaneIdentyfikacyjne', () => ({
  generateDaneIdentyfikacyjne: vi.fn(() => [{ id: 'ID' }]),
}));
vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn(() => [{ contact: 'KONTAKT' }]),
}));

describe('generatePodmiot3', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders minimal column structure', () => {
    const podmiot: Podmiot3 = { NrEORI: { _text: '999' }, Rola: { _text: 'X' } };
    const result: Content[] = generatePodmiot3(podmiot, 0);
    const last: any = result[result.length - 1] || {};

    expect(last.type).toBe('2COL');
    expect(last.left).toEqual(
      expect.arrayContaining([
        { text: 'HEADER:Podmiot inny 1' },
        { text: 'LABEL:Numer EORI: 999' },
        [{ text: 'LABEL:Rola: SPRZEDAWCA' }, { text: 'LABEL:Rola inna: ' }, { text: 'LABEL:Udział: ' }],
      ])
    );
  });

  it('renders column1 with all tax ID and role info', () => {
    const podmiot: Podmiot3 = {
      NrEORI: { _text: '1000' },
      DaneIdentyfikacyjne: { NrID: { _text: 'TAXX' }, BrakID: { _text: '1' } },
      Rola: { _text: 'S' },
      OpisRoli: { _text: 'operator' },
      Udzial: { _text: '50%' },
    };
    const result: any = generatePodmiot3(podmiot, 1);
    const col1: Content = result[result.length - 1].left;

    expect(col1).toEqual(
      expect.arrayContaining([
        { text: 'HEADER:Podmiot inny 2' },
        { text: 'LABEL:Numer EORI: 1000' },
        { text: 'LABEL:Identyfikator podatkowy inny: TAXX' },
        { text: 'LABEL:Brak identyfikatora  ' },
        { id: 'ID' },
        [
          { text: 'LABEL:Rola: SPRZEDAWCA' },
          { text: 'LABEL:Rola inna: operator' },
          { text: 'LABEL:Udział: 50%' },
        ],
      ])
    );
  });

  it('adds Numer klienta in right column if present', () => {
    const podmiot: Podmiot3 = {
      NrEORI: { _text: 'foo' },
      Rola: { _text: 'ROLA' },
      NrKlienta: { _text: 'KLIENTX' },
    };
    const result: any = generatePodmiot3(podmiot, 2);
    const col2: Content = result[result.length - 1].right;

    expect(col2).toEqual(expect.arrayContaining([{ text: 'LABEL:Numer klienta: KLIENTX' }]));
  });

  it('always returns results wrapped in sections', () => {
    const podmiot: Podmiot3 = { NrEORI: { _text: 'A' }, Rola: { _text: 'B' } };
    const result: any = generatePodmiot3(podmiot, 1);

    expect(Array.isArray(result)).toBe(true);
    expect(result.some((r: any) => r.type === '2COL')).toBeTruthy();
  });
});
