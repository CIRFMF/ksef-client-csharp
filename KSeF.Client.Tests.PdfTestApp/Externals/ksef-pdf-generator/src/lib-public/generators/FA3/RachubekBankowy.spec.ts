import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generujRachunekBankowy } from './RachunekBankowy';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { RachunekBankowy } from '../../types/fa3.types';
import * as CommonFunctions from '../../../shared/generators/common/functions';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createSection: vi.fn(),
  formatText: vi.fn(),
}));

vi.mock('../../../shared/generators/common/functions', () => ({
  getTypRachunkowWlasnych: vi.fn(),
}));

describe(generujRachunekBankowy.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockAccount: RachunekBankowy = {
    NrRB: { _text: '12345678901234567890123456' },
    SWIFT: { _text: 'BPKOPLPW' },
    RachunekWlasnyBanku: { _text: '1' },
    NazwaBanku: { _text: 'PKO Bank Polski' },
    OpisRachunku: { _text: 'Rachunek główny' },
  } as any;

  beforeEach(() => {
    vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header'] as any);
    vi.mocked(PDFFunctions.createSection).mockReturnValue('section' as any);
    vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted' as any);
    vi.mocked(CommonFunctions.getTypRachunkowWlasnych).mockReturnValue('Tak');
  });

  describe('when accounts is undefined or empty', () => {
    it('should return empty array when accounts is undefined', () => {
      const result = generujRachunekBankowy(undefined, 'Rachunek bankowy');

      expect(result).toEqual([]);
    });

    it('should return empty array when accounts is empty array', () => {
      const result = generujRachunekBankowy([], 'Rachunek bankowy');

      expect(result).toEqual([]);
    });
  });

  describe('when accounts exist', () => {
    it('should call createHeader with provided title', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Rachunek bankowy ', [0, 12, 0, 8]);
    });

    it('should call createHeader with empty string when title is not provided', () => {
      generujRachunekBankowy([mockAccount]);

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('', [0, 12, 0, 8]);
    });

    it('should call createSection and return result', () => {
      const mockSection = 'section';

      vi.mocked(PDFFunctions.createSection).mockReturnValue(mockSection as any);

      const result = generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      expect(PDFFunctions.createSection).toHaveBeenCalledWith(expect.any(Array), false);
      expect(result).toEqual(mockSection);
    });

    it('should format "Pełny numer rachunku" field', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Pełny numer rachunku', FormatTyp.GrayBoldTitle);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith(mockAccount.NrRB?._text, FormatTyp.Default);
    });

    it('should format "Kod SWIFT" field', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Kod SWIFT', FormatTyp.GrayBoldTitle);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith(mockAccount.SWIFT?._text, FormatTyp.Default);
    });

    it('should format "Rachunek własny banku" field', () => {
      vi.mocked(CommonFunctions.getTypRachunkowWlasnych).mockReturnValue('Tak');

      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Rachunek własny banku', FormatTyp.GrayBoldTitle);
      expect(CommonFunctions.getTypRachunkowWlasnych).toHaveBeenCalledWith(mockAccount.RachunekWlasnyBanku);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Tak', FormatTyp.Default);
    });

    it('should format "Nazwa banku" field', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Nazwa banku', FormatTyp.GrayBoldTitle);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith(mockAccount.NazwaBanku?._text, FormatTyp.Default);
    });

    it('should format "Opis rachunku" field', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Opis rachunku', FormatTyp.GrayBoldTitle);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith(
        mockAccount.OpisRachunku?._text,
        FormatTyp.Default
      );
    });

    it('should create table structure with correct widths', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      const tableContent = sectionCall[0][1];

      expect(tableContent).toHaveProperty('table');
      expect(tableContent.table).toHaveProperty('widths', ['*', 'auto']);
    });

    it('should create table structure with unbreakable property', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      const tableContent = sectionCall[0][1];

      expect(tableContent).toHaveProperty('unbreakable', true);
    });

    it('should create table with correct layout', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      const tableContent = sectionCall[0][1];

      expect(tableContent).toHaveProperty('layout');
      expect(tableContent.layout).toHaveProperty('hLineWidth');
      expect(tableContent.layout).toHaveProperty('hLineColor');
      expect(tableContent.layout).toHaveProperty('vLineWidth');
      expect(tableContent.layout).toHaveProperty('vLineColor');
    });

    it('should create table with correct line colors', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      const tableContent = sectionCall[0][1];

      expect(tableContent.layout.hLineColor()).toBe('#BABABA');
      expect(tableContent.layout.vLineColor()).toBe('#BABABA');
    });

    it('should create table with correct line widths', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      const tableContent = sectionCall[0][1];

      expect(tableContent.layout.hLineWidth()).toBe(1);
      expect(tableContent.layout.vLineWidth()).toBe(1);
    });

    it('should handle account with undefined fields', () => {
      const accountWithUndefined: RachunekBankowy = {
        NrRB: { _text: undefined },
        SWIFT: { _text: undefined },
        RachunekWlasnyBanku: { _text: undefined },
        NazwaBanku: { _text: undefined },
        OpisRachunku: { _text: undefined },
      } as any;

      generujRachunekBankowy([accountWithUndefined], 'Rachunek bankowy');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith(undefined, FormatTyp.Default);
    });
  });

  describe('when multiple accounts exist', () => {
    it('should create multiple tables for multiple accounts', () => {
      const account2: RachunekBankowy = {
        NrRB: { _text: '98765432109876543210987654' },
        SWIFT: { _text: 'PKOPPLPW' },
        RachunekWlasnyBanku: { _text: '0' },
        NazwaBanku: { _text: 'mBank' },
        OpisRachunku: { _text: 'Rachunek pomocniczy' },
      } as any;

      generujRachunekBankowy([mockAccount, account2], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];

      expect(sectionCall.length).toBe(2);
    });

    it('should call getTypRachunkowWlasnych for each account', () => {
      const account2: RachunekBankowy = {
        NrRB: { _text: '98765432109876543210987654' },
        SWIFT: { _text: 'PKOPPLPW' },
        RachunekWlasnyBanku: { _text: '0' },
        NazwaBanku: { _text: 'mBank' },
        OpisRachunku: { _text: 'Rachunek pomocniczy' },
      } as any;

      generujRachunekBankowy([mockAccount, account2], 'Rachunek bankowy');

      expect(CommonFunctions.getTypRachunkowWlasnych).toHaveBeenCalledTimes(2);
      expect(CommonFunctions.getTypRachunkowWlasnych).toHaveBeenCalledWith(mockAccount.RachunekWlasnyBanku);
      expect(CommonFunctions.getTypRachunkowWlasnych).toHaveBeenCalledWith(account2.RachunekWlasnyBanku);
    });

    it('should format all fields for all accounts', () => {
      const account2: RachunekBankowy = {
        NrRB: { _text: '98765432109876543210987654' },
        SWIFT: { _text: 'PKOPPLPW' },
        RachunekWlasnyBanku: { _text: '0' },
        NazwaBanku: { _text: 'mBank' },
        OpisRachunku: { _text: 'Rachunek pomocniczy' },
      } as any;

      generujRachunekBankowy([mockAccount, account2], 'Rachunek bankowy');

      expect(PDFFunctions.formatText).toHaveBeenCalledWith(mockAccount.NrRB?._text, FormatTyp.Default);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith(account2.NrRB?._text, FormatTyp.Default);
    });
  });

  describe('table structure', () => {
    it('should create table with 5 rows', () => {
      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      const tableContent = sectionCall[0][1];

      expect(tableContent.table.body.length).toBe(5);
    });

    it('should include header in result array', () => {
      vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header-content'] as any);

      generujRachunekBankowy([mockAccount], 'Rachunek bankowy');

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];

      expect(sectionCall[0][0]).toBe('header-content');
    });
  });
});
