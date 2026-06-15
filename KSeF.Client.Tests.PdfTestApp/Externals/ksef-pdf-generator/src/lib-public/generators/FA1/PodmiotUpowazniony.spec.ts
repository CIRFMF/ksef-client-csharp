vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((label) => [{ text: `HEADER:${label}` }]),
  createLabelText: vi.fn((label, value) => ({
    text: `LABEL:${label}${value && value._text ? value._text : (value ?? '')}`,
  })),
  formatText: vi.fn((txt, _args) => ({ text: `FMT:${txt}` })),
  getTable: vi.fn((arr) => arr || []),
  getValue: vi.fn((val) => (val && val._text ? val._text : '')),
  hasValue: vi.fn((val) => Boolean(val && val._text)),
  generateTwoColumns: vi.fn((left, right) => ({ type: '2COL', left, right })),
}));

vi.mock('../../../shared/generators/common/functions', () => ({
  getRolaUpowaznionegoString: vi.fn(() => 'Rola string'),
}));
vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn((adres, label) => ({ adr: label })),
}));
vi.mock('./PodmiotDaneIdentyfikacyjne', () => ({
  generateDaneIdentyfikacyjne: vi.fn(() => [{ id: 'ID' }]),
}));
vi.mock('./PodmiotDaneKontaktowe', () => ({
  generateDaneKontaktowe: vi.fn(() => ({ contact: 'KONTAKT' })),
}));

import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePodmiotUpowazniony } from './PodmiotUpowazniony';
import { PodmiotUpowazniony } from '../../types/fa1.types';

describe(generatePodmiotUpowazniony.name, () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty if input undefined', () => {
    expect(generatePodmiotUpowazniony(undefined)).toEqual([]);
  });

  it('renders header and basic role and EORI if present', () => {
    const podmiot: PodmiotUpowazniony = {
      RolaPU: { _text: 'X' },
      NrEORI: { _text: 'EORI123' },
    };
    const res = generatePodmiotUpowazniony(podmiot);
    expect(res[0]).toEqual({ text: 'HEADER:Podmiot upoważniony' });
    expect(res.some((r: any) => r.type === '2COL')).toBe(true);
  });
});
