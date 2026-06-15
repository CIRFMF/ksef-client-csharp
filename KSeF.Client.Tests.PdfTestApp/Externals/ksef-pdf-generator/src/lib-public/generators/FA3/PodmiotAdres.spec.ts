import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePodmiotAdres } from './PodmiotAdres';
import { createHeader, createSubHeader } from '../../../shared/PDF-functions';
import { generateAdres } from './Adres';
import { Adres } from '../../types/fa3.types';
import { Margins } from 'pdfmake/interfaces';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn().mockReturnValue([{ text: 'Header' }]),
  createSubHeader: vi.fn().mockReturnValue([{ text: 'SubHeader' }]),
}));

vi.mock('./Adres', () => ({
  generateAdres: vi.fn().mockReturnValue([{ text: 'Adres' }]),
}));

describe(generatePodmiotAdres.name, () => {
  const mockAdres: Adres = {
    ulica: 'Testowa',
    nrDomu: '1',
    kodPocztowy: '00-000',
    miejscowosc: 'Warszawa',
  } as Adres;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('returns an empty array when podmiotAdres is undefined', () => {
    const result = generatePodmiotAdres(undefined);
    expect(result).toEqual([]);
    expect(createHeader).not.toHaveBeenCalled();
    expect(createSubHeader).not.toHaveBeenCalled();
    expect(generateAdres).not.toHaveBeenCalled();
  });

  it('uses createHeader when isSubheader is false by default', () => {
    const result = generatePodmiotAdres(mockAdres);
    expect(createHeader).toHaveBeenCalledWith('Adres', undefined);
    expect(createSubHeader).not.toHaveBeenCalled();
    expect(generateAdres).toHaveBeenCalledWith(mockAdres);
    expect(result).toEqual([{ text: 'Header' }, { text: 'Adres' }]);
  });

  it('uses createSubHeader when isSubheader is true', () => {
    const result = generatePodmiotAdres(mockAdres, 'Location', true);
    expect(createSubHeader).toHaveBeenCalledWith('Location', undefined);
    expect(createHeader).not.toHaveBeenCalled();
    expect(generateAdres).toHaveBeenCalledWith(mockAdres);
    expect(result).toEqual([{ text: 'SubHeader' }, { text: 'Adres' }]);
  });

  it('passes headerMargin to createHeader', () => {
    const mockMargin: Margins = [10, 20, 30, 40];
    generatePodmiotAdres(mockAdres, 'Adres', false, mockMargin);
    expect(createHeader).toHaveBeenCalledWith('Adres', mockMargin);
  });

  it('passes headerMargin to createSubHeader', () => {
    const mockMargin: Margins = [5, 5, 5, 5];
    generatePodmiotAdres(mockAdres, 'Header', true, mockMargin);
    expect(createSubHeader).toHaveBeenCalledWith('Header', mockMargin);
  });
});
