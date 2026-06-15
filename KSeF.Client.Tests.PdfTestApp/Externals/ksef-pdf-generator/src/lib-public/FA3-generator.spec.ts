import pdfMake, { TCreatedPdf } from 'pdfmake/build/pdfmake';
import { beforeEach, describe, expect, it, MockInstance, vi } from 'vitest';
import { generateFA3 } from './FA3-generator';
import { Faktura } from './types/fa3.types';

import { AdditionalDataTypes } from './types/common.types';

vi.mock('./generators/FA3/Adnotacje', () => ({ generateAdnotacje: vi.fn(() => ({ example: 'adnotacje' })) }));
vi.mock('./generators/FA3/DodatkoweInformacje', () => ({
  generateDodatkoweInformacje: vi.fn(() => ({ example: 'dodatkowe' })),
}));
vi.mock('./generators/FA3/Platnosc', () => ({ generatePlatnosc: vi.fn(() => ({ example: 'platnosc' })) }));
vi.mock('./generators/FA3/Podmioty', () => ({ generatePodmioty: vi.fn(() => [{ example: 'podmioty' }]) }));
vi.mock('./generators/FA3/PodsumowanieStawekPodatkuVat', () => ({
  generatePodsumowanieStawekPodatkuVat: vi.fn(() => ({ example: 'podsumowanie' })),
}));
vi.mock('./generators/FA3/Rabat', () => ({ generateRabat: vi.fn(() => ({ example: 'rabat' })) }));
vi.mock('./generators/FA3/Szczegoly', () => ({ generateSzczegoly: vi.fn(() => ({ example: 'szczegoly' })) }));
vi.mock('./generators/FA3/WarunkiTransakcji', () => ({
  generateWarunkiTransakcji: vi.fn(() => ({ example: 'warunki' })),
}));
vi.mock('./generators/FA3/Wiersze', () => ({ generateWiersze: vi.fn(() => ({ example: 'wiersze' })) }));
vi.mock('./generators/FA3/Zamowienie', () => ({
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

describe('generateFA3', (): void => {
  const mockCreatePdfReturn = { example: 'pdfCreatedObject' };

  beforeEach((): void => {
    vi.restoreAllMocks();
  });

  it('should call pdfMake.createPdf and return its result (KOR with OkresFaKorygowanej, call generateRabat)', () => {
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
      Zalacznik: {},
      Stopka: {},
      Naglowek: {},
    } as any;

    const additionalData: AdditionalDataTypes = { nrKSeF: 'nrKSeF' };

    const createPdfSpy: MockInstance = vi
      .spyOn(pdfMake, 'createPdf')
      .mockReturnValue(mockCreatePdfReturn as any);

    const result: TCreatedPdf = generateFA3(invoice, additionalData);

    expect(createPdfSpy).toHaveBeenCalled();
    expect(result).toBe(mockCreatePdfReturn);
  });

  it('should call pdfMake.createPdf and return its result (non-KOR, call generateWiersze)', () => {
    const invoice: Faktura = {
      Fa: {
        RodzajFaktury: { _text: 'VAT' },
        Zamowienie: {},
        P_15: { _text: '15' },
        KodWaluty: { _text: 'PLN' },
      },
      Zalacznik: {},
      Stopka: {},
      Naglowek: {},
    } as any;

    const additionalData: AdditionalDataTypes = { nrKSeF: 'nrKSeF' };

    const createPdfSpy: MockInstance = vi
      .spyOn(pdfMake, 'createPdf')
      .mockReturnValue(mockCreatePdfReturn as any);

    const result: TCreatedPdf = generateFA3(invoice, additionalData);

    expect(createPdfSpy).toHaveBeenCalled();
    expect(result).toBe(mockCreatePdfReturn);
  });
});
