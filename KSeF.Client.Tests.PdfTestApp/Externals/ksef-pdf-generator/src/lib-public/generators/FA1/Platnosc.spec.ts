import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePlatnosc } from './Platnosc';
import { Platnosc } from '../../types/fa1.types';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label: string, _pad?: any) => [{ text: `HEADER:${label}` }]),
  createLabelText: vi.fn((label: string, value: any) => ({
    text: `LABEL:${label}${typeof value === 'object' && value?._text ? value._text : value}`,
  })),
  generateLine: vi.fn(() => ({ text: 'LINE' })),
  generateTwoColumns: vi.fn((left, right, pad?) => ({ left, right, pad, type: '2COL' })),
  getContentTable: vi.fn((_header, data, _width) => ({
    content: Array.isArray(data) && data.length ? { data } : undefined,
  })),
  getTable: vi.fn((data) => data || []),
  hasValue: vi.fn((fp: any) => Boolean(fp && fp._text && fp._text !== '')),
}));
vi.mock('../../../shared/generators/common/functions', () => ({
  getFormaPlatnosciString: vi.fn((fp: any) => (fp?._text ? 'Przelew' : '')),
}));
vi.mock('./RachunekBankowy', () => ({
  generujRachunekBankowy: vi.fn((table, label) => [{ text: `ACCOUNT:${label}` }]),
}));

describe('generatePlatnosc', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty array when no payment info given', () => {
    expect(generatePlatnosc(undefined)).toEqual([]);
  });

  it('handles case: Platnosc.Zaplacono = 1', () => {
    const p: Platnosc = { Zaplacono: { _text: '1' }, DataZaplaty: { _text: '2025-10-10' } };
    const result = generatePlatnosc(p);

    expect(result).toEqual(
      expect.arrayContaining([
        { text: 'LINE' },
        { text: 'HEADER:Płatność' },
        { text: 'LABEL:Informacja o płatności: Zapłacono' },
        { text: 'LABEL:Data zapłaty: 2025-10-10' },
      ])
    );
  });

  it('handles case: Platnosc.ZaplataCzesciowa = 1', () => {
    const p: Platnosc = { ZaplataCzesciowa: { _text: '1' } };
    const result = generatePlatnosc(p);

    expect(result).toEqual(
      expect.arrayContaining([{ text: 'LABEL:Informacja o płatności: Zapłata częściowa' }])
    );
  });

  it('handles case: not paid', () => {
    const p: Platnosc = {};
    const result = generatePlatnosc(p);

    expect(result).toEqual(expect.arrayContaining([{ text: 'LABEL:Informacja o płatności: Brak zapłaty' }]));
  });

  it('adds "Forma płatności" with getFormaPlatnosciString when present', () => {
    const p: Platnosc = { FormaPlatnosci: { _text: '1' } };
    const result = generatePlatnosc(p);

    expect(result).toEqual(expect.arrayContaining([{ text: 'LABEL:Forma płatności: Przelew' }]));
  });

  it('falls back on "Brak zapłaty" and inna/opis if FormaPlatnosci undefined but OpisPlatnosci present', () => {
    const p: Platnosc = { OpisPlatnosci: { _text: 'Gotówka przy odbiorze' } };
    const result = generatePlatnosc(p);

    expect(result).toEqual(
      expect.arrayContaining([
        { text: 'LABEL:Informacja o płatności: Brak zapłaty' },
        { text: 'LABEL:Forma płatności: Płatność inna' },
        { text: 'LABEL:Opis płatności innej: Gotówka przy odbiorze' },
      ])
    );
  });

  it('renders tables for partials and terms if present', () => {
    const p: Platnosc = {
      PlatnosciCzesciowe: [
        { DataZaplatyCzesciowej: { _text: '2025-01-01' }, KwotaZaplatyCzesciowej: { _text: '100' } },
      ],
      TerminyPlatnosci: [
        { TerminPlatnosci: { _text: '2025-02-01' }, TerminPlatnosciOpis: { _text: 'Do miesiąca' } },
      ],
    };
    const result: any = generatePlatnosc(p);

    expect(result.some((el: any) => el.type === '2COL')).toBe(true);
  });

  it('renders Skonto info if present', () => {
    const p: Platnosc = { Skonto: { WarunkiSkonta: { _text: '30 dni' }, WysokoscSkonta: { _text: '5%' } } };
    const result = generatePlatnosc(p);

    expect(result).toEqual(
      expect.arrayContaining([
        [{ text: 'HEADER:Skonto' }],
        { text: 'LABEL:Warunki skonta: 30 dni' },
        { text: 'LABEL:Wysokość skonta: 5%' },
      ])
    );
  });

  it('always outputs column with bank accounts', () => {
    const p: Platnosc = { RachunekBankowy: [{}], RachunekBankowyFaktora: [{}] };
    const result: any = generatePlatnosc(p);
    const accCol = result.find(
      (el: any) =>
        Array.isArray(el.left) &&
        Array.isArray(el.right) &&
        el.left[0]?.text?.startsWith('ACCOUNT:') &&
        el.right[0]?.text?.startsWith('ACCOUNT:')
    );

    expect(accCol).toBeTruthy();
  });
});
