import { describe, expect, it, vi } from 'vitest';
import { generateDaneIdentyfikacyjneTPodmiot3Dto } from './Podmiot3Podmiot2k';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((text: string) => [`header:${text}`]),
  createLabelText: vi.fn((label: string, value: any) => [`label:${label}${value}`]),
  generateTwoColumns: vi.fn((col1: any, col2: any) => ({ columns: [col1, col2] })),
  getTable: vi.fn((data: any) => (data ? ['table:data'] : [])),
  hasValue: vi.fn((val: any) => val !== undefined && val !== null && val !== ''),
}));

vi.mock('../../../shared/generators/common/functions', () => ({
  getRolaString: vi.fn((rola: any) => `rola:${rola}`),
}));

vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn((adres: any, label?: string) => [`adres:${label ?? ''}`]),
}));

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot2Dto', () => ({
  generateDaneIdentyfikacyjneTPodmiot2Dto: vi.fn((data: any) => [`dane:${data}`]),
}));

vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn((data: any) => [`kontakt:${data}`]),
}));

describe(generateDaneIdentyfikacyjneTPodmiot3Dto.name, () => {
  it('should return empty array when input is undefined', () => {
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(undefined, 0);
    expect(result).toEqual([]);
  });

  it('should generate full structure with all fields', () => {
    const podmiotDto = {
      fakturaPodmiotNDto: {
        NrEORI: 'EORI123',
        Rola: 'R',
        OpisRoli: 'OpisRoli',
        Udzial: 100,
        DaneKontaktowe: [{ email: 'test@test.com' }],
        NrKlienta: 'K123',
        IDNabywcy: 'ID1',
        DaneIdentyfikacyjne: 'Dane1',
        Adres: 'Adres1',
        AdresKoresp: 'AdresKoresp1',
      },
      podmiot2KDto: {
        IDNabywcy: 'ID2',
        DaneIdentyfikacyjne: 'Dane2',
        Adres: 'Adres2',
      },
    };

    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(podmiotDto as any, 0);

    expect(result[0]).toEqual('header:Podmiot inny 1');

    expect(result.some((r: any) => Array.isArray(r) && r[0].startsWith('label:'))).toBe(true);

    expect(result.some((r: any) => r.columns)).toBe(true);
  });

  it('should handle missing optional fields gracefully', () => {
    const podmiotDto = {
      fakturaPodmiotNDto: {},
      podmiot2KDto: {},
    };

    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(podmiotDto as any, 1);

    expect(result[0]).toEqual('header:Podmiot inny 2');
  });
});
