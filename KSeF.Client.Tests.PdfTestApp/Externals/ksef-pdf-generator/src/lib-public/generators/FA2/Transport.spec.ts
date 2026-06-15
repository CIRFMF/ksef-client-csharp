import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateTransport } from './Transport';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Transport } from '../../types/fa2.types';
import { Kraj } from '../../../shared/consts/const';
import * as PrzewoznikModule from './Przewoznik';
import * as CommonFunctions from '../../../shared/generators/common/functions';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createLabelText: vi.fn(),
  createSection: vi.fn(),
  createSubHeader: vi.fn(),
  formatText: vi.fn(),
  generateTwoColumns: vi.fn(),
  getTable: vi.fn(),
  hasValue: vi.fn(),
}));

vi.mock('./Przewoznik', () => ({
  generatePrzewoznik: vi.fn(),
}));

vi.mock('../../../shared/generators/common/functions', () => ({
  getDateTimeWithoutSeconds: vi.fn(),
  getRodzajTransportuString: vi.fn(),
  getOpisTransportuString: vi.fn(),
}));

describe(generateTransport.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockTransport: Transport = {
    RodzajTransportu: { _text: '1' },
    TransportInny: { _text: '0' },
    OpisInnegoTransportu: { _text: '' },
    NrZleceniaTransportu: { _text: 'TR001' },
    OpisLadunku: { _text: 'Goods' },
    LadunekInny: { _text: '0' },
    OpisInnegoLadunku: { _text: '' },
    JednostkaOpakowania: { _text: 'Box' },
    DataGodzRozpTransportu: { _text: '2024-01-01T10:00:00' },
    DataGodzZakTransportu: { _text: '2024-01-02T15:00:00' },
    Przewoznik: {} as any,
    WysylkaZ: {
      AdresL1: { _text: 'Street 1' },
      AdresL2: { _text: 'City 1' },
      KodKraju: { _text: 'PL' },
      GLN: { _text: '123456' },
    },
    WysylkaDo: {
      AdresL1: { _text: 'Street 2' },
      AdresL2: { _text: 'City 2' },
      KodKraju: { _text: 'DE' },
      GLN: { _text: '789012' },
    },
    WysylkaPrzez: [],
  } as any;

  beforeEach(() => {
    vi.mocked(PDFFunctions.createHeader).mockReturnValue('header' as any);
    vi.mocked(PDFFunctions.createLabelText).mockReturnValue('label' as any);
    vi.mocked(PDFFunctions.createSection).mockReturnValue('section' as any);
    vi.mocked(PDFFunctions.createSubHeader).mockReturnValue('subheader' as any);
    vi.mocked(PDFFunctions.formatText).mockReturnValue('text' as any);
    vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns' as any);
    vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
    vi.mocked(PDFFunctions.hasValue).mockReturnValue(true);
    vi.mocked(PrzewoznikModule.generatePrzewoznik).mockReturnValue('przewoznik' as any);
    vi.mocked(CommonFunctions.getDateTimeWithoutSeconds).mockReturnValue('2024-01-01 10:00');
    vi.mocked(CommonFunctions.getRodzajTransportuString).mockReturnValue('Road');
    vi.mocked(CommonFunctions.getOpisTransportuString).mockReturnValue('Opis');
  });

  it('should call createHeader with "Transport"', () => {
    generateTransport(mockTransport);

    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Transport');
  });

  it('should call createSection and return result', () => {
    const mockSection = { section: 'test' };
    vi.mocked(PDFFunctions.createSection).mockReturnValue(mockSection as any);

    const result = generateTransport(mockTransport);

    expect(PDFFunctions.createSection).toHaveBeenCalledWith(expect.any(Array), true);
    expect(result).toEqual(mockSection);
  });

  describe('rodzaj transportu section', () => {
    it('should add rodzaj transportu when RodzajTransportu exists', () => {
      const data = {
        ...mockTransport,
        RodzajTransportu: { _text: '1' },
      } as any;

      vi.mocked(CommonFunctions.getRodzajTransportuString).mockReturnValue('Road');

      generateTransport(data);

      expect(CommonFunctions.getRodzajTransportuString).toHaveBeenCalledWith(data.RodzajTransportu);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Rodzaj transportu: ', 'Road');
    });

    it('should add transport inny when TransportInny is "1"', () => {
      const data = {
        ...mockTransport,
        RodzajTransportu: { _text: '' },
        TransportInny: { _text: '1' },
        OpisInnegoTransportu: { _text: 'Custom transport' },
      } as any;

      generateTransport(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Rodzaj transportu: ', 'Transport inny');
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Opis innego rodzaju transportu: ',
        data.OpisInnegoTransportu
      );
    });

    it('should not add transport inny when OpisInnegoTransportu is empty', () => {
      const data = {
        ...mockTransport,
        RodzajTransportu: { _text: '' },
        TransportInny: { _text: '1' },
        OpisInnegoTransportu: { _text: '' },
      } as any;

      generateTransport(data);

      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const transportInnyCall = calls.find(
        (call) => call[0] === 'Rodzaj transportu: ' && call[1] === 'Transport inny'
      );
      expect(transportInnyCall).toBeUndefined();
    });
  });

  describe('dane transportu section', () => {
    it('should add numer zlecenia transportu', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Numer zlecenia transportu: ',
        mockTransport.NrZleceniaTransportu
      );
    });

    it('should add opis ladunku when hasValue returns true', () => {
      vi.mocked(PDFFunctions.hasValue).mockReturnValue(true);

      generateTransport(mockTransport);

      expect(PDFFunctions.hasValue).toHaveBeenCalledWith(mockTransport.OpisLadunku);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Opis ładunku: ', 'Opis');
    });

    it('should not add opis ladunku when hasValue returns false', () => {
      vi.mocked(PDFFunctions.hasValue).mockReturnValue(false);

      generateTransport(mockTransport);

      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const ladunekCall = calls.find((call) => call[0] === 'Opis ładunku: ');
      expect(ladunekCall).toBeUndefined();
    });

    it('should add ladunek inny when LadunekInny is "1"', () => {
      const data = {
        ...mockTransport,
        LadunekInny: { _text: '1' },
        OpisInnegoLadunku: { _text: 'Custom cargo' },
      } as any;

      vi.mocked(PDFFunctions.hasValue).mockReturnValue(true);

      generateTransport(data);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Opis ładunku: ', 'Ładunek inny');
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Opis innego ładunku: ',
        data.OpisInnegoLadunku
      );
    });

    it('should not add ladunek inny when OpisInnegoLadunku is empty', () => {
      const data = {
        ...mockTransport,
        LadunekInny: { _text: '1' },
        OpisInnegoLadunku: { _text: '' },
      } as any;

      generateTransport(data);

      const calls = vi.mocked(PDFFunctions.createLabelText).mock.calls;
      const ladunekInnyCall = calls.find(
        (call) => call[0] === 'Opis ładunku: ' && call[1] === 'Ładunek inny'
      );
      expect(ladunekInnyCall).toBeUndefined();
    });

    it('should add jednostka opakowania', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Jednostka opakowania: ',
        mockTransport.JednostkaOpakowania
      );
    });

    it('should add data rozpoczecia transportu', () => {
      vi.mocked(CommonFunctions.getDateTimeWithoutSeconds).mockReturnValue('2024-01-01 10:00');

      generateTransport(mockTransport);

      expect(CommonFunctions.getDateTimeWithoutSeconds).toHaveBeenCalledWith(
        mockTransport.DataGodzRozpTransportu
      );
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Data i godzina rozpoczęcia transportu: ',
        '2024-01-01 10:00'
      );
    });

    it('should add data zakonczenia transportu', () => {
      vi.mocked(CommonFunctions.getDateTimeWithoutSeconds).mockReturnValue('2024-01-02 15:00');

      generateTransport(mockTransport);

      expect(CommonFunctions.getDateTimeWithoutSeconds).toHaveBeenCalledWith(
        mockTransport.DataGodzZakTransportu
      );
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
        'Data i godzina zakończenia transportu: ',
        '2024-01-02 15:00'
      );
    });

    it('should add subheader for dane transportu', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Dane transportu', [0, 0, 0, 0]);
    });
  });

  describe('przewoznik section', () => {
    it('should call generatePrzewoznik', () => {
      generateTransport(mockTransport);

      expect(PrzewoznikModule.generatePrzewoznik).toHaveBeenCalledWith(mockTransport.Przewoznik);
    });
  });

  describe('wysylka section', () => {
    it('should call createHeader with "Wysyłka"', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Wysyłka');
    });

    it('should add WysylkaZ address', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Adres miejsca wysyłki', [0, 0, 0, 0]);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Street 1', FormatTyp.Default);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith('City 1', FormatTyp.Default);
    });

    it('should add WysylkaZ country code', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.formatText).toHaveBeenCalledWith(Kraj['PL'], FormatTyp.Default);
    });

    it('should add WysylkaZ GLN', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('GLN: ', '123456');
    });

    it('should add WysylkaDo address when AdresL1 exists', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith(
        'Adres miejsca docelowego, do którego został zlecony transport',
        [0, 0, 0, 0]
      );
      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Street 2', FormatTyp.Default);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith('City 2', FormatTyp.Default);
    });

    it('should add WysylkaDo country code', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.formatText).toHaveBeenCalledWith(Kraj['DE'], FormatTyp.Default);
    });

    it('should add WysylkaDo GLN', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('GLN: ', '789012');
    });

    it('should not add WysylkaDo when AdresL1 is missing', () => {
      const data = {
        ...mockTransport,
        WysylkaDo: {
          AdresL1: undefined,
          AdresL2: { _text: 'City 2' },
        },
      } as any;

      generateTransport(data);

      const calls = vi.mocked(PDFFunctions.createSubHeader).mock.calls;
      const wysylkaDoCall = calls.find(
        (call) => call[0] === 'Adres miejsca docelowego, do którego został zlecony transport'
      );
      expect(wysylkaDoCall).toBeUndefined();
    });

    it('should handle undefined country code', () => {
      const data = {
        ...mockTransport,
        WysylkaZ: {
          ...mockTransport.WysylkaZ,
          KodKraju: { _text: '' },
        },
      } as any;

      generateTransport(data);

      expect(PDFFunctions.formatText).toHaveBeenCalledWith(Kraj[''], FormatTyp.Default);
    });
  });

  describe('wysylka przez section', () => {
    it('should add WysylkaPrzez addresses', () => {
      const data = {
        ...mockTransport,
        WysylkaPrzez: [
          {
            AdresL1: { _text: 'Street 3' },
            AdresL2: { _text: 'City 3' },
            KodKraju: { _text: 'FR' },
            GLN: { _text: '345678' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockReturnValue([
        {
          AdresL1: { _text: 'Street 3' },
          AdresL2: { _text: 'City 3' },
          KodKraju: { _text: 'FR' },
          GLN: { _text: '345678' },
        },
      ] as any);

      generateTransport(data);

      expect(PDFFunctions.getTable).toHaveBeenCalledWith(data.WysylkaPrzez);
      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Adres pośredni wysyłki', [0, 4, 0, 0]);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith('Street 3', FormatTyp.Default);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith('City 3', FormatTyp.Default);
      expect(PDFFunctions.formatText).toHaveBeenCalledWith(Kraj['FR'], FormatTyp.Default);
      expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('GLN: ', '345678');
    });

    it('should add newline between multiple WysylkaPrzez addresses', () => {
      const data = {
        ...mockTransport,
        WysylkaPrzez: [
          {
            AdresL1: { _text: 'Street 3' },
            AdresL2: { _text: 'City 3' },
            KodKraju: { _text: 'FR' },
            GLN: { _text: '345678' },
          },
          {
            AdresL1: { _text: 'Street 4' },
            AdresL2: { _text: 'City 4' },
            KodKraju: { _text: 'IT' },
            GLN: { _text: '901234' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockReturnValue([
        {
          AdresL1: { _text: 'Street 3' },
          AdresL2: { _text: 'City 3' },
          KodKraju: { _text: 'FR' },
          GLN: { _text: '345678' },
        },
        {
          AdresL1: { _text: 'Street 4' },
          AdresL2: { _text: 'City 4' },
          KodKraju: { _text: 'IT' },
          GLN: { _text: '901234' },
        },
      ] as any);

      generateTransport(data);

      const generateTwoColumnsCalls = vi.mocked(PDFFunctions.generateTwoColumns).mock.calls;
      const lastCall = generateTwoColumnsCalls[generateTwoColumnsCalls.length - 1];

      expect(Array.isArray(lastCall[0])).toBe(true);
      expect(Array.isArray(lastCall[1])).toBe(true);
      expect(lastCall[1]).toEqual([]);
      expect(lastCall[0]).toContain('\n');
    });

    it('should not add WysylkaPrzez when empty', () => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([]);

      generateTransport(mockTransport);

      const calls = vi.mocked(PDFFunctions.createSubHeader).mock.calls;
      const wysylkaPrzezCalls = calls.filter((call) => call[0] === 'Adres pośredni wysyłki');
      expect(wysylkaPrzezCalls).toHaveLength(0);
    });
  });

  describe('generateTwoColumns calls', () => {
    it('should call generateTwoColumns for transport and dane', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith(expect.any(Array), expect.any(Array));
    });

    it('should call generateTwoColumns for wysylkaZ and wysylkaDo', () => {
      generateTransport(mockTransport);

      const calls = vi.mocked(PDFFunctions.generateTwoColumns).mock.calls;
      expect(calls.length).toBeGreaterThanOrEqual(2);
    });

    it('should call generateTwoColumns for wysylkaPrzez with empty second column', () => {
      generateTransport(mockTransport);

      expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith(expect.any(Array), []);
    });
  });

  describe('complete integration', () => {
    it('should handle all sections when fully populated', () => {
      const data = {
        RodzajTransportu: { _text: '1' },
        TransportInny: { _text: '0' },
        NrZleceniaTransportu: { _text: 'TR001' },
        OpisLadunku: { _text: 'Goods' },
        LadunekInny: { _text: '1' },
        OpisInnegoLadunku: { _text: 'Custom' },
        JednostkaOpakowania: { _text: 'Box' },
        DataGodzRozpTransportu: { _text: '2024-01-01T10:00:00' },
        DataGodzZakTransportu: { _text: '2024-01-02T15:00:00' },
        Przewoznik: {} as any,
        WysylkaZ: {
          AdresL1: { _text: 'Street 1' },
          AdresL2: { _text: 'City 1' },
          KodKraju: { _text: 'PL' },
          GLN: { _text: '123456' },
        },
        WysylkaDo: {
          AdresL1: { _text: 'Street 2' },
          AdresL2: { _text: 'City 2' },
          KodKraju: { _text: 'DE' },
          GLN: { _text: '789012' },
        },
        WysylkaPrzez: [
          {
            AdresL1: { _text: 'Street 3' },
            AdresL2: { _text: 'City 3' },
            KodKraju: { _text: 'FR' },
            GLN: { _text: '345678' },
          },
        ],
      } as any;

      vi.mocked(PDFFunctions.getTable).mockReturnValue([
        {
          AdresL1: { _text: 'Street 3' },
          AdresL2: { _text: 'City 3' },
          KodKraju: { _text: 'FR' },
          GLN: { _text: '345678' },
        },
      ] as any);

      generateTransport(data);

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Transport');
      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Wysyłka');
      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Dane transportu', [0, 0, 0, 0]);
      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Adres miejsca wysyłki', [0, 0, 0, 0]);
      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith(
        'Adres miejsca docelowego, do którego został zlecony transport',
        [0, 0, 0, 0]
      );
      expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Adres pośredni wysyłki', [0, 4, 0, 0]);
      expect(PrzewoznikModule.generatePrzewoznik).toHaveBeenCalled();
      expect(PDFFunctions.createSection).toHaveBeenCalled();
    });
  });
});
