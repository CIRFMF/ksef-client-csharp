import pdfMake from 'pdfmake/build/pdfmake';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { generateFA1 } from './FA1-generator';
import { Faktura } from './types/fa1.types';

import { AdditionalDataTypes } from './types/common.types';

vi.mock('./generators/FA1/Adnotacje', () => ({ generateAdnotacje: vi.fn(() => ({ example: 'adnotacje' })) }));
vi.mock('./generators/FA1/DodatkoweInformacje', () => ({
  generateDodatkoweInformacje: vi.fn(() => ({ example: 'dodatkowe' })),
}));
vi.mock('./generators/FA1/Platnosc', () => ({ generatePlatnosc: vi.fn(() => ({ example: 'platnosc' })) }));
vi.mock('./generators/FA1/Podmioty', () => ({ generatePodmioty: vi.fn(() => [{ example: 'podmioty' }]) }));
vi.mock('./generators/FA1/PodsumowanieStawekPodatkuVat', () => ({
  generatePodsumowanieStawekPodatkuVat: vi.fn(() => ({ example: 'podsumowanie' })),
}));
vi.mock('./generators/FA1/Rabat', () => ({ generateRabat: vi.fn(() => ({ example: 'rabat' })) }));
vi.mock('./generators/FA1/Szczegoly', () => ({ generateSzczegoly: vi.fn(() => ({ example: 'szczegoly' })) }));
vi.mock('./generators/FA1/WarunkiTransakcji', () => ({
  generateWarunkiTransakcji: vi.fn(() => ({ example: 'warunki' })),
}));
vi.mock('./generators/FA1/Wiersze', () => ({ generateWiersze: vi.fn(() => ({ example: 'wiersze' })) }));
vi.mock('./generators/FA1/Zamowienie', () => ({
  generateZamowienie: vi.fn(() => ({ example: 'zamowienie' })),
}));
vi.mock('./generators/common/DaneFaKorygowanej', () => ({
  generateDaneFaKorygowanej: vi.fn(() => ({ example: 'daneKorygowanej' })),
}));
vi.mock('./generators/common/Naglowek', () => ({ generateNaglowek: vi.fn(() => [{ example: 'naglowek' }]) }));
vi.mock('./generators/common/Rozliczenie', () => ({
  generateRozliczenie: vi.fn(() => ({ example: 'rozliczenie' })),
}));
vi.mock('./generators/common/Stopka', () => ({ generateStopka: vi.fn(() => [{ example: 'stopka' }]) }));
vi.mock('./PDF-functions', () => ({
  generateStyle: vi.fn(() => ({ styles: {}, defaultStyle: {} })),
  hasValue: vi.fn(() => true),
}));

describe('generateFA1', () => {
  const mockCreatePdfReturn = { example: 'pdfCreatedObject' };

  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('calls pdfMake.createPdf and returns result (KOR with OkresFaKorygowanej, calls generateRabat)', () => {
    const invoice: Faktura = {
      Fa: {
        RodzajFaktury: { _text: 'KOR' },
        OkresFaKorygowanej: { _text: 'someValue' },
        Zamowienie: {},
        P_15: { _text: '15' },
        KodWaluty: { _text: 'PLN' },
        Adnotacje: {},
        Rozliczenie: {},
        Platnosc: {},
        WarunkiTransakcji: {},
      },
      Stopka: {},
      Naglowek: {},
    } as any;

    const additionalData: AdditionalDataTypes = { nrKSeF: 'nrKSeF' };

    const createPdfSpy = vi.spyOn(pdfMake, 'createPdf').mockReturnValue(mockCreatePdfReturn as any);

    const result = generateFA1(invoice, additionalData);

    expect(createPdfSpy).toHaveBeenCalled();
    expect(result).toBe(mockCreatePdfReturn);
  });

  it('calls pdfMake.createPdf and returns result (non-KOR, calls generateWiersze)', () => {
    const invoice: Faktura = {
      Fa: {
        RodzajFaktury: { _text: 'VAT' },
        Zamowienie: {},
        P_15: { _text: '15' },
        KodWaluty: { _text: 'PLN' },
      },
      Stopka: {},
      Naglowek: {},
    } as any;

    const additionalData: AdditionalDataTypes = { nrKSeF: 'nrKSeF' };

    const createPdfSpy = vi.spyOn(pdfMake, 'createPdf').mockReturnValue(mockCreatePdfReturn as any);

    const result = generateFA1(invoice, additionalData);

    expect(createPdfSpy).toHaveBeenCalled();
    expect(result).toBe(mockCreatePdfReturn);
  });
});
