import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateRabat } from './Rabat';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp, { Position } from '../../../shared/enums/common.enum';
import { Fa } from '../../types/fa3.types';
import { Content } from 'pdfmake/interfaces';

vi.mock('../../../shared/PDF-functions', () => ({
  createHeader: vi.fn(),
  createLabelText: vi.fn(),
  createSection: vi.fn(),
  formatText: vi.fn(),
  generateTwoColumns: vi.fn(),
  getContentTable: vi.fn(),
  getTable: vi.fn(),
}));

describe(generateRabat.name, () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const mockInvoice: Fa = {
    FaWiersz: [
      {
        NrWierszaFa: { _text: '1' },
        P_7: { _text: 'Product 1' },
        P_8B: { _text: '5' },
        P_8A: { _text: 'szt' },
      },
    ],
    P_15: { _text: '100.00' },
  } as any;

  beforeEach(() => {
    vi.mocked(PDFFunctions.getTable).mockReturnValue([]);
    vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header'] as any);
    vi.mocked(PDFFunctions.createLabelText).mockReturnValue(['label'] as any);
    vi.mocked(PDFFunctions.createSection).mockReturnValue('section' as any);
    vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted' as any);
    vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns' as any);
    vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
      content: null,
      fieldsWithValue: [],
    });
  });

  it('should call createHeader with "Rabat"', () => {
    generateRabat(mockInvoice);

    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Rabat');
  });

  it('should call createSection and return result', () => {
    const mockSection = 'section';
    vi.mocked(PDFFunctions.createSection).mockReturnValue(mockSection as any);

    const result: Content[] = generateRabat(mockInvoice);

    expect(PDFFunctions.createSection).toHaveBeenCalledWith(expect.any(Array), true);
    expect(result).toEqual(mockSection);
  });

  it('should call getTable with FaWiersz', () => {
    generateRabat(mockInvoice);

    expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockInvoice.FaWiersz);
  });

  it('should call createLabelText with discount value', () => {
    generateRabat(mockInvoice);

    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Wartość rabatu ogółem: ',
      mockInvoice.P_15,
      FormatTyp.Currency,
      { alignment: Position.RIGHT }
    );
  });

  it('should call getContentTable with correct headers', () => {
    generateRabat(mockInvoice);

    expect(PDFFunctions.getContentTable).toHaveBeenCalledWith(
      expect.arrayContaining([
        expect.objectContaining({ name: 'NrWierszaFay', title: 'Lp.' }),
        expect.objectContaining({ name: 'P_7', title: 'Nazwa towaru lub usługi' }),
        expect.objectContaining({ name: 'P_8B', title: 'Ilość' }),
        expect.objectContaining({ name: 'P_8A', title: 'Miara' }),
      ]),
      [],
      '*'
    );
  });

  describe('discount text generation', () => {
    it('should generate text "nie dotyczy" when NrWierszaFa is in fieldsWithValue', () => {
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: ['NrWierszaFa', 'P_7'],
      });

      generateRabat(mockInvoice);

      expect(PDFFunctions.formatText).toHaveBeenCalledWith(
        'Rabat nie dotyczy wszystkich dostaw towarów i wykonanych usług na rzecz tego nabywcy w danym okresie.',
        FormatTyp.Default
      );
    });

    it('should generate text "dotyczy" when NrWierszaFa is not in fieldsWithValue', () => {
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: ['P_7', 'P_8B'],
      });

      generateRabat(mockInvoice);

      expect(PDFFunctions.formatText).toHaveBeenCalledWith(
        'Rabat dotyczy wszystkich dostaw towarów i wykonanych usług na rzecz tego nabywcy w danym okresie.',
        FormatTyp.Default
      );
    });

    it('should call generateTwoColumns with formatted text and empty string', () => {
      vi.mocked(PDFFunctions.formatText).mockReturnValue('formatted-text' as any);

      generateRabat(mockInvoice);

      expect(PDFFunctions.generateTwoColumns).toHaveBeenCalledWith('formatted-text', '');
    });
  });

  describe('table content', () => {
    it('should add table content when fieldsWithValue is not empty and content exists', () => {
      const mockTable = { table: 'mock-table' };
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: mockTable as any,
        fieldsWithValue: ['P_7', 'P_8B'],
      });

      generateRabat(mockInvoice);

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall).toContain(mockTable);
    });

    it('should not add table content when fieldsWithValue is empty', () => {
      const mockTable = { table: 'mock-table' };
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: mockTable as any,
        fieldsWithValue: [],
      });

      generateRabat(mockInvoice);

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall).not.toContain(mockTable);
    });

    it('should not add table content when content is null', () => {
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: ['P_7', 'P_8B'],
      });

      generateRabat(mockInvoice);

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall.every((item: any) => item !== null)).toBe(true);
    });

    it('should not add table content when both fieldsWithValue is empty and content is null', () => {
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: null,
        fieldsWithValue: [],
      });

      generateRabat(mockInvoice);

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall.length).toBeGreaterThan(0);
    });
  });

  describe('result structure', () => {
    it('should include header in result', () => {
      vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header-content'] as any);

      generateRabat(mockInvoice);

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall).toContain('header-content');
    });

    it('should include label text in result', () => {
      vi.mocked(PDFFunctions.createLabelText).mockReturnValue(['label-content'] as any);

      generateRabat(mockInvoice);

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall).toContain('label-content');
    });

    it('should include two columns in result', () => {
      vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns-content' as any);

      generateRabat(mockInvoice);

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall).toContain('columns-content');
    });
  });

  describe('complete integration', () => {
    it('should generate complete structure with all elements', () => {
      const mockTable = { table: 'mock-table' };
      vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
        content: mockTable as any,
        fieldsWithValue: ['NrWierszaFa', 'P_7'],
      });

      vi.mocked(PDFFunctions.createHeader).mockReturnValue(['header'] as any);
      vi.mocked(PDFFunctions.createLabelText).mockReturnValue(['label'] as any);
      vi.mocked(PDFFunctions.generateTwoColumns).mockReturnValue('columns' as any);

      generateRabat(mockInvoice);

      expect(PDFFunctions.createHeader).toHaveBeenCalled();
      expect(PDFFunctions.createLabelText).toHaveBeenCalled();
      expect(PDFFunctions.formatText).toHaveBeenCalled();
      expect(PDFFunctions.generateTwoColumns).toHaveBeenCalled();
      expect(PDFFunctions.getContentTable).toHaveBeenCalled();
      expect(PDFFunctions.createSection).toHaveBeenCalled();

      const sectionCall = vi.mocked(PDFFunctions.createSection).mock.calls[0][0] as any[];
      expect(sectionCall.length).toBeGreaterThan(3);
    });
  });
});
