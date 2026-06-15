import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePodmioty } from './Podmioty';
import type { Faktura } from '../../types/fa2.types';
import { createSection } from '../../../shared/PDF-functions';
import { generatePodmiot1 } from './Podmiot1';
import { generatePodmiot1Podmiot1K } from './Podmiot1Podmiot1K';
import { generatePodmiot2 } from './Podmiot2';
import { generatePodmiot2Podmiot2K } from './Podmiot2Podmiot2k';
import { generatePodmiot3 } from './Podmiot3';
import { generateDaneIdentyfikacyjneTPodmiot3Dto } from './Podmiot3Podmiot2k';
import { generatePodmiotUpowazniony } from './PodmiotUpowazniony';

vi.mock('../../../shared/PDF-functions', () => ({
  createSection: vi.fn((content: any, flag: boolean) => ({ content, flag })),
  getTable: vi.fn((data: any) => data || []),
  getValue: vi.fn((val: any) => val?._text ?? val),
  generateColumns: vi.fn((left, right) => ({ columns: [left, right] })),
}));

vi.mock('./Podmiot1', () => ({
  generatePodmiot1: vi.fn((data: any) => ({ text: 'Podmiot1 content' })),
}));

vi.mock('./Podmiot1Podmiot1K', () => ({
  generatePodmiot1Podmiot1K: vi.fn((podmiot1: any, podmiot1K: any) => ({ text: 'Podmiot1K content' })),
}));

vi.mock('./Podmiot2', () => ({
  generatePodmiot2: vi.fn((data: any) => ({ text: 'Podmiot2 content' })),
}));

vi.mock('./Podmiot2Podmiot2k', () => ({
  generatePodmiot2Podmiot2K: vi.fn((podmiot2: any, podmiot2K: any) => ({ text: 'Podmiot2K content' })),
}));

vi.mock('./Podmiot3', () => ({
  generatePodmiot3: vi.fn((data: any, idx: number) => ({ text: `Podmiot3 content ${idx}` })),
}));

vi.mock('./Podmiot3Podmiot2k', () => ({
  generateDaneIdentyfikacyjneTPodmiot3Dto: vi.fn((data: any, idx: number) => ({
    text: `DaneIdentyfikacyjne Podmiot3 ${idx}`,
  })),
}));

vi.mock('./PodmiotUpowazniony', () => ({
  generatePodmiotUpowazniony: vi.fn(() => ({ text: 'PodmiotUpowazniony content' })),
}));

describe(generatePodmioty.name, () => {
  const baseInvoice: any = {
    Fa: {
      Podmiot1K: [{ _text: 'P1K1' }],
      Podmiot2K: [{ _text: 'P2K1' }],
    },
    Podmiot1: { _text: 'P1' },
    Podmiot2: { _text: 'P2' },
    Podmiot3: [{ Rola: { _text: '4' } }],
    PodmiotUpowazniony: { _text: 'PU' },
  } as any;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('creates section with all podmioty when Podmiot1K and Podmiot2K are present', () => {
    const result = generatePodmioty(baseInvoice) as any;
    const content = result.content;

    expect(generatePodmiot1Podmiot1K).toHaveBeenCalledWith(baseInvoice.Podmiot1, baseInvoice.Fa.Podmiot1K);
    expect(generatePodmiot2Podmiot2K).toHaveBeenCalledWith(baseInvoice.Podmiot2, baseInvoice.Fa.Podmiot2K[0]);
    expect(generatePodmiot3).toHaveBeenCalled();
    expect(generateDaneIdentyfikacyjneTPodmiot3Dto).not.toHaveBeenCalled();
    expect(generatePodmiotUpowazniony).toHaveBeenCalledWith(baseInvoice.PodmiotUpowazniony);
    expect(createSection).toHaveBeenCalled();
    expect(result.flag).toBe(true);
    expect(content).toHaveLength(4);
    expect(content[0]).toEqual({ text: 'Podmiot1K content' });
    expect(content[1]).toEqual({ text: 'Podmiot2K content' });
    expect(content[2]).toEqual({ text: 'Podmiot3 content 0' });
    expect(content[3]).toEqual({ text: 'PodmiotUpowazniony content' });
  });

  it('calls generatePodmiot1 and generatePodmiot2 when no Podmiot1K and Podmiot2K', () => {
    const invoice: Faktura = { Podmiot1: {}, Podmiot2: {} } as any;
    const result = generatePodmioty(invoice) as any;

    expect(generatePodmiot1).toHaveBeenCalledWith(invoice.Podmiot1);
    expect(generatePodmiot2).toHaveBeenCalledWith(invoice.Podmiot2);
    expect(result.flag).toBe(true);
    expect(result.content[0].length).toBeGreaterThan(0);
    expect(result.content[1].text).toEqual('PodmiotUpowazniony content');
  });

  it('handles Podmiot3 with podmiot2KDto mapping', () => {
    const invoice: Faktura = {
      Fa: { Podmiot2K: [{ _text: 'P2K1' }, { _text: 'P2K2' }] },
      Podmiot3: [{ Rola: { _text: '4' } }, { Rola: { _text: '3' } }],
      Podmiot2: {},
    } as any;
    const result = generatePodmioty(invoice) as any;
    const content = result.content;

    expect(generateDaneIdentyfikacyjneTPodmiot3Dto).toHaveBeenCalled();
    expect(generatePodmiot3).toHaveBeenCalled();
    expect(result.flag).toBe(true);
    expect(content).toHaveLength(6);
    expect(content[0]).toEqual({ text: 'Podmiot2K content' });
    expect(content[1]).toEqual({ text: 'Podmiot2K content' });
    expect(content[2]).toEqual({ text: 'DaneIdentyfikacyjne Podmiot3 0' });
    expect(content[3]).toEqual({ text: '', margin: [0, 8, 0, 0] });
    expect(content[4]).toEqual({ text: 'Podmiot3 content 1' });
    expect(content[5]).toEqual({ text: 'PodmiotUpowazniony content' });
  });

  it('handles empty Podmiot3 gracefully', () => {
    const invoice: Faktura = { Fa: {}, Podmiot1: {}, Podmiot2: {}, Podmiot3: [] } as any;
    const result = generatePodmioty(invoice) as any;

    expect(result.content).toBeDefined();
    expect(generatePodmiotUpowazniony).toHaveBeenCalled();
    expect(result.flag).toBe(true);
    expect(result.content[0].length).toBeGreaterThan(0);
    expect(result.content[1].text).toEqual('PodmiotUpowazniony content');
  });
});
