import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import type { Podmiot1DaneKontaktowe } from '../../types/fa2.types';
import { createLabelText, getTable } from '../../../shared/PDF-functions';

vi.mock('../../../shared/PDF-functions', () => ({
  createLabelText: vi.fn((label: string, value: any) => ({ text: `${label}${value?._text ?? value}` })),
  getTable: vi.fn((data: any) => data),
}));

describe(generateDaneKontaktowe.name, () => {
  const mockData: Podmiot1DaneKontaktowe[] = [
    { Email: { _text: 'test@example.com' }, Telefon: { _text: '123456789' } },
    { Email: { _text: 'info@example.com' }, Telefon: { _text: '987654321' } },
  ] as any;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns mapped Content array when daneKontaktowe is provided', () => {
    const result = generateDaneKontaktowe(mockData);
    expect(getTable).toHaveBeenCalledWith(mockData);
    expect(createLabelText).toHaveBeenCalledWith('E-mail: ', mockData[0].Email);
    expect(createLabelText).toHaveBeenCalledWith('Tel.: ', mockData[0].Telefon);
    expect(Array.isArray(result)).toBe(true);
    expect(result?.length).toBe(2);
    expect(result?.[0]).toEqual([{ text: 'E-mail: test@example.com' }, { text: 'Tel.: 123456789' }]);
  });

  it('returns undefined when getTable returns undefined', () => {
    (getTable as any).mockReturnValueOnce(undefined);
    const result = generateDaneKontaktowe(mockData);
    expect(result).toBeUndefined();
    expect(createLabelText).not.toHaveBeenCalled();
  });

  it('handles empty daneKontaktowe array', () => {
    const result = generateDaneKontaktowe([]);
    expect(getTable).toHaveBeenCalledWith([]);
    expect(result).toEqual([]);
  });
});
