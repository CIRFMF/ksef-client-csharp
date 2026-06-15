import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateWarunkiTransakcji } from './WarunkiTransakcji';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { WarunkiTransakcji } from '../../types/fa1.types';
import * as TransportModule from './Transport';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createLabelText: vi.fn(),
  createSection: vi.fn(),
  createSubHeader: vi.fn(),
  formatText: vi.fn(),
  generateTwoColumns: vi.fn(),
  getContentTable: vi.fn(),
  getTable: vi.fn(),
}));

vi.mock('./Transport', () => ({
  generateTransport: vi.fn(),
}));

describe(generateWarunkiTransakcji.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('when warunkiTransakcji is undefined', () => {
    it('should return empty array', () => {
      const result = generateWarunkiTransakcji(undefined);

      expect(result).toEqual([]);
    });
  });

  describe('when warunkiTransakcji is defined', () => {
    const mockWarunkiTransakcji: WarunkiTransakcji = {
      Umowy: [],
      Zamowienia: [],
      NrPartiiTowaru: [],
      WalutaUmowna: { _text: '' },
      KursUmowny: { _text: '' },
      WarunkiDostawy: { _text: '' },
      PodmiotPosredniczacy: { _text: '0' },
    } as any;

    beforeEach(() => {
      vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
      vi.mocked(PDFFunctions.createHeader).mockReturnValue('header' as any);
      vi.mocked(PDFFunctions.createSection).mockReturnValue('section' as any);
      vi.mocked(PDFFunctions.createLabelText).mockReturnValue('label' as any);
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: [],
      });
    });

    it('should call createHeader with "Warunki transakcji"', () => {
      generateWarunkiTransakcji(mockWarunkiTransakcji);

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Warunki transakcji', [0, 8, 0, 4]);
    });

    it('should call getTable for Umowy, Zamowienia and NrPartiiTowaru', () => {
      const data = {
        ...mockWarunkiTransakcji,
        Umowy: [{ DataUmowy: { _text: '2024-01-01' } }],
        Zamowienia: [{ DataZamowienia: { _text: '2024-01-02' } }],
        NrPartiiTowaru: [{ _text: 'BATCH001' }],
      } as any;

      generateWarunkiTransakcji(data);

      expect(PDFFunctions.getTable).toHaveBeenCalledWith(data.Umowy);
      expect(PDFFunctions.getTable).toHaveBeenCalledWith(data.Zamowienia);
      expect(PDFFunctions.getTable).toHaveBeenCalledWith(data.NrPartiiTowaru);
    });

    it('should call createSection with table array', () => {
      generateWarunkiTransakcji(mockWarunkiTransakcji);

      expect(PDFFunctions.createSection).toHaveBeenCalledWith(expect.any(Array), true);
    });

    it('should return result from createSection', () => {
      const mockSection = { section: 'test' };
      vi.mocked(PDFFunctions.createSection).mockReturnValue(mockSection as any);

      const result = generateWarunkiTransakcji(mockWarunkiTransakcji);

      expect(result).toEqual(mockSection);
    });

    describe('umowy section', () => {
      it('should create umowy section when umowy exist', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Umowy: [{ DataUmowy: { _text: '2024-01-01' }, NrUmowy: { _text: 'U001' } }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.Umowy)
            return [
              {
                DataUmowy: { _text: '2024-01-01' },
                NrUmowy: { _text: 'U001' },
              },
            ] as any;
          return [];
        });

        vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
          content: { table: {} } as any,
          fieldsWithValue: ['DataUmowy', 'NrUmowy'],
        });

        vi.mocked(PDFFunctions.createSubHeader).mockReturnValue('subheader' as any);

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Umowa');
        expect(PDFFunctions.getContentTable).toHaveBeenCalledWith(
          expect.arrayContaining([
            expect.objectContaining({ name: 'DataUmowy', title: 'Data umowy' }),
            expect.objectContaining({ name: 'NrUmowy', title: 'Numer umowy' }),
          ]),
          expect.any(Array),
          '*'
        );
      });

      it('should not create umowy section when umowy are empty', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
        vi.mocked(PDFFunctions.createSubHeader).mockClear();

        generateWarunkiTransakcji(mockWarunkiTransakcji);

        expect(PDFFunctions.createSubHeader).not.toHaveBeenCalledWith('Umowa');
      });
    });

    describe('zamowienia section', () => {
      it('should create zamowienia section when zamowienia exist', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Zamowienia: [{ DataZamowienia: { _text: '2024-01-02' }, NrZamowienia: { _text: 'Z001' } }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.Zamowienia)
            return [
              {
                DataZamowienia: { _text: '2024-01-02' },
                NrZamowienia: { _text: 'Z001' },
              },
            ] as any;
          return [];
        });

        vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
          content: { table: {} } as any,
          fieldsWithValue: ['DataZamowienia', 'NrZamowienia'],
        });

        vi.mocked(PDFFunctions.createSubHeader).mockReturnValue('subheader' as any);

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Zamówienie');
        expect(PDFFunctions.getContentTable).toHaveBeenCalledWith(
          expect.arrayContaining([
            expect.objectContaining({ name: 'DataZamowienia', title: 'Data zamówienia' }),
            expect.objectContaining({ name: 'NrZamowienia', title: 'Numer zamówienia' }),
          ]),
          expect.any(Array),
          '*'
        );
      });

      it('should not create zamowienia section when zamowienia are empty', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
        vi.mocked(PDFFunctions.createSubHeader).mockClear();

        generateWarunkiTransakcji(mockWarunkiTransakcji);

        expect(PDFFunctions.createSubHeader).not.toHaveBeenCalledWith('Zamówienie');
      });

      it('should not create zamowienia section when fieldsWithValue is empty', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Zamowienia: [{ DataZamowienia: { _text: '' } }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.Zamowienia) return [{ DataZamowienia: { _text: '' } }] as any;
          return [];
        });

        vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
          content: { table: {} } as any,
          fieldsWithValue: [],
        });

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.generateTwoColumns).not.toHaveBeenCalled();
      });
    });

    describe('two columns generation', () => {
      it('should generate two columns when umowy exist', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Umowy: [{ DataUmowy: { _text: '2024-01-01' } }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.Umowy) return [{ DataUmowy: { _text: '2024-01-01' } }] as any;
          return [];
        });

        vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
          content: { table: 'umowy' } as any,
          fieldsWithValue: ['DataUmowy'],
        });

        vi.mocked(PDFFunctions.createSubHeader).mockReturnValue('subheader' as any);
        vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('twoColumns' as any);

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith(
          expect.arrayContaining(['subheader', { table: 'umowy' }]),
          []
        );
      });

      it('should generate two columns when zamowienia exist', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Zamowienia: [{ DataZamowienia: { _text: '2024-01-02' } }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.Zamowienia) return [{ DataZamowienia: { _text: '2024-01-02' } }] as any;
          return [];
        });

        vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
          content: { table: 'zamowienia' } as any,
          fieldsWithValue: ['DataZamowienia'],
        });

        vi.mocked(PDFFunctions.createSubHeader).mockReturnValue('subheader' as any);
        vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('twoColumns' as any);

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith(
          [],
          expect.arrayContaining(['subheader', { table: 'zamowienia' }])
        );
      });

      it('should not generate two columns when both umowy and zamowienia are empty', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([]);

        generateWarunkiTransakcji(mockWarunkiTransakcji);

        expect(PDFFunctions.generateTwoColumns).not.toHaveBeenCalled();
      });
    });

    describe('waluta umowna section', () => {
      it('should create waluta umowna section when WalutaUmowna exists', () => {
        const data = {
          ...mockWarunkiTransakcji,
          WalutaUmowna: { _text: 'EUR' },
          KursUmowny: { _text: '' },
        } as any;

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Waluta umowna i kurs umowny', [0, 8, 0, 4]);
        expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Waluta umowna: ', data.WalutaUmowna);
        expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Kurs umowny: ', data.KursUmowny);
      });

      it('should create waluta umowna section when KursUmowny exists', () => {
        const data = {
          ...mockWarunkiTransakcji,
          WalutaUmowna: { _text: '' },
          KursUmowny: { _text: '4.50' },
        } as any;

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Waluta umowna i kurs umowny', [0, 8, 0, 4]);
      });

      it('should not create waluta umowna section when both are empty', () => {
        const data = {
          ...mockWarunkiTransakcji,
          WalutaUmowna: { _text: '' },
          KursUmowny: { _text: '' },
        } as any;

        vi.mocked(PDFFunctions.createHeader).mockClear();

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.createHeader).toHaveBeenCalledTimes(1);
        expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Warunki transakcji', [0, 8, 0, 4]);
      });
    });

    describe('partia towaru section', () => {
      it('should create partia towaru section when NrPartiiTowaru exists', () => {
        const data = {
          ...mockWarunkiTransakcji,
          NrPartiiTowaru: [{ _text: 'BATCH001' }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.NrPartiiTowaru) return [{ _text: 'BATCH001' }] as any;
          return [];
        });

        vi.mocked(PDFFunctions.getContentTable).mockReturnValueOnce({
          content: { table: 'partia' } as any,
          fieldsWithValue: ['_text'],
        });

        vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('twoColumns' as any);

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.getContentTable).toHaveBeenCalledWith(
          expect.arrayContaining([expect.objectContaining({ name: '', title: 'Numer partii towaru' })]),
          expect.any(Array),
          '*',
          [0, 4]
        );
        expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith({ table: 'partia' }, ' ');
      });

      it('should not create partia towaru section when NrPartiiTowaru is empty', () => {
        vi.mocked(PDFFunctions.getTable).mockReturnValue([]);

        const callCountBefore = vi.mocked(PDFFunctions.generateTwoColumns).mock.calls.length;

        generateWarunkiTransakcji(mockWarunkiTransakcji);

        expect(vi.mocked(PDFFunctions.generateTwoColumns).mock.calls.length).toBe(callCountBefore);
      });
    });

    describe('warunki dostawy', () => {
      it('should add warunki dostawy label', () => {
        const data = {
          ...mockWarunkiTransakcji,
          WarunkiDostawy: { _text: 'FOB' },
        } as any;

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
          'Warunki dostawy towarów: ',
          data.WarunkiDostawy,
          FormatTyp.MarginTop4
        );
      });
    });

    describe('podmiot posredniczacy', () => {
      it('should add podmiot posredniczacy info when value is "1"', () => {
        const data = {
          ...mockWarunkiTransakcji,
          PodmiotPosredniczacy: { _text: '1' },
        } as any;

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.formatText).toHaveBeenCalledWith(
          expect.stringContaining('Dostawa dokonana przez podmiot, o którym mowa w art. 22 ust. 2d ustawy'),
          [FormatTyp.Label, FormatTyp.MarginTop4]
        );
      });

      it('should not add podmiot posredniczacy info when value is not "1"', () => {
        const data = {
          ...mockWarunkiTransakcji,
          PodmiotPosredniczacy: { _text: '0' },
        } as any;

        vi.mocked(PDFFunctions.formatText).mockClear();

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.formatText).not.toHaveBeenCalled();
      });

      it('should not add podmiot posredniczacy info when value is empty', () => {
        const data = {
          ...mockWarunkiTransakcji,
          PodmiotPosredniczacy: { _text: '' },
        } as any;

        vi.mocked(PDFFunctions.formatText).mockClear();

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.formatText).not.toHaveBeenCalled();
      });
    });

    describe('transport section', () => {
      it('should call generateTransport for each transport item', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Transport: [{ RodzajTransportu: { _text: 'Road' } }, { RodzajTransportu: { _text: 'Air' } }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.Transport) {
            return [{ RodzajTransportu: { _text: 'Road' } }, { RodzajTransportu: { _text: 'Air' } }] as any;
          }
          return [];
        });

        vi.mocked(TransportModule.generateTransport).mockReturnValue('transport' as any);

        generateWarunkiTransakcji(data);

        expect(TransportModule.generateTransport).toHaveBeenCalledTimes(2);
        expect(TransportModule.generateTransport).toHaveBeenCalledWith(
          {
            RodzajTransportu: { _text: 'Road' },
          },
          1
        );
        expect(TransportModule.generateTransport).toHaveBeenCalledWith(
          {
            RodzajTransportu: { _text: 'Air' },
          },
          2
        );
      });

      it('should not call generateTransport when Transport is undefined', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Transport: undefined,
        } as any;

        generateWarunkiTransakcji(data);

        expect(TransportModule.generateTransport).not.toHaveBeenCalled();
      });

      it('should not call generateTransport when Transport is empty', () => {
        const data = {
          ...mockWarunkiTransakcji,
          Transport: [],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockReturnValue([]);

        generateWarunkiTransakcji(data);

        expect(TransportModule.generateTransport).not.toHaveBeenCalled();
      });
    });

    describe('complete integration', () => {
      it('should handle all sections when fully populated', () => {
        const data = {
          Umowy: [{ DataUmowy: { _text: '2024-01-01' }, NrUmowy: { _text: 'U001' } }],
          Zamowienia: [{ DataZamowienia: { _text: '2024-01-02' }, NrZamowienia: { _text: 'Z001' } }],
          NrPartiiTowaru: [{ _text: 'BATCH001' }],
          WalutaUmowna: { _text: 'EUR' },
          KursUmowny: { _text: '4.50' },
          WarunkiDostawy: { _text: 'FOB' },
          PodmiotPosredniczacy: { _text: '1' },
          Transport: [{ RodzajTransportu: { _text: 'Road' } }],
        } as any;

        vi.mocked(PDFFunctions.getTable).mockImplementation((field: any) => {
          if (field === data.Umowy) return [{ DataUmowy: { _text: '2024-01-01' } }] as any;
          if (field === data.Zamowienia) return [{ DataZamowienia: { _text: '2024-01-02' } }] as any;
          if (field === data.NrPartiiTowaru) return [{ _text: 'BATCH001' }] as any;
          if (field === data.Transport) return [{ RodzajTransportu: { _text: 'Road' } }] as any;
          return [];
        });

        vi.mocked(PDFFunctions.getContentTable)
          .mockReturnValueOnce({ content: { table: 'umowy' } as any, fieldsWithValue: ['DataUmowy'] })
          .mockReturnValueOnce({
            content: { table: 'zamowienia' } as any,
            fieldsWithValue: ['DataZamowienia'],
          })
          .mockReturnValueOnce({ content: { table: 'partia' } as any, fieldsWithValue: ['_text'] });

        generateWarunkiTransakcji(data);

        expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Warunki transakcji', [0, 8, 0, 4]);
        expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Waluta umowna i kurs umowny', [0, 8, 0, 4]);
        expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Umowa');
        expect(PDFFunctions.createSubHeader).toHaveBeenCalledWith('Zamówienie');
        expect(PDFFunctions.formatText).toHaveBeenCalled();
        expect(TransportModule.generateTransport).toHaveBeenCalled();
        expect(PDFFunctions.createSection).toHaveBeenCalled();
      });
    });
  });
});
