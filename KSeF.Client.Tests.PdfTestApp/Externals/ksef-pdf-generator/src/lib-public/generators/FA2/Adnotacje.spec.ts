import { beforeEach, describe, expect, it, test, vi } from 'vitest';
import { createHeader, createLabelText, formatText, getTable } from '../../../shared/PDF-functions';
import { generateAdnotacje, generateDostawy } from './Adnotacje';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string) => ({ text, style: 'header' })),
  createLabelText: vi.fn((label: string, value: string) => [{ text: label + value }]),
  formatText: vi.fn((text: string) => ({ text })),
  getTable: vi.fn(() => []),
  hasValue: vi.fn((v) => !!v?._text),
  verticalSpacing: vi.fn((n: number) => ({ text: `space-${n}` })),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));

describe(generateAdnotacje.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('zwraca pustą tablicę jeśli brak adnotacji', () => {
    const result = generateAdnotacje(undefined);

    expect(result).toEqual([]);
  });

  test.each([
    [{ Zwolnienie: { P_19: { _text: '1' } } }, true, 'powinien dodać nagłówek i sekcję "Adnotacje"'],
    [{ P_18A: { _text: '1' } }, true, 'powinien dodać "Mechanizm podzielonej płatności"'],
    [{ P_16: { _text: '1' } }, true, 'powinien dodać "Metoda kasowa"'],
    [{ P_18: { _text: '1' } }, true, 'powinien dodać "Odwrotne obciążenie"'],
    [{ P_23: { _text: '1' } }, true, 'powinien dodać "Procedura trójstronna uproszczona"'],
    [{ P_17: { _text: '1' } }, true, 'powinien dodać "Samofakturowanie"'],
  ])('dla adnotacji %s %s (%s)', (adnotacje, expected, desc) => {
    const result = generateAdnotacje(adnotacje as any);

    if (expected) {
      expect(result.length).toBeGreaterThan(0);
      expect(createHeader).toHaveBeenCalledWith('Adnotacje');
    } else {
      expect(result).toEqual([]);
    }
  });

  it('dodaje adnotacje dla NoweSrodkiTransportu z VAT-22', () => {
    const adnotacje = {
      NoweSrodkiTransportu: { P_42_5: { _text: '1' } },
    };
    const result = generateAdnotacje(adnotacje as any);

    expect(result.length).toBeGreaterThan(0);
  });

  it('dodaje procedurę marży', () => {
    const adnotacje = {
      PMarzy: { P_PMarzy: { _text: '1' }, P_PMarzy_3_1: { _text: '1' } },
    };
    const result = generateAdnotacje(adnotacje as any);

    expect(result.length).toBeGreaterThan(0);
    expect(createLabelText).toHaveBeenCalledWith('Procedura marży: ', 'towary używane');
  });
});

describe(generateDostawy.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('zwraca pustą tablicę jeśli brak danych', () => {
    (getTable as any).mockReturnValueOnce([]);
    const result = generateDostawy({} as any);

    expect(result).toEqual([]);
  });

  it('generuje tabelę jeśli są dane', () => {
    (getTable as any).mockReturnValueOnce([
      {
        P_22A: { _text: '2025-01-01' },
        P_22B: { _text: '123' },
        DetailsString: { _text: 'Test opis' },
      },
    ]);

    const result = generateDostawy({ NowySrodekTransportu: [] } as any);

    expect(result.length).toBeGreaterThan(0);
    expect(result[0]).toHaveProperty('table');
    expect(formatText).toHaveBeenCalledWith('2025-01-01');
  });

  test.each([
    ['P_22B', 'Dostawa dotyczy pojazdów lądowych, o których mowa w art. 2 pkt 10 lit. a ustawy'],
    ['P_22C', 'Dostawa dotyczy jednostek pływających, o których mowa w art. 2 pkt 10 lit. b ustawy'],
    ['P_22D', 'Dostawa dotyczy statków powietrznych, o których mowa w art. 2 pkt 10 lit. c ustawy'],
  ])('poprawnie rozpoznaje typ środka transportu (%s)', (field, expected) => {
    (getTable as any).mockReturnValueOnce([{ [field]: { _text: '1' } }]);

    const result = generateDostawy({ NowySrodekTransportu: [] } as any);
    const textOutput = JSON.stringify(result);

    expect(textOutput).toContain(expected);
  });

  it('adds all Zwolnienie fields together (P_19A, P_19B, P_19C)', () => {
    const annotations = {
      Zwolnienie: {
        P_19: { _text: '1' },
        P_19A: { _text: 'Law' },
        P_19B: { _text: 'Directive' },
        P_19C: { _text: 'OtherBasis' },
      },
    } as any;

    const result = generateAdnotacje(annotations);
    const flat = result.flatMap((r: any) => (Array.isArray(r.columns) ? r.columns.flat() : [r]))?.flat();

    expect(
      flat.flatMap((c) => (Array.isArray(c) ? c : [c])).some((item) => item?.text?.includes('Directive'))
    ).toBe(true);
    expect(flat.some((c: any) => c.text?.includes('Dostawa towarów lub świadczenie'))).toBe(true);
    expect(
      flat
        .flatMap((c) => (Array.isArray(c) ? c : [c]))
        .some((item) => item?.text?.includes('Podstawa zwolnienia od podatku'))
    ).toBe(true);
  });

  it('adds VAT-22 and calls generateDostawy', () => {
    const annotations = {
      NoweSrodkiTransportu: { P_42_5: { _text: '1' }, NowySrodekTransportu: [] },
    } as any;

    const result = generateAdnotacje(annotations);

    expect(result.some((r: any) => r?.table || r?.text?.includes('space'))).toBe(true);
  });

  it('adds full Marża procedure with P_PMarzy_3_2', () => {
    const annotations = {
      PMarzy: { P_PMarzy: { _text: '1' }, P_PMarzy_3_2: { _text: '1' } },
    } as any;

    const result = generateAdnotacje(annotations);

    expect(result.flat().some((c: any) => c?.text?.includes('Adnotacje'))).toBe(true);
  });

  it('generates all transport type combinations', () => {
    (getTable as any).mockReturnValueOnce([
      {
        P_22A: { _text: '2025-01-01' },
        P_22B: { _text: '1' },
        P_22B1: { _text: 'VIN' },
        P_22B2: { _text: 'Chassis' },
        P_22B3: { _text: 'Underbody' },
        P_22B4: { _text: 'Frame' },
        P_22C: { _text: 'C' },
        P_22C1: { _text: 'Hull' },
        P_22D: { _text: 'D' },
        P_22D1: { _text: 'Serial' },
        DetailsString: { _text: 'Description' },
        P_22BT: { _text: 'BT' },
      },
    ]);

    const result = generateDostawy({ NowySrodekTransportu: [] } as any);
    const textOutput = JSON.stringify(result);

    expect(textOutput).toContain('VIN');
    expect(textOutput).toContain('Chassis');
    expect(textOutput).toContain('Underbody');
    expect(textOutput).toContain('Frame');
    expect(textOutput).toContain('Hull');
    expect(textOutput).toContain('Serial');
    expect(textOutput).toContain('Description');
    expect(textOutput).toContain('BT');
  });

  it('generates only header when no details exist', () => {
    (getTable as any).mockReturnValueOnce([{ P_22A: { _text: '2025-01-01' } }]);
    const result: any = generateDostawy({ NowySrodekTransportu: [] } as any);

    expect(result[0].table.body[0][0].text).toBe('Data dopuszczenia do użytku');
  });
});
