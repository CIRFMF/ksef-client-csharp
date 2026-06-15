import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateDostawy } from './Adnotacje';
import { Adnotacje } from '../../types/fa1.types';

vi.mock('../../../shared/PDF-functions', () => ({
  formatText: vi.fn((text, format) => ({ text, format })),
  hasValue: vi.fn((val) => Boolean(val && val._text)),
}));
const formatText = vi.fn((text, format) => ({ text, format }));
const hasValue = vi.fn((val) => Boolean(val && val._text));

describe(generateDostawy.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
    hasValue.mockImplementation((val) => Boolean(val && val._text));
  });

  it('returns empty array when no values', () => {
    const adnotacje: Adnotacje = {};
    const result = generateDostawy(adnotacje);
    expect(result).toEqual([]);
  });

  it('creates table for P_22A–P_22BRP fields', () => {
    const adnotacje: Adnotacje = {
      P_22A: { _text: '2024-01-01' },
      P_22BMK: { _text: 'Ford' },
      P_22BMD: { _text: 'Focus' },
      P_22BK: { _text: 'Red' },
      P_22BNR: { _text: 'ABC123' },
      P_22BRP: { _text: '2023' },
    };
    const result = generateDostawy(adnotacje);
    expect(result).toHaveLength(1);
  });

  it('does not create table when no valid values found', () => {
    const adnotacje: Adnotacje = { P_22D: { _text: '' } };
    hasValue.mockReturnValue(false);
    const result = generateDostawy(adnotacje);
    expect(result).toEqual([]);
  });
});
