import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateTransport } from './Transport';
import * as PDFFunctions from '../../../shared/PDF-functions';
import * as PrzewoznikModule from './Przewoznik';
import * as CommonFunctions from '../../../shared/generators/common/functions';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createLabelText: vi.fn(),
  createSection: vi.fn(),
  createSubHeader: vi.fn(),
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
  let mockTransport: any;

  beforeEach(() => {
    vi.clearAllMocks();
    mockTransport = {
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
      Przewoznik: {},
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
    };

    vi.mocked(PDFFunctions.createHeader).mockReturnValue('header' as any);
    vi.mocked(PDFFunctions.createLabelText).mockReturnValue('label' as any);
    vi.mocked(PDFFunctions.createSection).mockReturnValue('section' as any);
    vi.mocked(PDFFunctions.createSubHeader).mockReturnValue('subheader' as any);
    vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns' as any);
    vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
    vi.mocked(PDFFunctions.hasValue).mockReturnValue(true);
    vi.mocked(PrzewoznikModule.generatePrzewoznik).mockReturnValue('przewoznik' as any);
    vi.mocked(CommonFunctions.getDateTimeWithoutSeconds).mockReturnValue('2024-01-01 10:00');
    vi.mocked(CommonFunctions.getRodzajTransportuString).mockReturnValue('Road');
    vi.mocked(CommonFunctions.getOpisTransportuString).mockReturnValue('Opis');
  });

  it('should return a section', () => {
    const result = generateTransport(mockTransport);
    expect(PDFFunctions.createSection).toHaveBeenCalledWith(expect.any(Array), true);
    expect(result).toBe('section');
  });

  it('should handle RodzajTransportu', () => {
    generateTransport(mockTransport);
    expect(CommonFunctions.getRodzajTransportuString).toHaveBeenCalledWith(mockTransport.RodzajTransportu);
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Rodzaj transportu: ', 'Road');
  });

  it('should handle TransportInny', () => {
    const data = {
      ...mockTransport,
      RodzajTransportu: { _text: '' },
      TransportInny: { _text: '1' },
      OpisInnegoTransportu: { _text: 'Custom transport' },
    };
    generateTransport(data);
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Rodzaj transportu: ', 'Transport inny');
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Rodzaj transportu: ', 'Transport inny');
  });

  it('should handle Dane transportu', () => {
    generateTransport(mockTransport);
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Numer zlecenia transportu: ',
      mockTransport.NrZleceniaTransportu
    );
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Opis ładunku: ', 'Opis');
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Jednostka opakowania: ',
      mockTransport.JednostkaOpakowania
    );
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Data i godzina rozpoczęcia transportu: ',
      '2024-01-01 10:00'
    );
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Data i godzina zakończenia transportu: ',
      '2024-01-01 10:00'
    );
    expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Dane transportu', [0, 0, 0, 0]);
  });

  it('should call generatePrzewoznik', () => {
    generateTransport(mockTransport);
    expect(PrzewoznikModule.generatePrzewoznik).toHaveBeenCalledWith(mockTransport.Przewoznik);
  });

  it('should handle WysylkaZ and WysylkaDo', () => {
    generateTransport(mockTransport);
    expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Adres miejsca wysyłki', [0, 0, 0, 0]);
    expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith(
      'Adres miejsca docelowego, do którego został zlecony transport',
      [0, 0, 0, 0]
    );
  });

  it('should handle WysylkaPrzez', () => {
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
    };
    vi.mocked(PDFFunctions.getTable).mockReturnValue(data.WysylkaPrzez as any);
    generateTransport(data);
    expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Adres pośredni wysyłki', [0, 4, 0, 0]);
  });

  it('should call generateTwoColumns for main sections and wysylka', () => {
    generateTransport(mockTransport);
    expect(PDFFunctions.generateTwoColumns).toHaveBeenCalled();
  });

  it('should add header for Transport and Wysyłka', () => {
    generateTransport(mockTransport);
    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Transport');
    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Wysyłka');
  });

  it('should handle full integration with all fields', () => {
    const data = {
      ...mockTransport,
      LadunekInny: { _text: '1' },
      OpisInnegoLadunku: { _text: 'Custom cargo' },
    };
    generateTransport(data);
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Opis ładunku: ', 'Ładunek inny');
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Rodzaj transportu: ', 'Road');
  });
});
