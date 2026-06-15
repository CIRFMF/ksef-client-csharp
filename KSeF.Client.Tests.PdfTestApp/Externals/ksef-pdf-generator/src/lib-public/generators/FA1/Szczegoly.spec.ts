import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generateSzczegoly } from './Szczegoly';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { TRodzajFaktury } from '../../../shared/consts/const';
import { Fa } from '../../types/fa1.types';

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
  let mockFaVat: Fa;

  beforeEach(() => {
    vi.clearAllMocks();

    mockFaVat = {
      FaWiersze: [],
      Zamowienie: { ZamowienieWiersz: [] },
      RodzajFaktury: TRodzajFaktury.VAT,
      OkresFa: { P_6_Od: { _text: '' }, P_6_Do: { _text: '' } },
      P_1: { _text: '2024-01-01' },
      P_1M: { _text: 'Warsaw' },
      OkresFaKorygowanej: { _text: '' },
      P_6: { _text: '2024-01-15' },
      KodWaluty: { _text: 'PLN' },
    } as any;

    vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
    vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header'] as any);
    vi.mocked(PDFFunctions.createLabelText).mockReturnValue('label' as any);
    vi.mocked(PDFFunctions.createLabelTextArray).mockReturnValue('labelArray' as any);
    vi.mocked(PDFFunctions.createSection).mockReturnValue('section' as any);
    vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns' as any);
    vi.mocked(PDFFunctions.getContentTable).mockReturnValue({ content: null, fieldsWithValue: [] });
    vi.mocked(PDFFunctions.getDifferentColumnsValue).mockReturnValue([]);
    vi.mocked(PDFFunctions.getValue).mockReturnValue('PLN');
    vi.mocked(PDFFunctions.hasColumnsValue).mockReturnValue(false);
    vi.mocked(PDFFunctions.hasValue).mockReturnValue(false);
  });

  it('should create header and section', () => {
    const result = generateSzczegoly(mockFaVat);

    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Szczegóły');
    expect(PDFFunctions.createSection).toHaveBeenCalledWith(expect.any(Array), true);
    expect(result).toEqual('section');
  });

  it('should call getTable for FaWiersze and ZamowienieWiersz', () => {
    generateSzczegoly(mockFaVat);
    expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockFaVat.FaWiersze?.FaWiersz);
    expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockFaVat.Zamowienie?.ZamowienieWiersz);
  });

  describe('P_6 label', () => {
    it('uses "Data otrzymania zapłaty" for ZAL', () => {
      generateSzczegoly({ ...mockFaVat, RodzajFaktury: TRodzajFaktury.ZAL } as any);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Data otrzymania zapłaty: ', mockFaVat.P_6);
    });

    it('uses "Data dokonania lub zakończenia dostawy" for VAT', () => {
      generateSzczegoly(mockFaVat);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Data dokonania lub zakończenia dostawy towarów lub wykonania usługi: ',
        mockFaVat.P_6
      );
    });
  });

  describe('P_6 scope', () => {
    it('creates scope array when P_6_Od and P_6_Do exist', () => {
      const data = {
        ...mockFaVat,
        OkresFa: { P_6_Od: { _text: '2024-01-01' }, P_6_Do: { _text: '2024-01-31' } },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockImplementation(
        (value: any) => value === data.OkresFa.P_6_Od || value === data.OkresFa.P_6_Do
      );

      generateSzczegoly(data);

      expect(PDFFunctions.createLabelTextArray).toHaveBeenCalledWith([
        { value: 'Data dokonania lub zakończenia dostawy towarów lub wykonania usługi: od ' },
        { value: data.OkresFa.P_6_Od, formatTyp: FormatTyp.Value },
        { value: ' do ' },
        { value: data.OkresFa.P_6_Do, formatTyp: FormatTyp.Value },
      ]);
    });
  });

  describe('ceny labels', () => {
    it('adds netto label when P_11 exists', () => {
      vi.mocked(PDFFunctions.hasColumnsValue).mockImplementation((col) => col === 'P_11');
      generateSzczegoly(mockFaVat);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Faktura wystawiona w cenach: ', 'netto');
    });

    it('adds brutto label when P_11 does not exist', () => {
      vi.mocked(PDFFunctions.hasColumnsValue).mockReturnValue(false);
      generateSzczegoly(mockFaVat);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Faktura wystawiona w cenach: ', 'brutto');
    });

    it('adds currency label', () => {
      generateSzczegoly(mockFaVat);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Kod waluty: ', mockFaVat.KodWaluty);
    });
  });

  describe('P_12_XII label', () => {
    it('adds OSS label when P_12_XII exists', () => {
      vi.mocked(PDFFunctions.hasColumnsValue).mockImplementation((col) => col === 'P_12_XII');
      generateSzczegoly(mockFaVat);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Procedura One Stop Shop', ' ');
    });
  });

  describe('kurs waluty', () => {
    it('adds currency rate when currency is not PLN and common rate exists', () => {
      const data = { ...mockFaVat, KodWaluty: { _text: 'EUR' }, Zamowienie: { ZamowienieWiersz: [] } } as any;
      vi.mocked(PDFFunctions.hasValue).mockReturnValue(true);
      vi.mocked(PDFFunctions.getValue).mockReturnValue('EUR');
      vi.mocked(PDFFunctions.getDifferentColumnsValue).mockReturnValue([{ value: '4.50' }]);
      generateSzczegoly(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Kurs waluty wspólny dla wszystkich wierszy faktury',
        ' '
      );
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Kurs waluty: ', '4.50', FormatTyp.Currency6);
    });

    it('does not add currency rate when PLN', () => {
      vi.mocked(PDFFunctions.getValue).mockReturnValue('PLN');
      generateSzczegoly(mockFaVat);
      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const currencyCall = calls.find((call) => call[0] === 'Kurs waluty: ');
      expect(currencyCall).toBeUndefined();
    });
  });

  describe('columns distribution', () => {
    it('calls generateTwoColumns with labels split', () => {
      generateSzczegoly(mockFaVat);
      expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith(expect.any(Array), expect.any(Array));
    });
  });

  describe('faktura zaliczkowa', () => {
    it('generates table when data exists', () => {
      const data = { ...mockFaVat, NrFaZaliczkowej: [{ _text: 'FA001' }] } as any;
      vi.mocked(PDFFunctions.getTable).mockReturnValueOnce([{ _text: 'FA001' }] as any);
      generateSzczegoly(data);
      expect(PDFFunctions.getContentTable).toHaveBeenCalled();
    });
  });
});
