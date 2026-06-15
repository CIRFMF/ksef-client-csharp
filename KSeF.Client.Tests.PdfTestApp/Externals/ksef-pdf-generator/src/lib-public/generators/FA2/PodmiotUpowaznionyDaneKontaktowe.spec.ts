import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generatePodmiotUpowaznionyDaneKontaktowe } from './PodmiotUpowaznionyDaneKontaktowe';
import type { PodmiotUpowaznionyDaneKontaktowe } from '../../types/fa2.types';
import FormatTyp from '../../../shared/enums/common.enum';

vi.mock('../../../shared/PDF-functions', () => ({
  formatText: vi.fn((text: string, type: FormatTyp) => ({ text, type })),
  getTable: vi.fn((data: any) => data),
  hasValue: vi.fn((val: any) => Boolean(val && val._text)),
  createLabelText: vi.fn((label: string, value: any) => ({ text: `${label}${value?._text ?? value}` })),
  verticalSpacing: vi.fn((amount: number) => ({ verticalSpacing: amount })),
}));

import {
  formatText,
  getTable,
  hasValue,
  createLabelText,
  verticalSpacing,
} from '../../../shared/PDF-functions';

describe(generatePodmiotUpowaznionyDaneKontaktowe.name, () => {
  const mockData: PodmiotUpowaznionyDaneKontaktowe[] = [
    { EmailPU: { _text: 'email@test.pl' }, TelefonPU: { _text: '123456789' } },
    { EmailPU: { _text: 'contact@test.pl' }, TelefonPU: { _text: '987654321' } },
  ] as any;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns empty array when daneKontaktoweSource is undefined', () => {
    const result = generatePodmiotUpowaznionyDaneKontaktowe(undefined);
    expect(result).toEqual([]);
  });

  it('adds formatted header for contact data', () => {
    const result = generatePodmiotUpowaznionyDaneKontaktowe(mockData);
    expect(formatText).toHaveBeenCalledWith('Dane kontaktowe', FormatTyp.Description);
    expect(result[0]).toEqual({ text: 'Dane kontaktowe', type: FormatTyp.Description });
  });

  it('maps each contact with EmailPU and TelefonPU', () => {
    const result = generatePodmiotUpowaznionyDaneKontaktowe(mockData);
    expect(getTable).toHaveBeenCalledWith(mockData);
    expect(hasValue).toHaveBeenCalledWith(mockData[0].EmailPU);
    expect(hasValue).toHaveBeenCalledWith(mockData[0].TelefonPU);
    expect(createLabelText).toHaveBeenCalledWith('E-mail: ', mockData[0].EmailPU);
    expect(createLabelText).toHaveBeenCalledWith('Tel.: ', mockData[0].TelefonPU);
    expect(verticalSpacing).toHaveBeenCalledWith(1);
    expect(result.flat().some((r) => (r as any).text.includes('email@test.pl'))).toBe(true);
    expect(result.flat().some((r) => (r as any).text.includes('123456789'))).toBe(true);
  });

  it('returns empty array when getTable returns empty array', () => {
    (getTable as any).mockReturnValueOnce([]);
    const result = generatePodmiotUpowaznionyDaneKontaktowe(mockData);
    expect(result).toEqual([]);
    expect(createLabelText).not.toHaveBeenCalled();
  });

  it('handles contacts with missing EmailPU or TelefonPU', () => {
    const data: PodmiotUpowaznionyDaneKontaktowe[] = [
      { EmailPU: undefined, TelefonPU: { _text: '111111111' } },
    ] as any;
    (hasValue as any).mockImplementation((val: any) => Boolean(val && val._text));
    const result = generatePodmiotUpowaznionyDaneKontaktowe(data);
    expect(createLabelText).toHaveBeenCalledWith('Tel.: ', data[0].TelefonPU);
    expect(createLabelText).not.toHaveBeenCalledWith('E-mail: ', undefined);
    expect(result.flat().some((c) => (c as any)?.text?.includes('E-mail'))).toBeFalsy();
    expect(result.flat().some((c) => (c as any)?.text?.includes('Tel.: 111111111'))).toBeTruthy();
  });
});
