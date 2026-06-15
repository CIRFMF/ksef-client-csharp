import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateDaneIdentyfikacyjneTPodmiot3Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot3Dto';
import type { Podmiot3DaneIdentyfikacyjne } from '../../types/fa2Podmiot3DaneIdentyfikacyjne.types';
import FormatTyp from '../../../shared/enums/common.enum';
import { createLabelText, createLabelTextArray, hasValue } from '../../../shared/PDF-functions';

vi.mock('../../../shared/PDF-functions', () => ({
  createLabelText: vi.fn((label: string, value: any, type: FormatTyp) => ({
    text: `${label}${value?._text ?? value}`,
    type,
  })),
  createLabelTextArray: vi.fn((arr: any[]) => ({
    text: arr.map((a) => a.value?._text ?? a.value).join(''),
  })),
  hasValue: vi.fn((val: any) => Boolean(val && val._text)),
}));

describe(generateDaneIdentyfikacyjneTPodmiot3Dto.name, () => {
  const baseData: Podmiot3DaneIdentyfikacyjne = {
    NIP: { _text: '1234567890' },
    Nazwa: { _text: 'Test Company' },
  } as any;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns empty array when daneIdentyfikacyjne is undefined', () => {
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(undefined);
    expect(result).toEqual([]);
  });

  it('uses NIP when provided', () => {
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(baseData);
    expect(hasValue).toHaveBeenCalledWith(baseData.NIP);
    expect(createLabelText).toHaveBeenCalledWith('NIP: ', baseData.NIP, FormatTyp.Default);
    expect(createLabelText).toHaveBeenCalledWith('Nazwa: ', baseData.Nazwa, FormatTyp.Default);
    expect(result.some((r) => (r as any).text.includes('NIP: 1234567890'))).toBe(true);
  });

  it('uses IDWew when NIP is not provided', () => {
    const data: Podmiot3DaneIdentyfikacyjne = {
      IDWew: { _text: 'INT-001' },
      Nazwa: { _text: 'Internal' },
    } as any;
    (hasValue as any).mockImplementation((val: any) => Boolean(val && val._text));
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(data);
    expect(createLabelText).toHaveBeenCalledWith('Identyfikator wewnętrzny: ', data.IDWew, FormatTyp.Default);
    expect(result.some((r) => (r as any).text.includes('Identyfikator wewnętrzny'))).toBe(true);
  });

  it('uses KodUE and NrVatUE when present', () => {
    const data: Podmiot3DaneIdentyfikacyjne = {
      KodUE: { _text: 'PL' },
      NrVatUE: { _text: '987654321' },
      Nazwa: { _text: 'VAT UE Test' },
    } as any;
    (hasValue as any).mockImplementation((val: any) => val?._text === 'PL');
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(data);
    expect(createLabelTextArray).toHaveBeenCalledWith([
      { value: 'Numer VAT-UE: ', formatTyp: FormatTyp.Label },
      { value: data.KodUE, formatTyp: FormatTyp.Default },
      { value: '' },
      { value: data.NrVatUE, formatTyp: FormatTyp.Default },
    ]);
    expect(result.some((r) => (r as any).text.includes('Numer VAT-UE'))).toBe(true);
  });

  it('uses NrID and KodKraju when present', () => {
    const data: Podmiot3DaneIdentyfikacyjne = {
      NrID: { _text: '999' },
      KodKraju: { _text: 'DE' },
      Nazwa: { _text: 'Other Tax ID' },
    } as any;
    (hasValue as any).mockImplementation((val: any) => val?._text === '999' || val?._text === 'DE');
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(data);
    expect(createLabelTextArray).toHaveBeenCalledWith([
      { value: 'Identyfikator podatkowy inny: ', formatTyp: FormatTyp.Label },
      { value: data.KodKraju, formatTyp: FormatTyp.Default },
      { value: '' },
      { value: data.NrID, formatTyp: FormatTyp.Default },
    ]);
    expect(result.some((r) => (r as any).text.includes('Identyfikator podatkowy inny'))).toBe(true);
  });

  it('adds "Brak identyfikatora" when BrakID._text is "1"', () => {
    const data: Podmiot3DaneIdentyfikacyjne = {
      BrakID: { _text: '1' },
      Nazwa: { _text: 'Missing ID' },
    } as any;
    (hasValue as any).mockReturnValue(false);
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(data);
    expect(createLabelText).toHaveBeenCalledWith('Brak identyfikatora', ' ', FormatTyp.Default);
    expect(result.some((r) => (r as any).text.includes('Brak identyfikatora'))).toBe(true);
  });

  it('adds Nazwa when present', () => {
    const data: Podmiot3DaneIdentyfikacyjne = { Nazwa: { _text: 'Only Name' } } as any;
    (hasValue as any).mockImplementation((val: any) => Boolean(val && val._text));
    const result = generateDaneIdentyfikacyjneTPodmiot3Dto(data);
    expect(createLabelText).toHaveBeenCalledWith('Nazwa: ', data.Nazwa, FormatTyp.Default);
    expect(result.some((r) => (r as any).text.includes('Nazwa: Only Name'))).toBe(true);
  });
});
