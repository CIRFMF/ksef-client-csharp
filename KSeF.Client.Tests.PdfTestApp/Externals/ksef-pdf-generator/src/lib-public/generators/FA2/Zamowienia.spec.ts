import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { TRodzajFaktury } from '../../../shared/consts/const';
import { Zamowienie } from '../../types/fa2.types';
import { generateZamowienie } from './Zamowienie';
import { ZamowienieKorekta } from '../../enums/invoice.enums';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createLabelTextArray: vi.fn(),
  formatText: vi.fn(),
  getContentTable: vi.fn(),
  getTable: vi.fn(),
}));

describe(generateZamowienie.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('when orderData is undefined', () => {
    it('should return an empty array', () => {
      const result = generateZamowienie(
        undefined,
        ZamowienieKorekta.BeforeCorrection,
        '100',
        TRodzajFaktury.ZAL,
        'PLN'
      );

      expect(result).toEqual([]);
    });
  });

  describe('when orderData is defined', () => {
    const mockOrderData: Zamowienie = {
      ZamowienieWiersz: [
        {
          NrWierszaZam: { _text: '1' },
          P_7Z: { _text: 'Towar 1' },
          P_9AZ: { _text: '100' },
          P_8BZ: { _text: '2' },
        },
      ],
      WartoscZamowienia: { _text: '200' },
    } as any;

    beforeEach(() => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([
        {
          NrWierszaZam: { _text: '1' },
          P_7Z: { _text: 'Towar 1' },
          P_9AZ: { _text: '100' },
          P_8BZ: { _text: '2' },
        },
      ] as any);

      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: { table: {} } as any,
        fieldsWithValue: ['P_11'],
      });

      vi.mocked(PDFFunctions.createHeader).mockReturnValue('Header' as any);
      vi.mocked(PDFFunctions.formatText).mockReturnValue('200 PLN' as any);
    });

    it('should call getTable with ZamowienieWiersz', () => {
      generateZamowienie(mockOrderData, ZamowienieKorekta.BeforeCorrection, '100', TRodzajFaktury.ZAL, 'PLN');

      expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockOrderData.ZamowienieWiersz);
    });

    it('should fill NrWierszaZam if it is empty', () => {
      const dataWithEmptyNr = {
        ...mockOrderData,
        ZamowienieWiersz: [
          {
            NrWierszaZam: { _text: '' },
            P_7Z: { _text: 'Towar 1' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockReturnValue([
        {
          NrWierszaZam: { _text: '' },
          P_7Z: { _text: 'Towar 1' },
        },
      ] as any);

      generateZamowienie(
        dataWithEmptyNr,
        ZamowienieKorekta.BeforeCorrection,
        '100',
        TRodzajFaktury.ZAL,
        'PLN'
      );

      const tableCall = vi.mocked(PDFFunctions.getTable).mock.results[0].value;
      expect(tableCall[0].NrWierszaZam._text).toBe('1');
    });

    describe('price formatting', () => {
      it('should use FormatTyp.CurrencyAbs for BeforeCorrection', () => {
        vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
          content: { table: {} } as any,
          fieldsWithValue: ['field1', 'field2', 'field3'],
        });

        generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.BeforeCorrection,
          '100',
          TRodzajFaktury.ZAL,
          'PLN'
        );

        const calls = vi.mocked(PDFFunctions.getContentTable).mock.calls;
        const header = calls[0][0];
        const cenaNetto = header.find((h: any) => h.name === 'P_9AZ');
        expect(cenaNetto?.format).toBe(FormatTyp.CurrencyAbs);
      });

      it('should use FormatTyp.Currency when not BeforeCorrection', () => {
        vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
          content: { table: {} } as any,
          fieldsWithValue: ['field1', 'field2', 'field3'],
        });

        generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.AfterCorrection,
          '100',
          TRodzajFaktury.KOR_ZAL,
          'PLN'
        );

        const calls = vi.mocked(PDFFunctions.getContentTable).mock.calls;
        const header = calls[0][0];
        const cenaNetto = header.find((h: any) => h.name === 'P_9AZ');
        expect(cenaNetto?.format).toBe(FormatTyp.Currency);
      });
    });

    describe('table generation', () => {
      it('should generate one table when fieldsWithValue.length <= 8', () => {
        vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
          content: { table: {} } as any,
          fieldsWithValue: ['field1', 'field2', 'field3'],
        });

        const result = generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.BeforeCorrection,
          '100',
          TRodzajFaktury.ZAL,
          'PLN'
        );

        expect(PDFFunctions.getContentTable).toHaveBeenCalledTimes(1);
        expect(result[0]).toBeDefined();
      });

      it('should generate two tables when fieldsWithValue.length > 8', () => {
        vi.mocked(PDFFunctions.getContentTable)
          .mockReturnValueOnce({
            content: null,
            fieldsWithValue: Array(9).fill('field'),
          })
          .mockReturnValueOnce({
            content: { table: {} } as any,
            fieldsWithValue: ['field1', 'field2'],
          })
          .mockReturnValueOnce({
            content: { table: {} } as any,
            fieldsWithValue: ['field3', 'field4'],
          });

        generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.BeforeCorrection,
          '100',
          TRodzajFaktury.ZAL,
          'PLN'
        );

        expect(PDFFunctions.getContentTable).toHaveBeenCalledTimes(3);
      });
    });

    describe('payment amount description', () => {
      it('should add description for advance invoice (ZAL) when p_15 > 0', () => {
        vi.mocked(PDFFunctions.createLabelTextArray).mockReturnValue(['Label', 'Value'] as any);

        const result = generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.BeforeCorrection,
          '100',
          TRodzajFaktury.ZAL,
          'PLN'
        );

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith([
          { value: 'Otrzymana kwota zapłaty (zaliczki): ', formatTyp: FormatTyp.LabelGreater },
          { value: '100', formatTyp: FormatTyp.CurrencyGreater },
        ]);
      });

      it('should not add description for ZAL invoice when p_15 = 0', () => {
        vi.mocked(PDFFunctions.createLabelTextArray).mockClear();

        generateZamowienie(mockOrderData, ZamowienieKorekta.BeforeCorrection, '0', TRodzajFaktury.ZAL, 'PLN');

        expect(PDFFunctions.createLabelTextArray).not.toHaveBeenCalled();
      });

      it('should add description for advance correction (KOR_ZAL) when not BeforeCorrection', () => {
        vi.mocked(PDFFunctions.createLabelTextArray).mockReturnValue(['Label', 'Value'] as any);

        generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.AfterCorrection,
          '150',
          TRodzajFaktury.KOR_ZAL,
          'PLN'
        );

        expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith([
          { value: 'Kwota należności ogółem: ', formatTyp: FormatTyp.LabelGreater },
          { value: '150', formatTyp: FormatTyp.CurrencyGreater },
        ]);
      });
    });

    describe('price text', () => {
      it('should display "netto" when P_11 is in fieldsWithValue', () => {
        vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
          content: { table: {} } as any,
          fieldsWithValue: ['P_11', 'P_7Z'],
        });

        const result = generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.BeforeCorrection,
          '100',
          TRodzajFaktury.ZAL,
          'EUR'
        );

        const stack = (result[0] as any).stack;
        expect(stack[1]).toContain('netto');
        expect(stack[1]).toContain('EUR');
      });

      it('should display "brutto" when P_11 is not in fieldsWithValue', () => {
        vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
          content: { table: {} } as any,
          fieldsWithValue: ['P_7Z', 'P_9AZ'],
        });

        const result = generateZamowienie(
          mockOrderData,
          ZamowienieKorekta.BeforeCorrection,
          '100',
          TRodzajFaktury.ZAL,
          'USD'
        );

        const stack = (result[0] as any).stack;
        expect(stack[1]).toContain('brutto');
        expect(stack[1]).toContain('USD');
      });
    });

    it('should call createHeader with correct parameter', () => {
      generateZamowienie(mockOrderData, ZamowienieKorekta.BeforeCorrection, '100', TRodzajFaktury.ZAL, 'PLN');

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith(ZamowienieKorekta.BeforeCorrection);
    });

    it('should format order value', () => {
      generateZamowienie(mockOrderData, ZamowienieKorekta.BeforeCorrection, '100', TRodzajFaktury.ZAL, 'PLN');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith('200', FormatTyp.Currency);
    });
  });
});
