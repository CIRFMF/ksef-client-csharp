import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelTextArray,
  formatText,
  getContentTable,
  getTable,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { Procedura, TRodzajFaktury } from '../../../shared/consts/const';
import { FP, Zamowienie } from '../../types/fa1.types';
import FormatTyp, { Position } from '../../../shared/enums/common.enum';
import { ZamowienieKorekta } from '../../enums/invoice.enums';
import { FormContentState } from '../../../shared/types/additional-data.types';

export function generateZamowienie(
  orderData: Zamowienie | undefined,
  zamowienieKorekta: ZamowienieKorekta,
  p_15: string,
  rodzajFaktury: string,
  KodWaluty: string
): Content[] {
  if (!orderData) {
    return [];
  }
  const formatAbs: FormatTyp.Currency | FormatTyp.CurrencyAbs =
    zamowienieKorekta === ZamowienieKorekta.BeforeCorrection ? FormatTyp.CurrencyAbs : FormatTyp.Currency;
  const orderTable: Record<string, FP>[] = getTable(orderData?.ZamowienieWiersz).map((el, index) => {
    if (!el.NrWierszaZam._text) {
      el.NrWierszaZam._text = (index + 1).toString();
    }
    return el;
  });
  const definedHeaderLp: HeaderDefine[] = [
    { name: 'NrWierszaZam', title: 'Lp.', format: FormatTyp.Default, width: 'auto' },
  ];
  const definedHeader1: HeaderDefine[] = [
    { name: 'UU_IDZ', title: 'Unikalny numer wiersza', format: FormatTyp.Default, width: 'auto' },
    { name: 'P_7Z', title: 'Nazwa towaru lub usługi', format: FormatTyp.Default, width: '*' },
    {
      name: 'P_9AZ',
      title: 'Cena jedn. netto',
      format: formatAbs,
    },
    { name: 'P_8BZ', title: 'Ilość', format: FormatTyp.Right, width: 'auto' },
    { name: 'P_8AZ', title: 'Miara', format: FormatTyp.Default, width: 'auto' },
    { name: 'P_12Z', title: 'Stawka podatku', format: FormatTyp.Default, width: 'auto' },
    { name: 'P_12Z_XII', title: 'Stawka podatku OSS', format: FormatTyp.Default, width: 'auto' },
    { name: 'P_11NettoZ', title: 'Wartość sprzedaży netto', format: formatAbs, width: 'auto' },
    { name: 'P_11VatZ', title: 'Kwota podatku', format: formatAbs, width: 'auto' },
    { name: 'KursWalutyZ', title: 'Kwota podatku', format: formatAbs, width: 'auto' },
  ];
  const definedHeader2: HeaderDefine[] = [
    { name: 'GTINZ', title: 'GTIN', format: FormatTyp.Default, width: 'auto' },
    { name: 'PKWiUZ', title: 'PKWiU', format: FormatTyp.Default, width: 'auto' },
    { name: 'CNZ', title: 'CN', format: FormatTyp.Default, width: 'auto' },
    { name: 'PKOBZ', title: 'PKOB', format: FormatTyp.Default, width: 'auto' },
    { name: 'DodatkoweInfoZ', title: 'Dodatkowe informacje', format: FormatTyp.Default, width: '*' },
    {
      name: 'P_12Z_Procedura',
      title: 'Procedura',
      format: FormatTyp.Default,
      mappingData: Procedura,
      width: '*',
    },
    { name: 'KwotaAkcyzyZ', title: 'Kwota podatku akcyzowego', format: FormatTyp.Currency, width: 'auto' },
    { name: 'GTUZ', title: 'GTU', format: FormatTyp.Default, width: 'auto' },
    { name: 'ProceduraZ', title: 'Oznaczenia dotyczące procedur', format: FormatTyp.Default, width: '*' },
  ];

  let content: FormContentState = getContentTable<(typeof orderTable)[0]>(
    [...definedHeaderLp, ...definedHeader1, ...definedHeader2],
    orderTable,
    '*'
  );
  const table: Content[] = [];

  if (content.fieldsWithValue.length <= 9) {
    if (content.content) {
      table.push(content.content);
    }
  } else {
    content = getContentTable<(typeof orderTable)[0]>(
      [...definedHeaderLp, ...definedHeader1],
      orderTable,
      '*'
    );
    if (content.content) {
      table.push(content.content);
    }
    content = getContentTable<(typeof orderTable)[0]>(
      [...definedHeaderLp, ...definedHeader2],
      orderTable,
      '*'
    );
    if (content.content && content.fieldsWithValue.length > 1) {
      table.push(content.content);
    }
  }
  const ceny = `Faktura wystawiona w cenach ${content.fieldsWithValue.includes('P_11') ? 'netto' : 'brutto'} w walucie ${KodWaluty}`;
  let opis: Content = '';

  if (Number(p_15) > 0 && rodzajFaktury == TRodzajFaktury.ZAL) {
    opis = {
      stack: createLabelTextArray([
        { value: 'Otrzymana kwota zapłaty (zaliczki): ', formatTyp: FormatTyp.LabelGreater },
        { value: p_15, formatTyp: FormatTyp.CurrencyGreater },
      ]),
      alignment: Position.RIGHT,
      margin: [0, 8, 0, 0],
    };
  } else if (
    zamowienieKorekta !== ZamowienieKorekta.BeforeCorrection &&
    rodzajFaktury == TRodzajFaktury.KOR_ZAL &&
    Number(p_15) >= 0
  ) {
    opis = {
      stack: createLabelTextArray([
        { value: 'Kwota należności ogółem: ', formatTyp: FormatTyp.LabelGreater },
        { value: p_15, formatTyp: FormatTyp.CurrencyGreater },
      ]),
      alignment: Position.RIGHT,
      margin: [0, 8, 0, 0],
    };
  }
  return [
    {
      stack: [
        createHeader(zamowienieKorekta),
        ceny,
        {
          text: [
            'Wartość zamówienia lub umowy z uwzględnieniem kwoty podatku: ',
            formatText(orderData.WartoscZamowienia?._text, FormatTyp.Currency),
          ],
          marginBottom: 4,
        },
        ...table,
        opis,
      ],
    },
  ];
}
