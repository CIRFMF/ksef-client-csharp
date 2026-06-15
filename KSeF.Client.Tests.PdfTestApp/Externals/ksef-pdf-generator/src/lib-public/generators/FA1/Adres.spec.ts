import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Adres } from '../../types/fa1.types';
import { generateAdres } from './Adres';

vi.mock('../../../shared/PDF-functions', () => ({
  createLabelText: vi.fn().mockImplementation((label, value) => [{ text: `LABEL:${label}${value._text}` }]),
}));

describe('generateAdres', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns an empty array when both AdresPol and AdresZagr are undefined', () => {
    const adres: Adres = {};
    const result = generateAdres(adres);

    expect(result).toEqual([]);
  });

  it('creates correct fields for foreign address (AdresZagr)', () => {
    const adres: Adres = {
      AdresZagr: {
        KodKraju: { _text: 'PL' },
        Ulica: { _text: 'Example Street' },
        NrDomu: { _text: '123' },
        NrLokalu: { _text: '4A' },
        KodPocztowy: { _text: '00-123' },
        Miejscowosc: { _text: 'Warsaw' },
        GLN: { _text: '123456789' },
      },
    };
    const result = generateAdres(adres);

    expect(result).toEqual(
      expect.arrayContaining([
        [{ text: 'LABEL:Kraj: PL' }],
        [{ text: 'LABEL:Ulica: Example Street' }],
        [{ text: 'LABEL:Numer domu: 123' }],
        [{ text: 'LABEL:Numer lokalu: 4A' }],
        [{ text: 'LABEL:Kod pocztowy: 00-123' }],
        [{ text: 'LABEL:Miejscowość: Warsaw' }],
        [{ text: 'LABEL:GLN: 123456789' }],
      ])
    );
  });

  it('creates correct fields for Polish address (AdresPol)', () => {
    const adres: Adres = {
      AdresPol: {
        Wojewodztwo: { _text: 'Mazowieckie' },
        Powiat: { _text: 'Warszawa' },
        Gmina: { _text: 'Centrum' },
        Ulica: { _text: 'Main' },
        NrDomu: { _text: '5' },
        NrLokalu: { _text: '12B' },
        KodPocztowy: { _text: '01-234' },
        Miejscowosc: { _text: 'Warsaw' },
        Poczta: { _text: 'Warsaw 1' },
        GLN: { _text: '987654321' },
      },
    };
    const result = generateAdres(adres);

    expect(result).toEqual(
      expect.arrayContaining([
        [{ text: 'LABEL:Województwo: Mazowieckie' }],
        [{ text: 'LABEL:Powiat: Warszawa' }],
        [{ text: 'LABEL:Gmina: Centrum' }],
        [{ text: 'LABEL:Ulica: Main' }],
        [{ text: 'LABEL:Numer domu: 5' }],
        [{ text: 'LABEL:Numer lokalu: 12B' }],
        [{ text: 'LABEL:Kod pocztowy: 01-234' }],
        [{ text: 'LABEL:Miejscowość: Warsaw' }],
        [{ text: 'LABEL:Poczta: Warsaw 1' }],
        [{ text: 'LABEL:GLN: 987654321' }],
      ])
    );
  });

  it('works correctly when both AdresPol and AdresZagr exist', () => {
    const adres: Adres = {
      AdresPol: { Miejscowosc: { _text: 'Katowice' } },
      AdresZagr: { Miejscowosc: { _text: 'Berlin' } },
    };
    const result = generateAdres(adres);

    expect(result).toEqual(
      expect.arrayContaining([
        [{ text: 'LABEL:Miejscowość: Katowice' }],
        [{ text: 'LABEL:Miejscowość: Berlin' }],
      ])
    );
  });

  it('skips undefined fields in AdresPol and AdresZagr', () => {
    const adres: Adres = {
      AdresPol: {},
      AdresZagr: {},
    };
    const result = generateAdres(adres);

    expect(result).toEqual([]);
  });
});
