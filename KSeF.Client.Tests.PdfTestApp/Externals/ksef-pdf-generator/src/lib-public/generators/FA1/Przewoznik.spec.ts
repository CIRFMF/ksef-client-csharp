import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePrzewoznik } from './Przewoznik';
import { Przewoznik } from '../../types/fa1.types';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label) => [{ text: `HEADER:${label}` }]),
  generateTwoColumns: vi.fn((left, right, margin) => ({ left, right, margin, type: '2COL' })),
}));

vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn(() => ({ adr: 'AdresPrzewoznika' })),
}));

vi.mock('./PodmiotDaneIdentyfikacyjne', () => ({
  generateDaneIdentyfikacyjne: vi.fn(() => [{ id: 'DaneIdentyfikacyjne' }]),
}));

describe('generatePrzewoznik', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty array if przewoznik is undefined', () => {
    expect(generatePrzewoznik(undefined)).toEqual([]);
  });

  it('returns header and twoColumns for valid przewoznik', () => {
    const przewoznik: Przewoznik = {
      DaneIdentyfikacyjne: { NIP: { _text: '12345' } },
      AdresPrzewoznika: { AdresPol: { Miasto: { _text: 'Warsaw' } } },
    };
    const result: any = generatePrzewoznik(przewoznik);

    expect(result[0]).toEqual({ text: 'HEADER:Przewoźnik' });
  });
});
