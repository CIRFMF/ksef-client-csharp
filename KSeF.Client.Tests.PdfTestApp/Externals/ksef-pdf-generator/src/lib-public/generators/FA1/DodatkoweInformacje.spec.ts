import { beforeEach, describe, expect, it, vi } from 'vitest';
import { DodatkowyOpi, Fa } from '../../types/fa1.types';
import { generateDodatkoweInformacje } from './DodatkoweInformacje';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: () => [{ text: 'HEADER:Dodatkowe informacje' }],
  createSubHeader: () => [{ text: 'SUBHEADER:Dodatkowy opis' }],
  formatText: (text: string) => ({ text }),
  createSection: (content: any, _: boolean) => content,
  getValue: (val: any) => val?._text || '',
  getTable: (data: any) => data,
  getContentTable: () => ({ content: { table: 'FAKE_TABLE' } }),
}));

describe('generateDodatkoweInformacje', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns an empty array when no triggers present', () => {
    const faVat: Fa = {};
    const result = generateDodatkoweInformacje(faVat);

    expect(result).toEqual([]);
  });

  it('returns correct table when DodatkowyOpis provided', () => {
    const faVat: Fa = {
      DodatkowyOpis: [
        { Klucz: { _text: 'RODZAJ' }, Wartosc: { _text: 'TRESC' } },
        { Klucz: { _text: 'INNY' }, Wartosc: { _text: 'OPIS' } },
      ] as DodatkowyOpi[],
    };
    const result = generateDodatkoweInformacje(faVat);

    expect(result).toContainEqual({ text: 'HEADER:Dodatkowe informacje' });
    expect(result).toContainEqual({ text: 'SUBHEADER:Dodatkowy opis' });
    expect(result).toContainEqual({ table: 'FAKE_TABLE' });
  });

  it('includes section when any element is present', () => {
    const faVat: Fa = { TP: { _text: '1' }, ZwrotAkcyzy: { _text: '1' } };
    const result = generateDodatkoweInformacje(faVat);

    expect(result.length).toBeGreaterThan(0);
  });
});
