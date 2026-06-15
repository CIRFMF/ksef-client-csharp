import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateAdnotacje } from './Adnotacje';
import { Adnotacje } from '../../types/fa1.types';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn().mockImplementation((label) => ({ text: `HEADER:${label}` })),
  createLabelText: vi.fn().mockImplementation((label, value) => [{ text: `LABEL:${label}${value}` }]),
  formatText: vi.fn().mockImplementation((text) => ({ text })),
  hasValue: vi.fn((val) => Boolean(val && val._text)),
  verticalSpacing: vi.fn().mockImplementation((size) => ({ text: `SPACING:${size}` })),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));

describe(generateAdnotacje.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns an empty array when argument is undefined', () => {
    const result = generateAdnotacje(undefined);

    expect(result).toEqual([]);
  });

  it('adds annotation P_19 and also P_19A, P_19B, P_19C', () => {
    const adnotacje: Adnotacje = {
      P_19: { _text: '1' },
      P_19A: { _text: 'tekst19A' },
      P_19B: { _text: 'tekst19B' },
      P_19C: { _text: 'tekst19C' },
    };
    const result = generateAdnotacje(adnotacje);

    expect(result.some((el) => JSON.stringify(el).includes('HEADER:Adnotacje'))).toBeTruthy();
    expect(
      result.some((el) =>
        JSON.stringify(el).includes('LABEL:Przepis ustawy albo aktu wydanego na podstawie ustawy: tekst19A')
      )
    ).toBeTruthy();
    expect(
      result.some((el) => JSON.stringify(el).includes('LABEL:Przepis dyrektywy: tekst19B'))
    ).toBeTruthy();
    expect(
      result.some((el) => JSON.stringify(el).includes('LABEL:Inna podstawa prawna: tekst19C'))
    ).toBeTruthy();
  });

  it('adds "Split payment mechanism" annotation when P_18A = 1', () => {
    const adnotacje: Adnotacje = { P_18A: { _text: '1' } };
    const result = generateAdnotacje(adnotacje);

    expect(result.some((el) => JSON.stringify(el).includes('Mechanizm podzielonej płatności'))).toBeTruthy();
  });

  it('adds correct texts for cash method/reverse charge/simplified procedure', () => {
    const adnotacje: Adnotacje = {
      P_16: { _text: '1' },
      P_18: { _text: '1' },
      P_23: { _text: '1' },
    };
    const result = generateAdnotacje(adnotacje);

    expect(JSON.stringify(result)).toContain('Metoda kasowa');
    expect(JSON.stringify(result)).toContain('Odwrotne obciążenie');
    expect(JSON.stringify(result)).toContain('Procedura trójstronna uproszczona');
  });

  it('adds "Self-billing" when P_17=1', () => {
    const adnotacje: Adnotacje = { P_17: { _text: '1' } };
    const result = generateAdnotacje(adnotacje);

    expect(JSON.stringify(result)).toContain('Samofakturowanie');
  });

  it('adds margin annotation based on subtypes', () => {
    const adnotacje: Adnotacje = {
      P_PMarzy: { _text: '1' },
      P_PMarzy_3_2: { _text: '1' }, // works of art
    };
    const result = generateAdnotacje(adnotacje);

    expect(JSON.stringify(result)).toContain('dzieła sztuki');
  });
});

describe('generateAdnotacje - additional coverage', () => {
  it('adds correct margin annotation for all subtypes', () => {
    const variants = [
      { key: 'P_PMarzy_3_1', expected: 'towary używane' },
      { key: 'P_PMarzy_2', expected: 'biura podróży' },
      { key: 'P_PMarzy_3_3', expected: 'przedmioty kolekcjonerskie i antyki' },
    ];

    variants.forEach(({ key, expected }) => {
      const adnotacje: any = {
        P_PMarzy: { _text: '1' },
        [key]: { _text: '1' },
      };
      const result = generateAdnotacje(adnotacje);
      expect(JSON.stringify(result)).toContain(expected);
    });
  });

  it('handles P_22 and calls generateDostawy', () => {
    const adnotacje: any = { P_22: { _text: '1' } };
    const result = generateAdnotacje(adnotacje);
    expect(JSON.stringify(result)).toContain('Wewnątrzwspólnotowe dostawy nowych środków transportu');
  });

  it('returns empty array if adnotacje has no valid fields', () => {
    const adnotacje: any = { P_99: { _text: '' } };
    const result = generateAdnotacje(adnotacje);
    expect(result).toEqual([]);
  });

  it('adds header and spacing when result has items', () => {
    const adnotacje: any = { P_16: { _text: '1' } };
    const result = generateAdnotacje(adnotacje) as any;
    const resultString = JSON.stringify(result);
    expect(resultString).toContain('HEADER:Adnotacje');
    expect(resultString).toContain('SPACING:1');
  });
});
