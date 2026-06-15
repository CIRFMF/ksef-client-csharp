import { describe, it, expect, vi, beforeEach } from 'vitest';
import { generatePodmiotAdres } from './PodmiotAdres';
import { Adres } from '../../types/fa1.types';
import { Margins } from 'pdfmake/interfaces';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn((title, margin) => [{ text: `HEADER:${title}`, margin }]),
  createSubHeader: vi.fn((title, margin) => [{ text: `SUBHEADER:${title}`, margin }]),
}));
vi.mock('./Adres', () => ({
  generateAdres: vi.fn((adres) => [{ text: `ADRES:${adres?.AdresPol?.Miasto?._text ?? ''}` }]),
}));

describe('generatePodmiotAdres', () => {
  beforeEach(() => vi.clearAllMocks());

  it('returns empty array for undefined address', () => {
    expect(generatePodmiotAdres(undefined)).toEqual([]);
  });

  it('renders with HEADER when isSubheader is false (default)', () => {
    const adres: Adres = { AdresPol: { Miasto: { _text: 'Katowice' } } };
    const result = generatePodmiotAdres(adres, 'Adres główny');

    expect(result[0]).toEqual({ text: 'HEADER:Adres główny', margin: undefined });
    expect(result[1]).toEqual({ text: 'ADRES:Katowice' });
  });

  it('renders with SUBHEADER and propagates headerMargin', () => {
    const adres: Adres = { AdresPol: { Miasto: { _text: 'Wrocław' } } };
    const margin: Margins = [2, 4, 6, 8];
    const result = generatePodmiotAdres(adres, 'Nagłówek podrzędny', true, margin);

    expect(result[0]).toEqual({ text: 'SUBHEADER:Nagłówek podrzędny', margin });
    expect(result[1]).toEqual({ text: 'ADRES:Wrocław' });
  });

  it('returns both header/subheader content and address output', () => {
    const adres: Adres = { AdresPol: { Miasto: { _text: 'Warszawa' } } };
    const resultHeader = generatePodmiotAdres(adres, 'Adres', false);
    const resultSub = generatePodmiotAdres(adres, 'Adres', true);

    expect(resultHeader[0]).toEqual({ text: 'HEADER:Adres', margin: undefined });
    expect(resultSub[0]).toEqual({ text: 'SUBHEADER:Adres', margin: undefined });
    expect(resultHeader[1]).toEqual({ text: 'ADRES:Warszawa' });
    expect(resultSub[1]).toEqual({ text: 'ADRES:Warszawa' });
  });
});
