import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generatePodmiot3 } from './Podmiot3';
import type { Podmiot3 } from '../../types/fa3.types';
import * as PDFFunctions from '../../../shared/PDF-functions';
import * as AdresModule from './Adres';
import * as DaneIdModule from './PodmiotDaneIdentyfikacyjneTPodmiot3Dto';
import * as DaneKontaktoweModule from './PodmiotDaneKontaktowe';
import * as CommonFunctions from '../../../shared/generators/common/functions';

vi.mock('../../../shared/PDF-functions');
vi.mock('./Adres');
vi.mock('./PodmiotDaneIdentyfikacyjneTPodmiot3Dto');
vi.mock('./PodmiotDaneKontaktowe');
vi.mock('../../../shared/generators/common/functions');

describe(generatePodmiot3.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(PDFFunctions.createHeader).mockImplementation((text) => [{ text }]);
    vi.mocked(PDFFunctions.createLabelText).mockImplementation((label, value) => [
      { text: `${label}${value ?? ''}` },
    ]);
    vi.mocked(PDFFunctions.formatText).mockImplementation((text, style) => ({ text, style }) as any);
    vi.mocked(PDFFunctions.generateLine).mockReturnValue({ line: true } as any);
    vi.mocked(PDFFunctions.generateTwoColumns).mockImplementation((c1, c2) => ({ columns: [c1, c2] }));

    vi.mocked(AdresModule.generateAdres).mockReturnValue([{ text: 'mockAddress' }]);
    vi.mocked(DaneIdModule.generateDaneIdentyfikacyjneTPodmiot3Dto).mockReturnValue([
      { text: 'mockDaneIdentyfikacyjne' },
    ]);
    vi.mocked(DaneKontaktoweModule.generateDaneKontaktowe).mockReturnValue([{ text: 'mockDaneKontaktowe' }]);
    vi.mocked(CommonFunctions.getRolaString).mockReturnValue('mockRola');
  });

  it('generates minimal podmiot3 structure', () => {
    const podmiot: Podmiot3 = { IDNabywcy: 'ID1', NrEORI: 'EORI1', DaneIdentyfikacyjne: {} } as any;
    const result = generatePodmiot3(podmiot, 0) as any;

    expect(result[0]).toEqual({ line: true });
    expect(result[1]).toHaveProperty('columns');
    expect(result[1].columns[0].length).toBeGreaterThan(0);
    expect(result[1].columns[1].length).toBe(0);
  });

  it('adds column2 when Adres is present', () => {
    const podmiot: Podmiot3 = {
      IDNabywcy: 'ID1',
      NrEORI: 'EORI1',
      DaneIdentyfikacyjne: {},
      Adres: {},
    } as any;

    const result = generatePodmiot3(podmiot, 0) as any;
    const column2 = result[1].columns[1];

    expect(column2[0]).toHaveProperty('text', 'Adres');
    expect(column2[1]).toEqual([{ text: 'GLN: ' }]);
  });

  it('adds column2 when AdresKoresp is present', () => {
    const podmiot: Podmiot3 = {
      IDNabywcy: 'ID1',
      NrEORI: 'EORI1',
      DaneIdentyfikacyjne: {},
      AdresKoresp: {},
    } as any;

    const result = generatePodmiot3(podmiot, 0) as any;
    const column2 = result[1].columns[1];

    expect(column2[0]).toHaveProperty('text', 'Adres do korespondencji');
    expect(column2[1]).toEqual({ text: 'GLN: ' });
  });

  it('adds column2 when DaneKontaktowe and NrKlienta are present', () => {
    const podmiot: Podmiot3 = {
      IDNabywcy: 'ID1',
      NrEORI: 'EORI1',
      DaneIdentyfikacyjne: {},
      DaneKontaktowe: [{}],
      NrKlienta: '1234',
    } as any;

    const result = generatePodmiot3(podmiot, 0) as any;
    const column2 = result[1].columns[1];

    expect(column2[0]).toHaveProperty('text', 'Dane kontaktowe');
    expect(column2[1]).toEqual({ text: 'mockDaneKontaktowe' });
    expect(column2[2]).toEqual([{ text: 'Numer klienta: 1234' }]);
  });

  it('handles all fields together', () => {
    const podmiot: Podmiot3 = {
      IDNabywcy: 'ID1',
      NrEORI: 'EORI1',
      DaneIdentyfikacyjne: {},
      Rola: 2,
      OpisRoli: 'opisRoli',
      Udzial: '50%',
      Adres: {},
      AdresKoresp: {},
      DaneKontaktowe: [{}],
      NrKlienta: '1234',
    } as any;

    const result = generatePodmiot3(podmiot, 1) as any;
    const column1 = result[1].columns[0] as any;
    const column2 = result[1].columns[1] as any;

    const flattenContent = (arr: any[]): any[] =>
      arr.flatMap((item) => (Array.isArray(item) ? flattenContent(item) : item));

    const flatColumn1 = flattenContent(column1);
    const flatColumn2 = flattenContent(column2);

    expect(flatColumn1.some((c) => typeof c.text === 'string' && c.text.includes('Podmiot inny 2'))).toBe(
      true
    );
    expect(
      flatColumn1.some((c) => typeof c.text === 'string' && c.text.includes('Identyfikator nabywcy: ID1'))
    ).toBe(true);
    expect(flatColumn1.some((c) => typeof c.text === 'string' && c.text.includes('Rola: mockRola'))).toBe(
      true
    );
    expect(
      flatColumn1.some((c) => typeof c.text === 'string' && c.text.includes('Rola inna: opisRoli'))
    ).toBe(true);
    expect(flatColumn1.some((c) => typeof c.text === 'string' && c.text.includes('Udział: 50%'))).toBe(true);

    expect(flatColumn2.some((c) => c.text === 'GLN: ')).toBe(true);
    expect(flatColumn2.some((c) => c.text === 'mockDaneKontaktowe')).toBe(true);
    expect(flatColumn2.some((c) => c.text === 'Numer klienta: 1234')).toBe(true);
  });
});
