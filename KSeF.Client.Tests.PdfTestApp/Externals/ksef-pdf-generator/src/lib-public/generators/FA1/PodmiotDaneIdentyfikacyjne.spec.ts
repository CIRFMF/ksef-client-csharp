import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateDaneIdentyfikacyjne } from './PodmiotDaneIdentyfikacyjne';
import { Content } from 'pdfmake/interfaces';
import { DaneIdentyfikacyjneTPodmiot2Dto } from '../../types/fa2-additional-types';

vi.mock('../../../shared/PDF-functions', () => ({
  createLabelText: vi.fn((label, value) => ({
    text: `LABEL:${label}${typeof value === 'object' && value?._text ? value._text : (value ?? '')}`,
  })),
  getValue: vi.fn((val) => (val && val._text ? val._text : '')),
  hasValue: vi.fn((val) => !!(val && val._text)),
}));

describe('generateDaneIdentyfikacyjne', () => {
  beforeEach(() => vi.clearAllMocks());

  it('adds NIP label always', () => {
    const dane: DaneIdentyfikacyjneTPodmiot2Dto = { NIP: { _text: '1111111111' } };
    const result = generateDaneIdentyfikacyjne(dane);

    expect(result[0]).toEqual({ text: 'LABEL:NIP: 1111111111' });
  });

  it('skips name/last name label for empty values', () => {
    const dane: DaneIdentyfikacyjneTPodmiot2Dto = { NIP: { _text: 'xx' } };
    const result: Content[] = generateDaneIdentyfikacyjne(dane);

    expect(result.some((r: any): boolean => r.text === 'LABEL: ')).toBeFalsy();
  });

  it('adds Pełna nazwa label when present', () => {
    const dane: DaneIdentyfikacyjneTPodmiot2Dto = {
      NIP: { _text: '99' },
      PelnaNazwa: { _text: 'INSTRUMENTS INC.' },
    };
    const result = generateDaneIdentyfikacyjne(dane);

    expect(result).toEqual(expect.arrayContaining([{ text: 'LABEL:Pełna nazwa: INSTRUMENTS INC.' }]));
  });

  it('adds Nazwa handlowa when surname is present', () => {
    const dane: DaneIdentyfikacyjneTPodmiot2Dto = {
      NIP: { _text: '43210' },
      Nazwisko: { _text: 'Smith' },
      NazwaHandlowa: { _text: 'Smithy PLC' },
    };
    const result = generateDaneIdentyfikacyjne(dane);

    expect(result).toEqual(expect.arrayContaining([{ text: 'LABEL:Nazwa handlowa: Smithy PLC' }]));
  });

  it('returns array including only expected fields', () => {
    const dane: DaneIdentyfikacyjneTPodmiot2Dto = {
      NIP: { _text: '11' },
      PelnaNazwa: { _text: 'Best Corp' },
    };
    const result = generateDaneIdentyfikacyjne(dane);

    expect(result.length).toBe(2);
    expect(result).toEqual([{ text: 'LABEL:NIP: 11' }, { text: 'LABEL:Pełna nazwa: Best Corp' }]);
  });
});
