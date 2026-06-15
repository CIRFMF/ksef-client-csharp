import { describe, it, expect, vi, beforeEach, test } from 'vitest';
import { generateAdres } from './Adres';
import FormatTyp from '../../../shared/enums/common.enum';

vi.mock('../../../shared/PDF-functions', () => ({
  formatText: vi.fn((text: string, style: string) => ({ text, style })),
  getKraj: vi.fn((code: string) => `Kraj: ${code}`),
  createLabelText: vi.fn((label: string, value: any) => [{ text: `${label}${value ?? ''}` }]),
}));

import { formatText, createLabelText } from '../../../shared/PDF-functions';

describe(generateAdres.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  test.each([
    [{ AdresL1: { _text: 'Ulica Testowa 1' } }, ['Ulica Testowa 1']],
    [{ AdresL2: { _text: '00-001 Warszawa' } }, ['00-001 Warszawa']],
    [{ KodKraju: { _text: 'PL' } }, ['Kraj: PL']],
    [
      { AdresL1: { _text: 'Ulica 1' }, AdresL2: { _text: 'Miasto' }, KodKraju: { _text: 'DE' } },
      ['Ulica 1', 'Miasto', 'Kraj: DE'],
    ],
  ])('generuje dane adresowe dla %s', (adres, expectedTexts) => {
    const result = generateAdres(adres as any);

    expect(formatText).toHaveBeenCalledTimes(expectedTexts.length);
    expectedTexts.forEach((text) => {
      expect(formatText).toHaveBeenCalledWith(text, FormatTyp.Value);
    });

    expect(createLabelText).toHaveBeenCalledWith('GLN: ', (adres as any).GLN);
    expect((result[result.length - 1] as any).text).toContain('GLN:');
  });

  it('zwraca tylko GLN gdy brak innych pól', () => {
    const adres = { GLN: '1234567890' };
    const result = generateAdres(adres as any);
    expect(formatText).not.toHaveBeenCalled();
    expect(createLabelText).toHaveBeenCalledWith('GLN: ', '1234567890');
    expect(result).toHaveLength(1);
    expect((result[0] as any).text).toContain('GLN:');
  });
});
