import pdfMake, { TCreatedPdf } from 'pdfmake/build/pdfmake';
import pdfFonts from 'pdfmake/build/vfs_fonts';
import { Content, TDocumentDefinitions } from 'pdfmake/interfaces';
import { generateStyle, hasValue } from '../shared/PDF-functions';
import { TRodzajFaktury } from '../shared/consts/const';
import { generateAdnotacje } from './generators/FA3/Adnotacje';
import { generateDodatkoweInformacje } from './generators/FA3/DodatkoweInformacje';
import { generatePlatnosc } from './generators/FA3/Platnosc';
import { generatePodmioty } from './generators/FA3/Podmioty';
import { generatePodsumowanieStawekPodatkuVat } from './generators/FA3/PodsumowanieStawekPodatkuVat';
import { generateRabat } from './generators/FA3/Rabat';
import { generateSzczegoly } from './generators/FA3/Szczegoly';
import { generateWarunkiTransakcji } from './generators/FA3/WarunkiTransakcji';
import { generateWiersze } from './generators/FA3/Wiersze';
import { generateZamowienie } from './generators/FA3/Zamowienie';
import { generateDaneFaKorygowanej } from './generators/common/DaneFaKorygowanej';
import { generateNaglowek } from './generators/common/Naglowek';
import { generateRozliczenie } from './generators/common/Rozliczenie';
import { generateStopka } from './generators/common/Stopka';
import { Faktura } from './types/fa3.types';
import { ZamowienieKorekta } from './enums/invoice.enums';
import { AdditionalDataTypes } from './types/common.types';

pdfMake.vfs = pdfFonts.vfs;

export function generateFA3(invoice: Faktura, additionalData: AdditionalDataTypes): TCreatedPdf {
  const isKOR_RABAT: boolean =
    invoice.Fa?.RodzajFaktury?._text == TRodzajFaktury.KOR && hasValue(invoice.Fa?.OkresFaKorygowanej);
  const rabatOrRowsInvoice: Content = isKOR_RABAT ? generateRabat(invoice.Fa!) : generateWiersze(invoice.Fa!);
  const docDefinition: TDocumentDefinitions = {
    content: [
      ...generateNaglowek(invoice.Fa, additionalData, invoice.Zalacznik),
      generateDaneFaKorygowanej(invoice.Fa),
      ...generatePodmioty(invoice),
      generateSzczegoly(invoice.Fa!),
      rabatOrRowsInvoice,
      generateZamowienie(
        invoice.Fa?.Zamowienie,
        ZamowienieKorekta.Order,
        invoice.Fa?.P_15?._text ?? '',
        invoice.Fa?.RodzajFaktury?._text ?? '',
        invoice.Fa?.KodWaluty?._text ?? ''
      ),
      generatePodsumowanieStawekPodatkuVat(invoice),
      generateAdnotacje(invoice.Fa?.Adnotacje),
      generateDodatkoweInformacje(invoice.Fa!),
      generateRozliczenie(invoice.Fa?.Rozliczenie, invoice.Fa?.KodWaluty?._text ?? ''),
      generatePlatnosc(invoice.Fa?.Platnosc),
      generateWarunkiTransakcji(invoice.Fa?.WarunkiTransakcji),
      ...generateStopka(additionalData, invoice.Stopka, invoice.Naglowek, invoice.Fa?.WZ, invoice.Zalacznik),
    ],
    ...generateStyle(),
  };

  return pdfMake.createPdf(docDefinition);
}
