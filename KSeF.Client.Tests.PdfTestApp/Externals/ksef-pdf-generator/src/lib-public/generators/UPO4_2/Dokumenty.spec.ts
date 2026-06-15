import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as PDFFunctions from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Potwierdzenie } from '../../types/upo-v4_2.types';
import { generateDokumnetUPO } from './Dokumenty';

vi.mock('../../../shared/PDF-functions', () => ({
  formatText: vi.fn(),
  generateLine: vi.fn(),
  getContentTable: vi.fn(),
  getTable: vi.fn(),
  getValue: vi.fn(),
  hasValue: vi.fn(),
  verticalSpacing: vi.fn(),
}));

describe(generateDokumnetUPO.name, () => {
  let mockPotwierdzenie: Potwierdzenie;

  beforeEach(() => {
    vi.clearAllMocks();

    mockPotwierdzenie = {
      NumerReferencyjnySesji: { _text: 'NR123' },
      OpisPotwierdzenia: {
        Strona: { _text: '1' },
        LiczbaStron: { _text: '2' },
        ZakresDokumentowOd: { _text: 'FA001' },
        ZakresDokumentowDo: { _text: 'FA002' },
        CalkowitaLiczbaDokumentow: { _text: '2' },
      },
      Uwierzytelnienie: {
        IdKontekstu: {
          NIP: { _text: '1234567890' },
        },
        SkrotDokumentuUwierzytelniajacego: { _text: 'ABC123' },
      },
      NazwaStrukturyLogicznej: { _text: 'XSD' },
      KodFormularza: { _text: 'K123' },
      Dokument: [
        {
          NumerKSeFDokumentu: 'D001',
          NumerFaktury: 'F001',
          NipSprzedawcy: '123',
          DataWystawieniaFaktury: '2024-01-01',
          DataPrzeslaniaDokumentu: '2024-01-02',
          DataNadaniaNumeruKSeF: '2024-01-03',
          SkrotDokumentu: 'XYZ',
        },
      ],
    } as any;

    vi.mocked(PDFFunctions.getTable).mockReturnValue(mockPotwierdzenie.Dokument as any);
    vi.mocked(PDFFunctions.hasValue).mockReturnValue(true);
    vi.mocked(PDFFunctions.formatText).mockImplementation((text) => text as any);
    vi.mocked(PDFFunctions.generateLine).mockReturnValue('line' as any);
    vi.mocked(PDFFunctions.verticalSpacing).mockImplementation((val) => `space${val}` as any);
    vi.mocked(PDFFunctions.getContentTable).mockReturnValue({
      content: 'tableContent',
      fieldsWithValue: [],
    } as any);
    vi.mocked(PDFFunctions.getValue).mockImplementation((val: any) => val?._text);
  });

  it('should generate basic UPO structure', () => {
    const result = generateDokumnetUPO(mockPotwierdzenie);

    expect(result[0]).toBe('space4');
    expect(result[1]).toBe('line');
    expect(result[2]).toBe('space8');
    expect(result[3]).toBe('Urzędowe poświadczenie odbioru dokumentu elektronicznego KSeF');
    expect(result[4]).toBe('space8');
  });

  it('should include all UPO fields when hasValue returns true', () => {
    generateDokumnetUPO(mockPotwierdzenie);

    expect(PDFFunctions.formatText).toHaveBeenCalledWith(
      'Numer referencyjny sesji: ',
      FormatTyp.GrayBoldTitle
    );
    expect(PDFFunctions.formatText).toHaveBeenCalledWith('Strona dokumentu UPO: ', FormatTyp.GrayBoldTitle);
    expect(PDFFunctions.formatText).toHaveBeenCalledWith(
      'Całkowita liczba stron dokumentu UPO: ',
      FormatTyp.GrayBoldTitle
    );
    expect(PDFFunctions.formatText).toHaveBeenCalledWith('Zakres dokumentów od: ', FormatTyp.GrayBoldTitle);
    expect(PDFFunctions.formatText).toHaveBeenCalledWith('Zakres dokumentów do: ', FormatTyp.GrayBoldTitle);
    expect(PDFFunctions.formatText).toHaveBeenCalledWith(
      'Całkowita liczba dokumentów: ',
      FormatTyp.GrayBoldTitle
    );
    expect(PDFFunctions.formatText).toHaveBeenCalledWith('Typ kontekstu: ', FormatTyp.GrayBoldTitle);
    expect(PDFFunctions.formatText).toHaveBeenCalledWith(
      'Identyfikator kontekstu uwierzytelnienia: ',
      FormatTyp.GrayBoldTitle
    );
    expect(PDFFunctions.formatText).toHaveBeenCalledWith(
      'Skrót dokumentu uwierzytelniającego: ',
      FormatTyp.GrayBoldTitle
    );
    expect(PDFFunctions.formatText).toHaveBeenCalledWith(
      'Nazwa pliku XSD struktury logicznej dotycząca przesłanego dokumentu:',
      FormatTyp.GrayBoldTitle
    );
    expect(PDFFunctions.formatText).toHaveBeenCalledWith(
      'Kod formularza przedłożonego dokumentu elektronicznego:',
      FormatTyp.GrayBoldTitle
    );
  });

  it('should not add document table if getContentTable.content is null', () => {
    vi.mocked(PDFFunctions.getContentTable).mockReturnValue({ content: null, fieldsWithValue: [] });
    const result = generateDokumnetUPO(mockPotwierdzenie);
    expect(result).not.toContain('null');
  });

  it('should call getTable with Dokument', () => {
    generateDokumnetUPO(mockPotwierdzenie);
    expect(PDFFunctions.getTable).toHaveBeenCalledWith(mockPotwierdzenie.Dokument);
  });
});
