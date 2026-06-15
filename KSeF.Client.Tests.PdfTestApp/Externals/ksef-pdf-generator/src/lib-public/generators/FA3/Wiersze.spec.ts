import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { TRodzajFaktury } from '../../../shared/consts/const';
import { Fa } from '../../types/fa3.types';
import { generateWiersze } from './Wiersze';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createLabelTextArray: vi.fn(),
  createSection: vi.fn(),
  formatText: vi.fn(),
  getContentTable: vi.fn(),
  getTable: vi.fn(),
  getValue: vi.fn(),
}));

describe(generateWiersze.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockFaVat: Fa = {
    FaWiersz: [
      {
        NrWierszaFa: { _text: '1' },
        P_7: { _text: 'Product 1' },
        P_9A: { _text: '100' },
        P_8B: { _text: '2' },
      },
    ],
    KodWaluty: { _text: 'PLN' },
    P_15: { _text: '200' },
    RodzajFaktury: { _text: TRodzajFaktury.VAT },
  } as any;

  const setupBasicMocks = (p15Value: string, rodzajFakturyValue: string, currencyValue: string = 'PLN') => {
    vi.mocked(PDFFunctions.getTable).mockReturnValue([
      {
        NrWierszaFa: { _text: '1' },
        P_7: { _text: 'Product 1' },
        P_9A: { _text: '100' },
      },
    ] as any);

    vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
      content: { table: {} } as any,
      fieldsWithValue: ['P_11', 'P_7'],
    });

    vi.mocked(PDFFunctions.getValue).mockImplementation((field: any) => {
      if (field === mockFaVat.P_15 || field?._text === p15Value) return p15Value;
      if (field === mockFaVat.RodzajFaktury || field?._text === rodzajFakturyValue) return rodzajFakturyValue;
      if (field === mockFaVat.KodWaluty || field?._text === currencyValue) return currencyValue;
      return undefined;
    });

    vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted text' as any);
    vi.mocked(PDFFunctions.createHeader).mockReturnValue(['Header'] as any);
    vi.mocked(PDFFunctions.createSection).mockReturnValue({ section: 'content' } as any);
    vi.mocked(PDFFunctions.createLabelTextArray).mockReturnValue(['Label', 'Value'] as any);
  };

  describe('when no invoice lines exist', () => {
    it('should return empty array when table is empty', () => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: [],
      });
      vi.mocked(PDFFunctions.getValue).mockReturnValue('0');

      const result = generateWiersze(mockFaVat);

      expect(result).toEqual([]);
    });
  });

  describe('when invoice lines exist', () => {
    it('should call getTable with FaWiersz', () => {
      setupBasicMocks('200', TRodzajFaktury.VAT);

      generateWiersze(mockFaVat);

      expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockFaVat.FaWiersz);
    });

    it('should call createHeader with "Pozycje"', () => {
      setupBasicMocks('200', TRodzajFaktury.VAT);

      generateWiersze(mockFaVat);

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Pozycje');
    });

    describe('price text formatting', () => {
      it('should display "netto" when P_11 is in fieldsWithValue', () => {
        setupBasicMocks('200', TRodzajFaktury.VAT);

        generateWiersze(mockFaVat);

        expect(PDFFunctions.formatText).toHaveBeenCalledWith(expect.stringContaining('netto'), [
          FormatTyp.Label,
          FormatTyp.MarginBottom8,
        ]);
      });

      it('should display "brutto" when P_11 is not in fieldsWithValue', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([{ NrWierszaFa: { _text: '1' } }] as any);

        vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
          content: { table: {} } as any,
          fieldsWithValue: ['P_11A', 'P_7'],
        });

        vi.mocked(PDFFunctions.getValue).mockReturnValue('0');
        vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted text' as any);
        vi.mocked(PDFFunctions.createHeader).mockReturnValue(['Header'] as any);
        vi.mocked(PDFFunctions.createSection).mockReturnValue({ section: 'content' } as any);

        generateWiersze(mockFaVat);

        expect(PDFFunctions.formatText).toHaveBeenCalledWith(expect.stringContaining('brutto'), [
          FormatTyp.Label,
          FormatTyp.MarginBottom8,
        ]);
      });

      it('should include currency code in price text', () => {
        setupBasicMocks('200', TRodzajFaktury.VAT);

        generateWiersze(mockFaVat);

        expect(PDFFunctions.formatText).toHaveBeenCalledWith(expect.stringContaining('PLN'), [
          FormatTyp.Label,
          FormatTyp.MarginBottom8,
        ]);
      });
    });

    describe('table generation', () => {
      it('should generate single table when fieldsWithValue.length <= 8', () => {
        setupBasicMocks('200', TRodzajFaktury.VAT);

        generateWiersze(mockFaVat);

        expect(PDFFunctions.getContentTable).toHaveBeenCalledTimes(1);
      });

      it('should generate two tables when fieldsWithValue.length > 8', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([{ NrWierszaFa: { _text: '1' } }] as any);

        vi.mocked(PDFFunctions.getContentTable)
          .mockReturnValueOnce({
            content: null,
            fieldsWithValue: Array(9).fill('field'),
          })
          .mockReturnValueOnce({
            content: { table: 'table1' } as any,
            fieldsWithValue: ['field1', 'field2'],
          })
          .mockReturnValueOnce({
            content: { table: 'table2' } as any,
            fieldsWithValue: ['field3', 'field4'],
          });

        vi.mocked(PDFFunctions.getValue).mockReturnValue('0');
        vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted text' as any);
        vi.mocked(PDFFunctions.createHeader).mockReturnValue(['Header'] as any);
        vi.mocked(PDFFunctions.createSection).mockReturnValue({ section: 'content' } as any);

        generateWiersze(mockFaVat);

        expect(PDFFunctions.getContentTable).toHaveBeenCalledTimes(3);
      });

      it('should not add second table if it has only 1 field with value', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([{ NrWierszaFa: { _text: '1' } }] as any);

        vi.mocked(PDFFunctions.getContentTable)
          .mockReturnValueOnce({
            content: null,
            fieldsWithValue: Array(9).fill('field'),
          })
          .mockReturnValueOnce({
            content: { table: 'table1' } as any,
            fieldsWithValue: ['field1', 'field2'],
          })
          .mockReturnValueOnce({
            content: { table: 'table2' } as any,
            fieldsWithValue: ['field1'],
          });

        vi.mocked(PDFFunctions.getValue).mockReturnValue('0');
        vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted text' as any);
        vi.mocked(PDFFunctions.createHeader).mockReturnValue(['Header'] as any);
        vi.mocked(PDFFunctions.createSection).mockReturnValue({ section: 'content' } as any);

        generateWiersze(mockFaVat);

        const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0];
        expect(sectionCall).not.toContain('\n');
        expect(sectionCall.filter((item: any) => item?.table === 'table2')).toHaveLength(0);
      });
    });

    describe('payment amount description', () => {
      it('should add "Kwota pozostała do zapłaty" for ROZ invoice type when P_15 > 0', () => {
        setupBasicMocks('150', TRodzajFaktury.ROZ, 'EUR');

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith([
          { value: 'Kwota pozostała do zapłaty: ', formatTyp: FormatTyp.LabelGreater },
          {
            value: '150',
            formatTyp: FormatTyp.CurrencyGreater,
            currency: 'EUR',
          },
        ]);
      });

      it('should not add description for ROZ invoice when P_15 = 0', () => {
        setupBasicMocks('0', TRodzajFaktury.ROZ, 'EUR');
        vi.mocked(PDFFunctions.createLabelTextArray).mockClear();

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createLabelTextArray).not.toHaveBeenCalled();
      });

      it('should add "Kwota należności ogółem" for VAT invoice when P_15 > 0', () => {
        setupBasicMocks('200', TRodzajFaktury.VAT, 'PLN');

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith([
          { value: 'Kwota należności ogółem: ', formatTyp: FormatTyp.LabelGreater },
          {
            value: '200',
            formatTyp: [FormatTyp.CurrencyGreater, FormatTyp.HeaderContent, FormatTyp.Value],
            currency: 'PLN',
          },
        ]);
      });

      it('should add description for KOR invoice when P_15 > 0', () => {
        setupBasicMocks('300', TRodzajFaktury.KOR, 'USD');

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith([
          { value: 'Kwota należności ogółem: ', formatTyp: FormatTyp.LabelGreater },
          {
            value: '300',
            formatTyp: [FormatTyp.CurrencyGreater, FormatTyp.HeaderContent, FormatTyp.Value],
            currency: 'USD',
          },
        ]);
      });

      it('should add description for KOR_ROZ invoice when P_15 > 0', () => {
        setupBasicMocks('250', TRodzajFaktury.KOR_ROZ, 'PLN');

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalled();
      });

      it('should add description for UPR invoice when P_15 > 0', () => {
        setupBasicMocks('180', TRodzajFaktury.UPR, 'PLN');

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalled();
      });

      it('should not add description for VAT invoice when P_15 = 0', () => {
        setupBasicMocks('0', TRodzajFaktury.VAT, 'PLN');
        vi.mocked(PDFFunctions.createLabelTextArray).mockClear();

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createLabelTextArray).not.toHaveBeenCalled();
      });

      it('should use empty string for currency if KodWaluty is undefined', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([{ NrWierszaFa: { _text: '1' } }] as any);

        vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
          content: { table: {} } as any,
          fieldsWithValue: ['P_11', 'P_7'],
        });

        vi.mocked(PDFFunctions.getValue).mockImplementation((field: any) => {
          if (field === mockFaVat.P_15) return '200';
          if (field === mockFaVat.RodzajFaktury) return TRodzajFaktury.VAT;
          if (field === mockFaVat.KodWaluty) return undefined;
          return undefined;
        });

        vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted text' as any);
        vi.mocked(PDFFunctions.createHeader).mockReturnValue(['Header'] as any);
        vi.mocked(PDFFunctions.createSection).mockReturnValue({ section: 'content' } as any);
        vi.mocked(PDFFunctions.createLabelTextArray).mockReturnValue(['Label', 'Value'] as any);

        const faVatNoCurrency = { ...mockFaVat, KodWaluty: undefined } as any;

        generateWiersze(faVatNoCurrency);

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith(
          expect.arrayContaining([
            expect.objectContaining({
              currency: '',
            }),
          ])
        );
      });
    });

    describe('createSection call', () => {
      it('should call createSection with correct parameters', () => {
        setupBasicMocks('200', TRodzajFaktury.VAT);

        generateWiersze(mockFaVat);

        expect(PDFFunctions.createSection).toHaveBeenCalledWith(
          expect.arrayContaining(['Header', 'formatted text']),
          true
        );
      });

      it('should return the result of createSection', () => {
        const mockSection = { section: 'test' };
        setupBasicMocks('200', TRodzajFaktury.VAT);
        vi.mocked(PDFFunctions.createSection).mockReturnValue(mockSection as any);

        const result = generateWiersze(mockFaVat);

        expect(result).toEqual(mockSection);
      });
    });

    describe('header definitions', () => {
      it('should include all required headers in first table', () => {
        setupBasicMocks('200', TRodzajFaktury.VAT);

        generateWiersze(mockFaVat);

        const firstCall = vi.mocked(PDFFunctions.getContentTable).mock.calls[0];
        const headers = firstCall[0];

        expect(headers).toEqual(
          expect.arrayContaining([
            expect.objectContaining({ name: 'NrWierszaFa', title: 'Lp.' }),
            expect.objectContaining({ name: 'P_7', title: 'Nazwa towaru lub usługi' }),
            expect.objectContaining({ name: 'P_9A', title: 'Cena jedn. netto' }),
            expect.objectContaining({ name: 'P_11', title: 'Wartość sprzedaży netto' }),
          ])
        );
      });

      it('should use correct format types for currency fields', () => {
        setupBasicMocks('200', TRodzajFaktury.VAT);

        generateWiersze(mockFaVat);

        const firstCall = vi.mocked(PDFFunctions.getContentTable).mock.calls[0];
        const headers = firstCall[0];

        const currencyHeaders = headers.filter(
          (h: any) => h.format === FormatTyp.Currency || h.format === FormatTyp.Currency6
        );

        expect(currencyHeaders.length).toBeGreaterThan(0);
      });
    });
  });
});
