import { beforeEach, describe, expect, it, vi } from 'vitest';
import FormatTyp from '../../../shared/enums/common.enum';
import { createLabelText, createLabelTextArray, formatText } from '../../../shared/PDF-functions';
import { generateDaneIdentyfikacyjneTPodmiot2Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot2Dto';
import { DaneIdentyfikacyjneTPodmiot2Dto } from '../../types/fa2-additional-types';

vi.mock('../../../shared/PDF-functions', () => ({
  createLabelText: vi.fn((label: string, value: any) => ({ text: `${label}${value?._text ?? value}` })),
  createLabelTextArray: vi.fn((arr: any[]) => ({ text: arr.map((a) => a.value?._text ?? a.value).join('') })),
  formatText: vi.fn((text: string, type: FormatTyp) => ({ text, type })),
}));

describe(generateDaneIdentyfikacyjneTPodmiot2Dto.name, () => {
  const baseData: DaneIdentyfikacyjneTPodmiot2Dto = {
    NIP: { _text: '1234567890' },
    Nazwa: { _text: 'Test Company' },
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('always includes NIP and Nazwa', () => {
    const result = generateDaneIdentyfikacyjneTPodmiot2Dto(baseData);
    expect(createLabelText).toHaveBeenCalledWith('NIP: ', baseData.NIP);
    expect(createLabelText).toHaveBeenCalledWith('Nazwa: ', baseData.Nazwa);
    expect(result).toContainEqual({ text: 'NIP: 1234567890' });
    expect(result).toContainEqual({ text: 'Nazwa: Test Company' });
  });

  it('adds VAT-UE section when NrVatUE is present', () => {
    const data: DaneIdentyfikacyjneTPodmiot2Dto = {
      ...baseData,
      NrVatUE: { _text: '123' },
      KodUE: { _text: 'PL' },
    };
    const result = generateDaneIdentyfikacyjneTPodmiot2Dto(data);
    expect(createLabelTextArray).toHaveBeenCalledWith([
      { value: 'Numer VAT-UE: ', formatTyp: FormatTyp.Label },
      { value: data.KodUE, formatTyp: FormatTyp.Value },
      { value: ' ' },
      { value: data.NrVatUE, formatTyp: FormatTyp.Value },
    ]);
    expect(result.some((r: any) => r.text.includes('Numer VAT-UE'))).toBe(true);
  });

  it('adds KodKraju section when KodKraju is present', () => {
    const data: DaneIdentyfikacyjneTPodmiot2Dto = {
      ...baseData,
      KodKraju: { _text: 'DE' },
      NrID: { _text: '999999' },
    };
    const result = generateDaneIdentyfikacyjneTPodmiot2Dto(data);
    expect(createLabelTextArray).toHaveBeenCalledWith([
      { value: 'Identyfikator podatkowy inny: ', formatTyp: FormatTyp.Label },
      { value: data.KodKraju, formatTyp: FormatTyp.Value },
      { value: ' ' },
      { value: data.NrID, formatTyp: FormatTyp.Value },
    ]);
    expect(result.some((r: any) => (r as any).text.includes('Identyfikator podatkowy inny'))).toBe(true);
  });

  it('adds "Brak identyfikatora" when BrakID._text is "1"', () => {
    const data: DaneIdentyfikacyjneTPodmiot2Dto = {
      ...baseData,
      BrakID: { _text: '1' },
    };
    const result = generateDaneIdentyfikacyjneTPodmiot2Dto(data);
    expect(formatText).toHaveBeenCalledWith('Brak identyfikatora', FormatTyp.Label);
    expect(result.some((r) => (r as any).text === 'Brak identyfikatora')).toBe(true);
  });

  it('does not add extra sections when optional fields are missing', () => {
    const result = generateDaneIdentyfikacyjneTPodmiot2Dto(baseData);
    expect(createLabelTextArray).not.toHaveBeenCalled();
    expect(formatText).not.toHaveBeenCalled();
    expect(result).toHaveLength(2);
  });
});
