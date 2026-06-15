import { describe, expect, it, vi } from 'vitest';
import { generateRozliczenie } from './Rozliczenie';
import { Content } from 'pdfmake/interfaces';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((...args) => ({ header: args[0], margin: args[1] })),
  createSubHeader: vi.fn((label) => ({ subHeader: label })),
  generateTwoColumns: vi.fn((left, right) => ({ columns: [left, right] })),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
  getTable: vi.fn((rows) => rows ?? []),
  getContentTable: vi.fn((header, rows) => ({ content: rows })),
  createLabelText: vi.fn((label, value) => ({ label, value })),
  createLabelTextArray: vi.fn((arr) => arr),
  createSection: vi.fn((result) => result),
}));

describe('generateRozliczenie', (): void => {
  it('returns empty array when settlement is undefined', (): void => {
    const content: Content[] = generateRozliczenie(undefined, 'PLN');

    expect(Array.isArray(content)).toBe(true);
    expect(content.length).toBe(0);
  });

  it('generates both charges and deductions columns', (): void => {
    const rozliczenie = {
      Obciazenia: [{ Kwota: { _text: '10' }, Powod: { _text: 'Correction' } }],
      Odliczenia: [{ Kwota: { _text: '5' }, Powod: { _text: 'Advance' } }],
      SumaObciazen: { _text: '10' },
      SumaOdliczen: { _text: '5' },
    };
    const content: Content[] = generateRozliczenie(rozliczenie, 'PLN');

    expect(content.some((el: Content): boolean => typeof el === 'object' && 'columns' in el)).toBe(true);
  });

  it('generates only charges column when only charges present', (): void => {
    const rozliczenie = {
      Obciazenia: [{ Kwota: { _text: '50' }, Powod: { _text: 'Interest' } }],
      Odliczenia: [],
      SumaObciazen: { _text: '50' },
    };
    const content: Content[] = generateRozliczenie(rozliczenie, 'USD');

    expect(content.some((el: Content): boolean => typeof el === 'object' && 'columns' in el)).toBe(true);
  });

  it('generates only deductions column when only deductions present', (): void => {
    const rozliczenie = {
      Obciazenia: [],
      Odliczenia: [{ Kwota: { _text: '20' }, Powod: { _text: 'Discount' } }],
      SumaOdliczen: { _text: '20' },
    };
    const content: Content[] = generateRozliczenie(rozliczenie, 'EUR');

    expect(content.some((el: Content): boolean => typeof el === 'object' && 'columns' in el)).toBe(true);
  });

  it('includes payment amount (Do zapłaty) if provided', (): void => {
    const rozliczenie = {
      Obciazenia: [],
      Odliczenia: [],
      DoZaplaty: { _text: '100.50' },
    };
    const content: Content[] = generateRozliczenie(rozliczenie, 'PLN');
    const hasPayment: boolean = content.some(
      (el: Content) =>
        typeof el === 'object' &&
        'stack' in el &&
        Array.isArray((el as any).stack) &&
        (el as any).stack.some((st: any): boolean =>
          typeof st.value === 'object' ? st.value._text === '100.50' : String(st.value).includes('Do zapłaty')
        )
    );

    expect(hasPayment).toBe(true);
  });

  it('includes balance amount (Do rozliczenia) if provided', (): void => {
    const rozliczenie = {
      Obciazenia: [],
      Odliczenia: [],
      DoRozliczenia: { _text: '120.00' },
    };
    const content: Content[] = generateRozliczenie(rozliczenie, 'PLN');
    const hasBalance: boolean = content.some(
      (el: Content) =>
        typeof el === 'object' &&
        'stack' in el &&
        Array.isArray((el as any).stack) &&
        (el as any).stack.some((st: any): boolean =>
          typeof st.value === 'object'
            ? st.value._text === '120.00'
            : String(st.value).includes('Do rozliczenia')
        )
    );

    expect(hasBalance).toBe(true);
  });
});
