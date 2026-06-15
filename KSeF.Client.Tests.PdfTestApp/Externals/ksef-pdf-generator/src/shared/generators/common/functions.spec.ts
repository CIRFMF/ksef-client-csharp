import { describe, expect, it } from 'vitest';

import {
  FA1RolaPodmiotu3,
  FA2RolaPodmiotu3,
  FA3RolaPodmiotu3,
  FormaPlatnosci,
  RodzajTransportu,
  TRolaPodmiotuUpowaznionegoFA1,
  TRolaPodmiotuUpowaznionegoFA2,
  TRolaPodmiotuUpowaznionegoFA3,
  TypRachunkowWlasnych,
} from '../../consts/const';
import {
  formatDateTime,
  getDateTimeWithoutSeconds,
  getFormaPlatnosciString,
  getRodzajTransportuString,
  getRolaString,
  getRolaUpowaznionegoString,
  getTypRachunkowWlasnych,
} from './functions';

describe('getRolaString', () => {
  it('returns empty string if rola undefined or _text missing', () => {
    expect(getRolaString(undefined, 1)).toBe('');
    expect(getRolaString({} as any, 1)).toBe('');
  });

  it('returns correct string for FA=1', () => {
    const key = Object.keys(FA1RolaPodmiotu3)[0];
    const expected = FA1RolaPodmiotu3[key as keyof typeof FA1RolaPodmiotu3].split('-')[0];

    expect(getRolaString({ _text: key } as any, 1)).toBe(expected);
  });

  it('returns correct string for FA=2', () => {
    const key = Object.keys(FA2RolaPodmiotu3)[0];
    const expected = FA2RolaPodmiotu3[key as keyof typeof FA2RolaPodmiotu3].split('-')[0];

    expect(getRolaString({ _text: key } as any, 2)).toBe(expected);
  });

  it('returns correct string for FA=3', () => {
    const key = Object.keys(FA3RolaPodmiotu3)[0];
    const expected = FA3RolaPodmiotu3[key as keyof typeof FA3RolaPodmiotu3].split('-')[0];

    expect(getRolaString({ _text: key } as any, 3)).toBe(expected);
  });
});

describe('getRolaUpowaznionegoString', () => {
  it('returns empty string if rola undefined or _text missing', () => {
    expect(getRolaUpowaznionegoString(undefined, 1)).toBe('');
    expect(getRolaUpowaznionegoString({} as any, 1)).toBe('');
  });

  it('returns correct string for FA=1', () => {
    const key = Object.keys(TRolaPodmiotuUpowaznionegoFA1)[0];
    const expected = TRolaPodmiotuUpowaznionegoFA1[key].split('-')[0];

    expect(getRolaUpowaznionegoString({ _text: key } as any, 1)).toBe(expected);
  });

  it('returns correct string for FA=2', () => {
    const key = Object.keys(TRolaPodmiotuUpowaznionegoFA2)[0];
    const expected = TRolaPodmiotuUpowaznionegoFA2[key].split('-')[0];

    expect(getRolaUpowaznionegoString({ _text: key } as any, 2)).toBe(expected);
  });

  it('returns correct string for FA=3', () => {
    const key = Object.keys(TRolaPodmiotuUpowaznionegoFA3)[0];
    const expected = TRolaPodmiotuUpowaznionegoFA3[key].split('-')[0];

    expect(getRolaUpowaznionegoString({ _text: key } as any, 3)).toBe(expected);
  });
});

describe('getFormaPlatnosciString', () => {
  it('returns empty string if undefined or no _text', () => {
    expect(getFormaPlatnosciString(undefined)).toBe('');
  });

  it('returns correct string for known key', () => {
    const key = Object.keys(FormaPlatnosci)[0];
    const expected = FormaPlatnosci[key as keyof typeof FormaPlatnosci];

    expect(getFormaPlatnosciString({ _text: key } as any)).toBe(expected);
  });
});

describe('getRodzajTransportuString', () => {
  it('returns empty string if undefined or no _text', () => {
    expect(getRodzajTransportuString(undefined)).toBe('');
  });

  it('returns correct string for known key', () => {
    const key = Object.keys(RodzajTransportu)[0];
    const expected = RodzajTransportu[key as keyof typeof RodzajTransportu];

    expect(getRodzajTransportuString({ _text: key } as any)).toBe(expected);
  });
});

describe('getTypRachunkowWlasnych', () => {
  it('returns empty string if undefined or no _text', () => {
    expect(getTypRachunkowWlasnych(undefined)).toBe('');
  });

  it('returns correct string for known key', () => {
    const key = Object.keys(TypRachunkowWlasnych)[0];
    const expected = TypRachunkowWlasnych[key as keyof typeof TypRachunkowWlasnych];

    expect(getTypRachunkowWlasnych({ _text: key } as any)).toBe(expected);
  });
});

describe('formatDateTime', () => {
  it('returns empty string for empty input', () => {
    expect(formatDateTime('')).toBe('');
    expect(formatDateTime(null as any)).toBe('');
  });

  it('returns input string for invalid date', () => {
    const invalid = 'not-a-date';

    expect(formatDateTime(invalid)).toBe(invalid);
  });

  it('formats date with seconds by default', () => {
    const date = '2025-10-03T12:15:30Z';

    expect(formatDateTime(date)).toBe('2025-10-03 14:15:30');
  });

  it('formats date without seconds if withoutSeconds true', () => {
    const date = '2025-10-03T12:15:30Z';

    expect(formatDateTime(date, true)).toBe('2025-10-03 14:15');
  });
});

describe('getDateTimeWithoutSeconds', () => {
  it('returns empty string if undefined or _text missing', () => {
    expect(getDateTimeWithoutSeconds(undefined)).toBe('');
    expect(getDateTimeWithoutSeconds({} as any)).toBe('');
  });

  it('returns formatted date without seconds if _text present', () => {
    const isoDate = { _text: '2025-10-03T12:15:30Z' } as any;

    expect(getDateTimeWithoutSeconds(isoDate)).toBe('2025-10-03 14:15');
  });
});
