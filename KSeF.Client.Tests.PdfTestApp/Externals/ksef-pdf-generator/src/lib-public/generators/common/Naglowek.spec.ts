import { describe, expect, it } from 'vitest';
import { TRodzajFaktury } from '../../../shared/consts/const';
import { Fa as Fa1 } from '../../types/fa1.types';
import { Fa as Fa3 } from '../../types/fa3.types';
import { generateNaglowek } from './Naglowek';

describe('generateNaglowek', () => {
  it('generates header for collective correction invoice', () => {
    const fa: Fa3 = {
      RodzajFaktury: { _text: TRodzajFaktury.KOR },
      OkresFaKorygowanej: '2025-01',
      P_2: { _text: 'FKZ/2025/05' },
    } as any;

    const result = generateNaglowek(fa);

    expect(
      result.some(
        (c) =>
          typeof c === 'object' &&
          c !== null &&
          'text' in c &&
          typeof (c as any).text === 'string' &&
          (c as any).text.includes('Faktura korygująca zbiorcza')
      )
    ).toBe(true);
  });

  it('generates header with ??? for unknown invoice type', () => {
    const fa: Fa1 = {
      RodzajFaktury: { _text: 'UNKNOWN' },
      P_2: { _text: 'FUNK/2025/99' },
    } as any;

    const result = generateNaglowek(fa);

    expect(
      result.some(
        (c) =>
          typeof c === 'object' &&
          c !== null &&
          'text' in c &&
          typeof (c as any).text === 'string' &&
          (c as any).text.includes('???')
      )
    ).toBe(true);
  });

  it('generates header even when fa is undefined', () => {
    const result = generateNaglowek();

    expect(Array.isArray(result)).toBe(true);
    expect(result.length).toBeGreaterThan(0);
  });
});
