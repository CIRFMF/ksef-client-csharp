import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import { FP } from '../../types/fa1.types';

vi.mock('../../../shared/PDF-functions', () => ({
  createLabelText: vi.fn((label: string, value: any) => ({
    text: `LABEL:${label}${typeof value === 'object' && value?._text ? value._text : (value ?? '')}`,
  })),
}));

describe('generateDaneKontaktowe', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty array for no input', () => {
    expect(generateDaneKontaktowe()).toEqual([]);
  });

  it('returns only email entry when only email is present', () => {
    const email: FP = { _text: 'aa@bb.cc' };
    const result = generateDaneKontaktowe(email, undefined);

    expect(result).toEqual([{ text: 'LABEL:Email: aa@bb.cc' }]);
  });

  it('returns only tel entry when only single phone is present', () => {
    const tel = [{ _text: '123456789' }];
    const result = generateDaneKontaktowe(undefined, tel);

    expect(result).toEqual([{ text: 'LABEL:Tel.: [object Object]\n' }]);
  });

  it('returns email and all telephones in order', () => {
    const email: FP = { _text: 'z@x.io' };
    const telo = [{ _text: '101' }, { _text: '202' }];
    const result = generateDaneKontaktowe(email, telo);

    expect(result[0]).toEqual({ text: 'LABEL:Email: z@x.io' });
    expect(result.length).toBe(3);
    expect(result[1]).toEqual({ text: 'LABEL:Tel.: [object Object]\n' });
    expect(result[2]).toEqual({ text: 'LABEL:Tel.: [object Object]\n' });
  });

  it('handles multiple telephones', () => {
    const tel = [{ _text: '666111222' }, { _text: '555222111' }];
    const result = generateDaneKontaktowe(undefined, tel);

    expect(result.length).toBe(2);
  });
});
