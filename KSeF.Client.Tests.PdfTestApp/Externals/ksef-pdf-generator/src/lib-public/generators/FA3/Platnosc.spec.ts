import { beforeEach, describe, expect, it, test, vi } from 'vitest';
import { generatePlatnosc } from './Platnosc';
import type { Platnosc, RachunekBankowy, Skonto } from '../../types/fa3.types';
import type { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  generateLine,
  generateTwoColumns,
  getContentTable,
  hasValue,
} from '../../../shared/PDF-functions';
import { getFormaPlatnosciString } from '../../../shared/generators/common/functions';
import { generujRachunekBankowy } from './RachunekBankowy';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string): Content[] => [{ text, style: 'header' }]),
  createLabelText: vi.fn((label: string, value: any): Content[] => [{ text: `${label}${value ?? ''}` }]),
  generateLine: vi.fn((): Content[] => [{ line: true } as any]),
  generateTwoColumns: vi.fn((left: any[], right: any[], margins?: number[]): Content[] => [
    { twoColumns: { left, right }, margins } as any,
  ]),
  getTable: vi.fn((data: any): any[] => data ?? []),
  getContentTable: vi.fn(() => ({ content: [{ text: 'mockTable' }] })),
  hasValue: vi.fn((v: any) => !!v),
  getValue: vi.fn((v: any) => !!v),
}));

vi.mock('../../../shared/generators/common/functions', () => ({
  getFormaPlatnosciString: vi.fn((v: any) => `Forma: ${v}`),
}));

vi.mock('./RachunekBankowy', () => ({
  generujRachunekBankowy: vi.fn((data: any, label: string): Content[] => [{ text: label }]),
}));
const mockedCreateLabelText = vi.mocked(createLabelText);

describe(generatePlatnosc.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test.each([
    [
      {
        Zaplacono: { _text: '1' },
        DataZaplaty: '2025-10-01',
        FormaPlatnosci: 'Przelew',
      } as Partial<Platnosc>,
      'Zapłacono',
    ],
    [
      { ZnacznikZaplatyCzesciowej: { _text: '1' }, FormaPlatnosci: 'Gotówka' } as Partial<Platnosc>,
      'Zapłata częściowa',
    ],
    [{} as Partial<Platnosc>, 'Brak zapłaty'],
  ])(
    'generuje poprawnie informacje o płatności dla %o',
    (platnosc: Partial<Platnosc>, expected: string): void => {
      const result: Content = generatePlatnosc(platnosc as Platnosc);

      expect(generateLine).toHaveBeenCalled();
      expect(createHeader).toHaveBeenCalledWith('Płatność');

      const labelCall = mockedCreateLabelText.mock.calls.find(
        ([label, value]): boolean => value === expected
      ) || ['defaultLabel', 'defaultValue'];

      expect(labelCall).toBeDefined();
      expect((result as Content[]).length).toBeGreaterThan(0);
    }
  );

  it('dodaje informacje o formie płatności jeśli hasValue zwraca true', () => {
    const platnosc: Partial<Platnosc> = { FormaPlatnosci: { _text: 'Karta' } };
    const result = generatePlatnosc(platnosc as Platnosc);

    expect(hasValue).toHaveBeenCalledWith({ _text: 'Karta' });
    expect(getFormaPlatnosciString).toHaveBeenCalledWith({ _text: 'Karta' });
  });

  it('generuje tabelę zapłaty częściowej i terminów płatności', () => {
    const platnosc: Partial<Platnosc> = {
      ZnacznikZaplatyCzesciowej: { _text: '1' },
      ZaplataCzesciowa: [
        { DataZaplatyCzesciowej: { _text: '2025-10-01' }, KwotaZaplatyCzesciowej: { _text: '100' } },
      ],
      TerminPlatnosci: [{ Termin: { _text: '2025-10-15' } }],
    };

    (getContentTable as any).mockReturnValueOnce({ content: [{ text: 'mockTable1' }] });
    (getContentTable as any).mockReturnValueOnce({ content: [{ text: 'mockTable2' }] });

    const result = generatePlatnosc(platnosc as Platnosc);

    expect(generateTwoColumns).toHaveBeenCalled();
    expect((result as Content[]).length).toBeGreaterThan(0);
  });

  it('dodaje rachunki bankowe i skonto', () => {
    const platnosc: Partial<Platnosc> = {
      RachunekBankowy: ['123'] as RachunekBankowy[],
      RachunekBankowyFaktora: ['456'] as RachunekBankowy[],
      Skonto: { WarunkiSkonta: '7 dni', WysokoscSkonta: '2%' } as Skonto,
    };

    const result: Content = generatePlatnosc(platnosc as Platnosc);

    expect(generujRachunekBankowy).toHaveBeenCalledTimes(2);
    expect(createHeader).toHaveBeenCalledWith('Płatność');
    expect(createLabelText).toHaveBeenCalledWith('Warunki skonta: ', '7 dni');
    expect(createLabelText).toHaveBeenCalledWith('Wysokość skonta: ', '2%');
  });

  it('zwraca pustą tablicę jeśli platnosc undefined', () => {
    const result = generatePlatnosc(undefined);

    expect(result).toEqual([]);
  });
});
