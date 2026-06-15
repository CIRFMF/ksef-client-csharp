import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePodmiotUpowazniony } from './PodmiotUpowazniony';
import type { PodmiotUpowazniony } from '../../types/fa3.types';

import { createHeader, createLabelText, hasValue } from '../../../shared/PDF-functions';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjneTPodmiot1Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot1Dto';
import { generatePodmiotUpowaznionyDaneKontaktowe } from './PodmiotUpowaznionyDaneKontaktowe';
import { getRolaUpowaznionegoString } from '../../../shared/generators/common/functions';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(() => [{ text: 'Header: Podmiot upoważniony' }]),
  createLabelText: vi.fn((label: string, value: any) => ({ text: `${label}${value?._text ?? value}` })),
  hasValue: vi.fn((val: any) => Boolean(val && (val._text || typeof val === 'string'))),
}));

vi.mock('./PodmiotAdres', () => ({
  generatePodmiotAdres: vi.fn(() => [{ text: 'Adres content' }]),
}));

vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot1Dto', () => ({
  generateDaneIdentyfikacyjneTPodmiot1Dto: vi.fn(() => [{ text: 'Dane identyfikacyjne content' }]),
}));

vi.mock('./PodmiotUpowaznionyDaneKontaktowe', () => ({
  generatePodmiotUpowaznionyDaneKontaktowe: vi.fn(() => [{ text: 'Kontakt content' }]),
}));

vi.mock('../../../shared/generators/common/functions', () => ({
  getRolaUpowaznionegoString: vi.fn(() => 'Agent celny'),
}));

describe(generatePodmiotUpowazniony.name, () => {
  const baseData: PodmiotUpowazniony = {
    RolaPU: { _text: '1' },
    NrEORI: { _text: 'EORI123' },
    DaneIdentyfikacyjne: { NIP: { _text: '1234567890' }, Nazwa: { _text: 'Test Sp. z o.o.' } } as any,
    Adres: { miejscowosc: 'Warszawa' } as any,
    AdresKoresp: { miejscowosc: 'Kraków' } as any,
    DaneKontaktowe: [{ Email: { _text: 'mail@test.pl' } }] as any,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns empty array when podmiotUpowazniony is undefined', () => {
    const result = generatePodmiotUpowazniony(undefined);
    expect(result).toEqual([]);
    expect(createHeader).not.toHaveBeenCalled();
  });

  it('always starts with header', () => {
    const result = generatePodmiotUpowazniony(baseData);
    expect(createHeader).toHaveBeenCalledWith('Podmiot upoważniony');
    expect(result[0]).toEqual({ text: 'Header: Podmiot upoważniony' });
  });

  it('adds Rola when RolaPU has value', () => {
    const result = generatePodmiotUpowazniony(baseData);
    expect(hasValue).toHaveBeenCalledWith(baseData.RolaPU);
    expect(getRolaUpowaznionegoString).toHaveBeenCalledWith(baseData.RolaPU, 3);
    expect(createLabelText).toHaveBeenCalledWith('Rola: ', 'Agent celny');
    expect(result.some((r) => (r as any).text?.includes('Rola: Agent celny'))).toBe(true);
  });

  it('adds EORI when NrEORI has value', () => {
    const result = generatePodmiotUpowazniony(baseData);
    expect(hasValue).toHaveBeenCalledWith(baseData.NrEORI);
    expect(createLabelText).toHaveBeenCalledWith('Numer EORI: ', baseData.NrEORI);
    expect(result.some((r) => (r as any).text?.includes('Numer EORI: EORI123'))).toBe(true);
  });

  it('adds DaneIdentyfikacyjne when present', () => {
    const result = generatePodmiotUpowazniony(baseData);
    expect(generateDaneIdentyfikacyjneTPodmiot1Dto).toHaveBeenCalledWith(baseData.DaneIdentyfikacyjne);
    expect(result.flat().some((r) => (r as any).text === 'Dane identyfikacyjne content')).toBe(true);
  });

  it('adds Adres, AdresKoresp, and DaneKontaktowe sections', () => {
    const result = generatePodmiotUpowazniony(baseData);
    expect(generatePodmiotAdres).toHaveBeenCalledWith(baseData.Adres);
    expect(generatePodmiotAdres).toHaveBeenCalledWith(baseData.AdresKoresp, 'Adres korespondencyjny');
    expect(generatePodmiotUpowaznionyDaneKontaktowe).toHaveBeenCalledWith(baseData.DaneKontaktowe);
    const flattened = result.flat();
    expect(flattened.some((r) => (r as any).text === 'Adres content')).toBe(true);
    expect(flattened.some((r) => (r as any).text === 'Kontakt content')).toBe(true);
  });

  it('handles missing optional fields gracefully', () => {
    const data: PodmiotUpowazniony = { Adres: { miejscowosc: 'Gdańsk' } } as any;
    (hasValue as any).mockReturnValue(false);
    const result = generatePodmiotUpowazniony(data);
    expect(result[0]).toEqual({ text: 'Header: Podmiot upoważniony' });
    expect(createLabelText).not.toHaveBeenCalled();
    expect(generateDaneIdentyfikacyjneTPodmiot1Dto).not.toHaveBeenCalled();
  });
});
