import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateSzczegoly } from './Szczegoly';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { TRodzajFaktury } from '../../../shared/consts/const';
import { Fa } from '../../types/fa3.types';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createLabelText: vi.fn(),
  createLabelTextArray: vi.fn(),
  createSection: vi.fn(),
  generateTwoColumns: vi.fn(),
  getContentTable: vi.fn(),
  getDifferentColumnsValue: vi.fn(),
  getTable: vi.fn(),
  getValue: vi.fn(),
  hasColumnsValue: vi.fn(),
  hasValue: vi.fn(),
}));

describe(generateSzczegoly.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockFaVat: Fa = {
    FaWiersz: [],
    Zamowienie: {
      ZamowienieWiersz: [],
    },
    RodzajFaktury: TRodzajFaktury.VAT,
    OkresFa: {
      P_6_Od: { _text: '' },
      P_6_Do: { _text: '' },
    },
    P_1: { _text: '2024-01-01' },
    P_1M: { _text: 'Warsaw' },
    OkresFaKorygowanej: { _text: '' },
    P_6: { _text: '2024-01-15' },
    KodWaluty: { _text: 'PLN' },
    KursWalutyZ: { _text: '' },
  } as any;

  beforeEach(() => {
    vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
    vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header'] as any);
    vi.mocked(PDFFunctions.createLabelText).mockReturnValue('label' as any);
    vi.mocked(PDFFunctions.createLabelTextArray).mockReturnValue('labelArray' as any);
    vi.mocked(PDFFunctions.createSection).mockReturnValue('section' as any);
    vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns' as any);
    vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
      content: null,
      fieldsWithValue: [],
    });
    vi.mocked(PDFFunctions.getDifferentColumnsValue).mockReturnValue([]);
    vi.mocked(PDFFunctions.getValue).mockReturnValue('PLN');
    vi.mocked(PDFFunctions.hasColumnsValue).mockReturnValue(false);
    vi.mocked(PDFFunctions.hasValue).mockReturnValue(false);
  });

  it('should call createHeader with "Szczegóły"', () => {
    generateSzczegoly(mockFaVat);

    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Szczegóły');
  });

  it('should call createSection and return result', () => {
    const mockSection = 'section';
    vi.mocked(PDFFunctions.createSection).mockReturnValue(mockSection as any);

    const result = generateSzczegoly(mockFaVat);

    expect(PDFFunctions.createSection).toHaveBeenCalledWith(expect.any(Array), true);
    expect(result).toEqual(mockSection);
  });

  it('should call getTable for FaWiersz', () => {
    generateSzczegoly(mockFaVat);

    expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockFaVat.FaWiersz);
  });

  it('should call getTable for ZamowienieWiersz', () => {
    generateSzczegoly(mockFaVat);

    expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockFaVat.Zamowienie?.ZamowienieWiersz);
  });

  describe('P_6 label', () => {
    it('should use "Data otrzymania zapłaty" label for ZAL invoice', () => {
      const data = {
        ...mockFaVat,
        RodzajFaktury: TRodzajFaktury.ZAL,
      } as any;

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Data otrzymania zapłaty: ', data.P_6);
    });

    it('should use "Data otrzymania zapłaty" label for KOR_ZAL invoice', () => {
      const data = {
        ...mockFaVat,
        RodzajFaktury: TRodzajFaktury.KOR_ZAL,
      } as any;

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Data otrzymania zapłaty: ', data.P_6);
    });

    it('should use "Data dokonania lub zakończenia dostawy" label for other invoice types', () => {
      const data = {
        ...mockFaVat,
        RodzajFaktury: TRodzajFaktury.VAT,
      } as any;

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Data dokonania lub zakończenia dostawy towarów lub wykonania usługi: ',
        data.P_6
      );
    });
  });

  describe('P_6 scope', () => {
    it('should generate P_6 scope when both P_6_Od and P_6_Do exist', () => {
      const data = {
        ...mockFaVat,
        OkresFa: {
          P_6_Od: { _text: '2024-01-01' },
          P_6_Do: { _text: '2024-01-31' },
        },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockImplementation(
        (value: any) => value === data.OkresFa.P_6_Od || value === data.OkresFa.P_6_Do
      );

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith([
        {
          value: 'Data dokonania lub zakończenia dostawy towarów lub wykonania usługi: od ',
        },
        { value: data.OkresFa.P_6_Od, formatTyp: FormatTyp.Value },
        { value: ' do ' },
        { value: data.OkresFa.P_6_Do, formatTyp: FormatTyp.Value },
      ]);
    });

    it('should generate P_6 scope when only P_6_Od exists', () => {
      const data = {
        ...mockFaVat,
        OkresFa: {
          P_6_Od: { _text: '2024-01-01' },
          P_6_Do: { _text: '' },
        },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockImplementation((value: any) => value === data.OkresFa.P_6_Od);

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Data dokonania lub zakończenia dostawy towarów lub wykonania usługi: od ',
        data.OkresFa.P_6_Od
      );
    });

    it('should generate P_6 scope when only P_6_Do exists', () => {
      const data = {
        ...mockFaVat,
        OkresFa: {
          P_6_Od: { _text: '' },
          P_6_Do: { _text: '2024-01-31' },
        },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockImplementation((value: any) => value === data.OkresFa.P_6_Do);

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Data dokonania lub zakończenia dostawy towarów lub wykonania usługi: do ',
        data.OkresFa.P_6_Do
      );
    });

    it('should not generate P_6 scope when both are empty', () => {
      vi.mocked(PDFFunctions.hasValue).mockReturnValue(false);
      vi.mocked(PDFFunctions.createLabelTextArray).mockClear();

      generateSzczegoly(mockFaVat);

      const calls = vi.mocked(PDFFunctions.createLabelTextArray).mock.calls;
      const p6Call = calls.find((call) =>
        call[0].some((item: any) => item.value?.includes('Data dokonania lub zakończenia'))
      );
      expect(p6Call).toBeUndefined();
    });
  });

  describe('ceny labels', () => {
    it('should add "netto" label when P_11 exists in FaWiersz', () => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([]);

      vi.mocked(PDFFunctions.hasColumnsValue).mockImplementation((column: string) => column === 'P_11');

      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Faktura wystawiona w cenach: ', 'netto');
    });

    it('should add "netto" label when P_11 exists in ZamowienieWiersz', () => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([]);

      vi.mocked(PDFFunctions.hasColumnsValue).mockImplementation((column: string) => column === 'P_11');

      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Faktura wystawiona w cenach: ', 'netto');
    });

    it('should add "brutto" label when P_11 does not exist', () => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
      vi.mocked(PDFFunctions.hasColumnsValue).mockReturnValue(false);

      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Faktura wystawiona w cenach: ', 'brutto');
    });

    it('should add currency code label', () => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([]);

      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Kod waluty: ', mockFaVat.KodWaluty);
    });

    it('should not add ceny labels when FaWiersz or ZamowienieWiersz exist', () => {
      vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
        if (field === mockFaVat.FaWiersz) return [{}];
        return [];
      });

      vi.mocked(PDFFunctions.createLabelText).mockClear();

      generateSzczegoly(mockFaVat);

      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const cenyCall = calls.find((call) => call[0] === 'Faktura wystawiona w cenach: ');
      expect(cenyCall).toBeUndefined();
    });
  });

  describe('P_12_XII label', () => {
    it('should add OSS label when P_12_XII exists in FaWiersz', () => {
      vi.mocked(PDFFunctions.hasColumnsValue).mockImplementation((column: string) => column === 'P_12_XII');

      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Procedura One Stop Shop', ' ');
    });

    it('should add OSS label when P_12_XII exists in ZamowienieWiersz', () => {
      vi.mocked(PDFFunctions.hasColumnsValue).mockImplementation((column: string) => column === 'P_12_XII');

      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Procedura One Stop Shop', ' ');
    });

    it('should not add OSS label when P_12_XII does not exist', () => {
      vi.mocked(PDFFunctions.hasColumnsValue).mockReturnValue(false);
      vi.mocked(PDFFunctions.createLabelText).mockClear();

      generateSzczegoly(mockFaVat);

      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const ossCall = calls.find((call) => call[0] === 'Procedura One Stop Shop');
      expect(ossCall).toBeUndefined();
    });
  });

  describe('kurs waluty labels', () => {
    it('should add currency rate from KursWalutyZ when exists', () => {
      const data = {
        ...mockFaVat,
        KodWaluty: { _text: 'EUR' },
        KursWalutyZ: { _text: '4.50' },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockImplementation(
        (value: any) => value === data.KodWaluty || value === data.KursWalutyZ
      );

      vi.mocked(PDFFunctions.getValue).mockImplementation((value: any) =>
        value === data.KodWaluty ? 'EUR' : ''
      );

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Kurs waluty wspólny dla wszystkich wierszy faktury',
        ' '
      );
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Kurs waluty: ',
        data.KursWalutyZ,
        FormatTyp.Currency6
      );
    });

    it('should add currency rate from common KursWaluty when KursWalutyZ does not exist', () => {
      const data = {
        ...mockFaVat,
        KodWaluty: { _text: 'EUR' },
        KursWalutyZ: { _text: '' },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockImplementation((value: any) => value === data.KodWaluty);

      vi.mocked(PDFFunctions.getValue).mockImplementation((value: any) =>
        value === data.KodWaluty ? 'EUR' : ''
      );

      vi.mocked(PDFFunctions.getDifferentColumnsValue).mockReturnValue([{ value: '4.50' }] as any);

      generateSzczegoly(data);

      expect(PDFFunctions.getDifferentColumnsValue).toHaveBeenCalledWith('KursWaluty', []);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Kurs waluty wspólny dla wszystkich wierszy faktury',
        ' '
      );
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Kurs waluty: ', '4.50', FormatTyp.Currency6);
    });

    it('should not add currency rate when KodWaluty is PLN', () => {
      vi.mocked(PDFFunctions.hasValue).mockReturnValue(true);
      vi.mocked(PDFFunctions.getValue).mockReturnValue('PLN');
      vi.mocked(PDFFunctions.createLabelText).mockClear();

      generateSzczegoly(mockFaVat);

      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const currencyCall = calls.find((call) => call[0] === 'Kurs waluty: ');
      expect(currencyCall).toBeUndefined();
    });

    it('should not add currency rate when multiple different rates exist', () => {
      const data = {
        ...mockFaVat,
        KodWaluty: { _text: 'EUR' },
        KursWalutyZ: { _text: '' },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockImplementation((value: any) => value === data.KodWaluty);

      vi.mocked(PDFFunctions.getValue).mockImplementation((value: any) =>
        value === data.KodWaluty ? 'EUR' : ''
      );

      vi.mocked(PDFFunctions.getDifferentColumnsValue).mockReturnValue([
        { value: '4.50' },
        { value: '4.60' },
      ] as any);

      vi.mocked(PDFFunctions.createLabelText).mockClear();

      generateSzczegoly(data);

      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const currencyCall = calls.find((call) => call[0] === 'Kurs waluty: ');
      expect(currencyCall).toBeUndefined();
    });
  });

  describe('standard labels', () => {
    it('should add data wystawienia label', () => {
      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Data wystawienia, z zastrzeżeniem art. 106na ust. 1 ustawy: ',
        mockFaVat.P_1
      );
    });

    it('should add miejsce wystawienia label', () => {
      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Miejsce wystawienia: ', mockFaVat.P_1M);
    });

    it('should add okres rabat label', () => {
      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Okres, którego dotyczy rabat: ',
        mockFaVat.OkresFaKorygowanej
      );
    });
  });

  describe('columns generation', () => {
    it('should call generateTwoColumns', () => {
      generateSzczegoly(mockFaVat);

      expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith(expect.any(Array), expect.any(Array));
    });

    it('should distribute labels between two columns', () => {
      generateSzczegoly(mockFaVat);

      const calls = vi.mocked(PDFFunctions.generateTwoColumns).mock.calls;
      expect(calls.length).toBeGreaterThanOrEqual(1);

      const firstCall = calls[0];
      expect(Array.isArray(firstCall[0])).toBe(true);
      expect(Array.isArray(firstCall[1])).toBe(true);
    });
  });

  describe('zaliczka czesciowa', () => {
    it('should generate zaliczka czesciowa table when data exists', () => {
      const data = {
        ...mockFaVat,
        ZaliczkaCzesciowa: [
          {
            P_6Z: { _text: '2024-01-01' },
            P_15Z: { _text: '1000' },
            KursWalutyZW: { _text: '4.50' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
        if (field === data.ZaliczkaCzesciowa)
          return [
            {
              P_6Z: { _text: '2024-01-01' },
              P_15Z: { _text: '1000' },
              KursWalutyZW: { _text: '4.50' },
            },
          ] as any;
        return [];
      });

      vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
        content: null,
        fieldsWithValue: [],
      });
      vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
        content: { table: 'zaliczka' } as any,
        fieldsWithValue: ['P_6Z'],
      });

      generateSzczegoly(data);

      expect(PDFFunctions.getContentTable).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({ name: 'P_6Z', title: 'Data otrzymania płatności' }),
          expect.objectContaining({ name: 'P_15Z', title: 'Kwota płatności' }),
          expect.objectContaining({ name: 'KursWalutyZW', title: 'Kurs waluty' }),
        ]),
        expect.any(Array),
        'auto'
      );
    });

    it('should not generate zaliczka czesciowa when data is undefined', () => {
      const data = {
        ...mockFaVat,
        ZaliczkaCzesciowa: undefined,
      } as any;

      const contentTableCalls = vi.mocked(PDFFunctions.getContentTable).mock.calls.length;

      generateSzczegoly(data);

      expect(vi.mocked(PDFFunctions.getContentTable).mock.calls.length).toBe(contentTableCalls);
    });

    it('should not include zaliczka czesciowa when content is null', () => {
      const data = {
        ...mockFaVat,
        ZaliczkaCzesciowa: [
          {
            P_6Z: { _text: '' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
        if (field === data.ZaliczkaCzesciowa) return [{ P_6Z: { _text: '' } }] as any;
        return [];
      });

      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: [],
      });

      const section = generateSzczegoly(data);

      expect(section).toBeDefined();
    });
  });

  describe('faktura zaliczkowa', () => {
    it('should generate faktura zaliczkowa table when data exists', () => {
      const data = {
        ...mockFaVat,
        FakturaZaliczkowa: [
          {
            NrKSeFFaZaliczkowej: { _text: 'FA001' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
        if (field === data.FakturaZaliczkowa)
          return [
            {
              NrKSeFFaZaliczkowej: { _text: 'FA001' },
            },
          ] as any;
        return [];
      });

      vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
        content: null,
        fieldsWithValue: [],
      });
      vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
        content: { table: 'faktura' } as any,
        fieldsWithValue: ['NrKSeFFaZaliczkowej'],
      });

      generateSzczegoly(data);

      expect(PDFFunctions.getContentTable).toHaveBeenCalledWith(
        expect.arrayContaining([
          expect.objectContaining({
            name: 'NrKSeFFaZaliczkowej',
            title: 'Numery wcześniejszych faktur zaliczkowych',
          }),
        ]),
        expect.any(Array),
        'auto',
        [0, 4, 0, 0]
      );
    });

    it('should not generate faktura zaliczkowa when data is undefined', () => {
      const data = {
        ...mockFaVat,
        FakturaZaliczkowa: undefined,
      } as any;

      const contentTableCalls = vi.mocked(PDFFunctions.getContentTable).mock.calls.length;

      generateSzczegoly(data);

      expect(vi.mocked(PDFFunctions.getContentTable).mock.calls.length).toBe(contentTableCalls);
    });

    it('should not include faktura zaliczkowa when content is null', () => {
      const data = {
        ...mockFaVat,
        FakturaZaliczkowa: [
          {
            NrKSeFFaZaliczkowej: { _text: '' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
        if (field === data.FakturaZaliczkowa) return [{ NrKSeFFaZaliczkowej: { _text: '' } }] as any;
        return [];
      });

      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: [],
      });

      const section = generateSzczegoly(data);

      expect(section).toBeDefined();
    });
  });
});
