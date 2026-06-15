import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generatePrzewoznik } from './Przewoznik';
import * as PDFFunctions from '../../../shared/PDF-functions';
import { Przewoznik } from '../../types/fa2.types';
import * as PodmiotAdresModule from './PodmiotAdres';
import * as PodmiotDaneIdentyfikacyjneModule from './PodmiotDaneIdentyfikacyjneTPodmiot2Dto';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  generateTwoColumns: vi.fn(),
}));

vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn(),
}));

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot2Dto', () => ({
  generateDaneIdentyfikacyjneTPodmiot2Dto: vi.fn(),
}));

describe(generatePrzewoznik.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockPrzewoznik: Przewoznik = {
    DaneIdentyfikacyjne: {
      NazwaPelna: { _text: 'Transport Company' },
      NIP: { _text: '1234567890' },
    },
    AdresPrzewoznika: {
      AdresL1: { _text: 'Street 1' },
      AdresL2: { _text: 'City 1' },
      KodKraju: { _text: 'PL' },
    },
  } as any;

  beforeEach(() => {
    vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header'] as any);
    vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns' as any);
    vi.mocked(PodmiotAdresModule.generatePodmiotAdres).mockReturnValue('adres' as any);
    vi.mocked(PodmiotDaneIdentyfikacyjneModule.generateDaneIdentyfikacyjneTPodmiot2Dto).mockReturnValue(
      'dane' as any
    );
  });

  describe('when przewoznik is undefined', () => {
    it('should return empty array', () => {
      const result = generatePrzewoznik(undefined);

      expect(result).toEqual([]);
    });

    it('should not call any functions', () => {
      generatePrzewoznik(undefined);

      expect(PDFFunctions.createHeader).not.toHaveBeenCalled();
      expect(PDFFunctions.generateTwoColumns).not.toHaveBeenCalled();
      expect(PodmiotAdresModule.generatePodmiotAdres).not.toHaveBeenCalled();
      expect(PodmiotDaneIdentyfikacyjneModule.generateDaneIdentyfikacyjneTPodmiot2Dto).not.toHaveBeenCalled();
    });
  });

  describe('when przewoznik is defined', () => {
    it('should call createHeader with "Przewoźnik"', () => {
      generatePrzewoznik(mockPrzewoznik);

      expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Przewoźnik');
    });

    it('should call generateDaneIdentyfikacyjneTPodmiot2Dto with DaneIdentyfikacyjne', () => {
      generatePrzewoznik(mockPrzewoznik);

      expect(PodmiotDaneIdentyfikacyjneModule.generateDaneIdentyfikacyjneTPodmiot2Dto).toHaveBeenCalledWith(
        mockPrzewoznik.DaneIdentyfikacyjne
      );
    });

    it('should call generatePodmiotAdres with correct parameters', () => {
      generatePrzewoznik(mockPrzewoznik);

      expect(PodmiotAdresModule.generatePodmiotAdres).toHaveBeenCalledWith(
        mockPrzewoznik.AdresPrzewoznika,
        'Adres przewoźnika',
        true,
        [0, 0, 0, 0]
      );
    });

    it('should call generateTwoColumns with results from other functions', () => {
      vi.mocked(PodmiotDaneIdentyfikacyjneModule.generateDaneIdentyfikacyjneTPodmiot2Dto).mockReturnValue(
        'dane-id' as any
      );
      vi.mocked(PodmiotAdresModule.generatePodmiotAdres).mockReturnValue('adres-content' as any);

      generatePrzewoznik(mockPrzewoznik);

      expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith('dane-id', 'adres-content', [0, 0, 0, 8]);
    });

    it('should return array with header and columns', () => {
      vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header-content'] as any);
      vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns-content' as any);

      const result = generatePrzewoznik(mockPrzewoznik) as any[];

      expect(Array.isArray(result)).toBe(true);
      expect(result).toContain('header-content');
      expect(result[1]).toEqual(['columns-content']);
    });

    it('should have correct structure with nested array', () => {
      const result = generatePrzewoznik(mockPrzewoznik) as any[];

      expect(result.length).toBe(2);
      expect(Array.isArray(result[1])).toBe(true);
    });
  });

  describe('integration', () => {
    it('should call all functions in correct order', () => {
      const callOrder: string[] = [];

      vi.mocked(PDFFunctions.createHeader).mockImplementation(() => {
        callOrder.push('createHeader');
        return ['header'] as any;
      });

      vi.mocked(PodmiotDaneIdentyfikacyjneModule.generateDaneIdentyfikacyjneTPodmiot2Dto).mockImplementation(
        () => {
          callOrder.push('generateDaneIdentyfikacyjneTPodmiot2Dto');
          return 'dane' as any;
        }
      );

      vi.mocked(PodmiotAdresModule.generatePodmiotAdres).mockImplementation(() => {
        callOrder.push('generatePodmiotAdres');
        return 'adres' as any;
      });

      vi.mocked(PDFFunctions.generateTwoColumns).mockImplementation(() => {
        callOrder.push('generateTwoColumns');
        return 'columns' as any;
      });

      generatePrzewoznik(mockPrzewoznik);

      expect(callOrder).toEqual([
        'createHeader',
        'generateDaneIdentyfikacyjneTPodmiot2Dto',
        'generatePodmiotAdres',
        'generateTwoColumns',
      ]);
    });

    it('should handle przewoznik with minimal data', () => {
      const minimalPrzewoznik: Przewoznik = {
        DaneIdentyfikacyjne: {},
        AdresPrzewoznika: {},
      } as any;

      const result = generatePrzewoznik(minimalPrzewoznik) as any[];

      expect(Array.isArray(result)).toBe(true);
      expect(result.length).toBeGreaterThan(0);
    });
  });
});
