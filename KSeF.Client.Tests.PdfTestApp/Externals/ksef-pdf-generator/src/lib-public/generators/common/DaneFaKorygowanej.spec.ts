import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as PDFFunctions from '../../../shared/PDF-functions';
import { TypKorekty } from '../../../shared/consts/const';
import { generateDaneFaKorygowanej } from './DaneFaKorygowanej';

describe('generateDaneFaKorygowanej', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.spyOn(PDFFunctions, 'createHeader').mockImplementation((text: string) => [{ text }]);

    vi.spyOn(PDFFunctions, 'createLabelText').mockImplementation((label, value, format) => [
      { text: `${label}${value}`, style: format },
    ]);
    vi.spyOn(PDFFunctions, 'createSection').mockImplementation((content, isLineOnTop?, margin?) => [
      {
        stack: content,
        isLineOnTop,
        margin,
      },
    ]);
    vi.spyOn(PDFFunctions, 'generateTwoColumns').mockImplementation((col1, col2) => [col1, col2]);
    vi.spyOn(PDFFunctions, 'getTable').mockImplementation((data) => (Array.isArray(data) ? data : []));
  });

  it('should generate content with one DaneFaKorygowanej item', () => {
    const invoice = {
      NrFaKorygowany: 'NR123',
      PrzyczynaKorekty: 'Some reason',
      TypKorekty: { _text: '1' },
      DaneFaKorygowanej: [
        {
          DataWystFaKorygowanej: '2025-01-01',
          NrFaKorygowanej: 'NR321',
          NrKSeFFaKorygowanej: 'KSEF456',
        },
      ],
    };

    generateDaneFaKorygowanej(invoice as any);

    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Dane faktury korygowanej');
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Poprawny numer faktury korygowanej: ',
      'NR123'
    );
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Przyczyna korekty dla faktur korygujących: ',
      'Some reason'
    );
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Typ skutku korekty: ', TypKorekty['1']);
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith(
      'Data wystawienia faktury, której dotyczy faktura korygująca: ',
      '2025-01-01'
    );
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Numer faktury korygowanej: ', 'NR321');
    expect(PDFFunctions.createLabelText).toHaveBeenCalledWith('Numer KSeF faktury korygowanej: ', 'KSEF456');
    expect(PDFFunctions.generateTwoColumns).toHaveBeenCalled();
    expect(PDFFunctions.createSection).toHaveBeenCalled();
  });

  it('should generate content with multiple DaneFaKorygowanej items', () => {
    const invoice = {
      NrFaKorygowany: 'NR123',
      PrzyczynaKorekty: 'Some reason',
      TypKorekty: { _text: '1' },
      DaneFaKorygowanej: [
        { DataWystFaKorygowanej: '2025-01-01', NrFaKorygowanej: 'NR1' },
        { DataWystFaKorygowanej: '2025-01-02', NrFaKorygowanej: 'NR2' },
      ],
    };

    generateDaneFaKorygowanej(invoice as any);

    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Dane faktury korygowanej');
    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Dane identyfikacyjne faktury korygowanej 1');
    expect(PDFFunctions.createHeader).toHaveBeenCalledWith('Dane identyfikacyjne faktury korygowanej 2');
    expect(PDFFunctions.createSection).toHaveBeenCalled();
  });

  it('should return empty section if no invoice provided', () => {
    generateDaneFaKorygowanej(undefined);

    expect(PDFFunctions.createSection).toHaveBeenCalled();
  });
});
