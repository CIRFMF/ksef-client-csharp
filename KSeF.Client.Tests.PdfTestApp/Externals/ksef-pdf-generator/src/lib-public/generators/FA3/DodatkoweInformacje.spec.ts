import { describe, it, expect, vi, beforeEach, test } from 'vitest';
import { generateDodatkoweInformacje } from './DodatkoweInformacje';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string) => [{ text, style: 'header' }]),
  createSection: vi.fn((content: any, bordered: boolean) => [{ section: content, bordered }]),
  createSubHeader: vi.fn((text: string) => [{ text, style: 'subheader' }]),
  formatText: vi.fn((text: string) => ({ text, style: 'value' })),
  getValue: vi.fn((obj: any) => obj?._text ?? obj ?? ''),
  getTable: vi.fn((data: any) => data ?? []),
  getContentTable: vi.fn(() => ({ content: { text: 'mockTable' } })),
}));

import {
  createSection,
  createSubHeader,
  formatText,
  getTable,
  getContentTable,
} from '../../../shared/PDF-functions';

describe(generateDodatkoweInformacje.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test.each([
    [{ TP: { _text: '1' } }, true],
    [{ ZwrotAkcyzy: { _text: '1' } }, true],
    [{ TP: { _text: '0' }, ZwrotAkcyzy: { _text: '0' } }, false],
  ])('dla danych %o', (faVat, shouldHaveContent) => {
    const result = generateDodatkoweInformacje(faVat as any);
    if (shouldHaveContent) {
      expect(result.length).toBeGreaterThan(0);
    } else {
      expect(result).toEqual([]);
    }
  });

  it('dodaje dodatkowy opis jeśli istnieje DodatkowyOpis', () => {
    (getTable as any).mockReturnValueOnce([
      { NrWiersza: { _text: '1' }, Klucz: { _text: 'Info' }, Wartosc: { _text: 'Opis' } },
    ]);

    const faVat = {
      TP: { _text: '1' },
      DodatkowyOpis: [{ NrWiersza: { _text: '1' }, Klucz: { _text: 'Info' }, Wartosc: { _text: 'Opis' } }],
    };

    const result = generateDodatkoweInformacje(faVat as any);

    expect(createSubHeader).toHaveBeenCalledWith('Dodatkowy opis');
    expect(getContentTable).toHaveBeenCalled();
    expect(result.length).toBeGreaterThan(0);
    expect(createSection).toHaveBeenCalled();
  });

  it('zwraca pustą tablicę gdy brak danych wejściowych', () => {
    const result = generateDodatkoweInformacje({} as any);
    expect(result).toEqual([]);
  });

  it('poprawnie dodaje sekcję TP i ZwrotAkcyzy razem', () => {
    const faVat = {
      TP: { _text: '1' },
      ZwrotAkcyzy: { _text: '1' },
    };
    const result = generateDodatkoweInformacje(faVat as any);

    expect(formatText).toHaveBeenCalledWith(
      '- Istniejące powiązania między nabywcą a dokonującym dostawy towarów lub usługodawcą'
    );
    expect(formatText).toHaveBeenCalledWith(
      '- Informacja dodatkowa związana ze zwrotem podatku akcyzowego zawartego w cenie oleju napędowego'
    );
    expect(createSection).toHaveBeenCalled();
    expect(result.length).toBeGreaterThan(0);
  });

    it('poprawnie dodaje sekcję FP', () => {
    const faVat = {
      FP: { _text: '1' },
    };
    const result = generateDodatkoweInformacje(faVat as any);

    expect(formatText).toHaveBeenCalledWith(
      '- Faktura, o której mowa w art. 109 ust. 3d ustawy'
    );
    expect(createSection).toHaveBeenCalled();
    expect(result.length).toBeGreaterThan(0);
  });
});
