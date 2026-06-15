import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { DaneIdentyfikacyjne } from '../../types/fa2.types';
import { createLabelText } from '../../../shared/PDF-functions';
import { generateDaneIdentyfikacyjneTPodmiot1Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot1Dto';

vi.mock('../../../shared/PDF-functions', () => ({
  createLabelText: vi.fn((label: string, value: string) => ({ text: `${label}${value}` })),
}));

describe(generateDaneIdentyfikacyjneTPodmiot1Dto.name, () => {
  const mockData: DaneIdentyfikacyjne = {
    NIP: '1234567890',
    Nazwa: 'Test Company',
  } as DaneIdentyfikacyjne;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('calls createLabelText twice with correct arguments', () => {
    const result = generateDaneIdentyfikacyjneTPodmiot1Dto(mockData);
    expect(createLabelText).toHaveBeenCalledTimes(2);
    expect(createLabelText).toHaveBeenCalledWith('NIP: ', '1234567890');
    expect(createLabelText).toHaveBeenCalledWith('Nazwa: ', 'Test Company');
    expect(result).toEqual([{ text: 'NIP: 1234567890' }, { text: 'Nazwa: Test Company' }]);
  });

  it('handles empty values correctly', () => {
    const result = generateDaneIdentyfikacyjneTPodmiot1Dto({ NIP: '', Nazwa: '' } as DaneIdentyfikacyjne);
    expect(createLabelText).toHaveBeenCalledWith('NIP: ', '');
    expect(createLabelText).toHaveBeenCalledWith('Nazwa: ', '');
    expect(result).toEqual([{ text: 'NIP: ' }, { text: 'Nazwa: ' }]);
  });
});
