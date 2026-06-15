import { Content, ContentText } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelTextArray,
  createSection,
  formatText,
  getContentTable,
  getTable,
  getValue,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { TRodzajFaktury } from '../../../shared/consts/const';
import { Fa, FP } from '../../types/fa2.types';
import FormatTyp, { Position } from '../../../shared/enums/common.enum';
import { TableWithFields } from '../../types/fa1-additional-types';
import { shouldAddMarza } from '../common/Wiersze';

export function generateWiersze(faVat: Fa): Content {
  const table: Content[] = [];
  const rodzajFaktury: string | number | undefined = getValue(faVat.RodzajFaktury);
  const isP_PMarzy: boolean = Boolean(Number(getValue(faVat.Adnotacje?.PMarzy?.P_PMarzy)));
  const faWiersze: Record<string, FP>[] = getTable(faVat.FaWiersz).map((wiersz) => {
    const marza: Record<string, FP> = shouldAddMarza(rodzajFaktury, isP_PMarzy, wiersz)!;
    return marza ? { ...wiersz, ...marza } : wiersz;
  });

  const definedHeaderLp: HeaderDefine[] = [
    { name: 'NrWierszaFa', title: 'Lp.', format: FormatTyp.Default, width: 'auto' },
  ];
  const definedHeader1: HeaderDefine[] = [
    { name: 'UU_ID', title: 'Unikalny numer wiersza', format: FormatTyp.Default, width: 'auto' },
    { name: 'P_7', title: 'Nazwa towaru lub usługi', format: FormatTyp.Default, width: '*' },
    { name: 'P_9A', title: 'Cena jedn. netto', format: FormatTyp.Currency, width: 'auto' },
    { name: 'P_9B', title: 'Cena jedn. brutto', format: FormatTyp.Currency, width: 'auto' },
    { name: 'P_8B', title: 'Ilość', format: FormatTyp.Right, width: 'auto' },
    { name: 'P_8A', title: 'Miara', format: FormatTyp.Default, width: 'auto' },
    { name: 'P_10', title: 'Rabat', format: FormatTyp.Currency, width: 'auto' },
    { name: 'P_12', title: 'Stawka podatku', format: FormatTyp.Default, width: 'auto' },
    { name: 'P_12_XII', title: 'Stawka podatku OSS', format: FormatTyp.Default, width: 'auto' },
    {
      name: 'P_12_Zal_15',
      title: 'Znacznik dla towaru lub usługi z zał. nr 15 do ustawy',
      format: FormatTyp.Default,
      width: 'auto',
    },
    { name: 'P_11', title: 'Wartość sprzedaży netto', format: FormatTyp.Currency, width: 'auto' },
    { name: 'P_11A', title: 'Wartość sprzedaży brutto', format: FormatTyp.Currency, width: 'auto' },
    { name: 'P_11Vat', title: 'Wartość sprzedaży vat', format: FormatTyp.Currency, width: 'auto' },
    { name: 'KursWaluty', title: 'Kurs waluty', format: FormatTyp.Currency6, width: 'auto' },
    { name: 'StanPrzed', title: 'Stan przed', format: FormatTyp.Boolean, width: 'auto' },
  ];
  const definedHeader2: HeaderDefine[] = [
    { name: 'GTIN', title: 'GTIN', format: FormatTyp.Default, width: 'auto' },
    { name: 'PKWiU', title: 'PKWiU', format: FormatTyp.Default, width: 'auto' },
    { name: 'CN', title: 'CN', format: FormatTyp.Default, width: 'auto' },
    { name: 'PKOB', title: 'PKOB', format: FormatTyp.Default, width: 'auto' },
    { name: 'KwotaAkcyzy', title: 'KwotaAkcyzy', format: FormatTyp.Default, width: 'auto' },
    { name: 'GTU', title: 'GTU', format: FormatTyp.Default, width: 'auto' },
    { name: 'Procedura', title: 'Procedura', format: FormatTyp.Default, width: '*' },
    { name: 'P_6A', title: 'Data dostawy / wykonania', format: FormatTyp.Default, width: 'auto' },
    { name: 'Indeks', title: 'Indeks', format: FormatTyp.Default, width: 'auto' },
  ];
  let content: TableWithFields = getContentTable<(typeof faWiersze)[0]>(
    [...definedHeaderLp, ...definedHeader1, ...definedHeader2],
    faWiersze,
    'auto'
  );
  const ceny: string | ContentText = formatText(
    `Faktura wystawiona w cenach ${content.fieldsWithValue.includes('P_11') ? 'netto' : 'brutto'} w walucie ${faVat.KodWaluty?._text}`,
    [FormatTyp.Label, FormatTyp.MarginBottom8]
  );

  const p_15: string | number | undefined = getValue(faVat.P_15);
  let opis: Content = '';

  if (rodzajFaktury == TRodzajFaktury.ROZ && Number(p_15) !== 0) {
    opis = {
      stack: createLabelTextArray([
        { value: 'Kwota pozostała do zapłaty: ', formatTyp: FormatTyp.LabelGreater },
        {
          value: p_15,
          formatTyp: FormatTyp.CurrencyGreater,
          currency: getValue(faVat.KodWaluty)?.toString() ?? '',
        },
      ]),
      alignment: Position.RIGHT,
      margin: [0, 8, 0, 0],
    };
  } else if (
    (rodzajFaktury == TRodzajFaktury.VAT ||
      rodzajFaktury == TRodzajFaktury.KOR ||
      rodzajFaktury == TRodzajFaktury.KOR_ROZ ||
      rodzajFaktury == TRodzajFaktury.UPR) &&
    Number(p_15) !== 0
  ) {
    opis = {
      stack: createLabelTextArray([
        { value: 'Kwota należności ogółem: ', formatTyp: FormatTyp.LabelGreater },
        {
          value: p_15,
          formatTyp: [FormatTyp.CurrencyGreater, FormatTyp.HeaderContent, FormatTyp.Value],
          currency: getValue(faVat.KodWaluty)?.toString() ?? '',
        },
      ]),
      alignment: Position.RIGHT,
      margin: [0, 8, 0, 0],
    };
  }
  if (content.fieldsWithValue.length <= 8 && content.content) {
    table.push(content.content);
  } else {
    content = getContentTable<(typeof faWiersze)[0]>([...definedHeaderLp, ...definedHeader1], faWiersze, '*');
    if (content.content) {
      table.push(content.content);
    }
    content = getContentTable<(typeof faWiersze)[0]>([...definedHeaderLp, ...definedHeader2], faWiersze, '*');
    if (content.content && content.fieldsWithValue.length > 1) {
      table.push('\n');
      table.push(content.content);
    }
  }
  if (table.length < 1) {
    return [];
  }
  return createSection([...createHeader('Pozycje'), ceny, ...table, opis], true);
}
