import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePodmioty } from './Podmioty';
import { Faktura } from '../../types/fa1.types';

vi.mock('../../../shared/PDF-functions', () => ({
  createSection: vi.fn((arr) => arr),
  getTable: vi.fn((obj) => (obj ? (Array.isArray(obj) ? obj : [obj]) : [])),
  getValue: vi.fn((val) => (val && val._text ? val._text : '')),
  hasValue: vi.fn((val) => Boolean(val && val._text)),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));
vi.mock('./Podmiot1', () => ({
  generatePodmiot1: vi.fn(() => [{ podmiot1: true }]),
}));
vi.mock('./Podmiot1Podmiot1K', () => ({
  generatePodmiot1Podmiot1K: vi.fn(() => [{ podmiot1K: true }]),
}));
vi.mock('./Podmiot2', () => ({
  generatePodmiot2: vi.fn(() => [{ podmiot2: true }]),
}));
vi.mock('./Podmiot2Podmiot2k', () => ({
  generatePodmiot2Podmiot2K: vi.fn(() => [{ podmiot2K: true }]),
}));
vi.mock('./Podmiot3', () => ({
  generatePodmiot3: vi.fn(() => [{ podmiot3: true }]),
}));
vi.mock('./Podmiot3Podmiot2k', () => ({
  generateDaneIdentyfikacyjneTPodmiot3Dto: vi.fn(() => [{ daneIdentyfikacyjne: true }]),
}));
vi.mock('./PodmiotUpowazniony', () => ({
  generatePodmiotUpowazniony: vi.fn(() => [{ upowazniony: true }]),
}));
vi.mock('../../../shared/generators/common/functions', () => ({
  getValue: vi.fn((val) => (val && val._text ? val._text : '')),
}));

describe(generatePodmioty.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('calls generatePodmiot1Podmiot1K when Podmiot1K exists', () => {
    const invoice: Faktura = {
      Podmiot1: {},
      Podmiot2: {},
      Fa: {
        Podmiot1K: {},
        Podmiot2K: [{}],
      },
    };
    const result = generatePodmioty(invoice);

    expect(result).toContainEqual(expect.arrayContaining([{ podmiot1K: true }]));
  });

  it('calls generatePodmiot1 when Podmiot1K missing', () => {
    const invoice: Faktura = {
      Podmiot1: {},
      Podmiot2: {},
      Fa: {
        Podmiot2K: [{}],
      },
    };
    const result = generatePodmioty(invoice);

    expect(result).toContainEqual(expect.arrayContaining([{ podmiot1: true }]));
  });

  it('calls generatePodmiot2Podmiot2K when Podmiot2K available', () => {
    const invoice: Faktura = {
      Podmiot2: {},
      Fa: {
        Podmiot2K: [{}, {}],
      },
    };
    const result = generatePodmioty(invoice);

    expect(result).toContainEqual(expect.arrayContaining([{ podmiot2K: true }]));
  });

  it('renders Podmiot1 and Podmiot2 side by side if no Fa Podmiot1K or Podmiot2K', () => {
    const invoice: Faktura = {
      Podmiot1: {},
      Podmiot2: {},
    };
    const result = generatePodmioty(invoice);

    expect(result).toEqual(
      expect.arrayContaining([
        expect.arrayContaining([
          expect.objectContaining({
            columns: expect.any(Array),
          }),
        ]),
      ])
    );
  });

  it('calls generateDaneIdentyfikacyjneTPodmiot3Dto or generatePodmiot3 accordingly', () => {
    const invoice: Faktura = {
      Podmiot3: [{ Rola: { _text: '4' } }],
      Fa: {
        Podmiot2K: [{}, {}],
      },
    };
    const result = generatePodmioty(invoice);

    expect(result.some((e: any) => e.some((o: any) => o.daneIdentyfikacyjne || o.podmiot3))).toBe(true);
  });

  it('calls generatePodmiotUpowazniony last', () => {
    const invoice: Faktura = {
      PodmiotUpowazniony: {},
    };
    const result = generatePodmioty(invoice);

    expect(result).toContainEqual(expect.arrayContaining([{ upowazniony: true }]));
  });
});
