import { Content, ContentTable, ContentText, TableCell } from 'pdfmake/interfaces';
import { DEFAULT_TABLE_LAYOUT, TableDataType } from '../../../shared/consts/const';
import {
  createHeader,
  createLabelText,
  createSection,
  createSubHeader,
  formatText,
  getContentTable,
  getTable,
  hasValue,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { BlokDanych, FP, Kol, MetaDane, Tabela, TMetaDane, Wiersz, Zalacznik } from '../../types/fa3.types';
import FormatTyp from '../../../shared/enums/common.enum';
import { FormContentState } from '../../../shared/types/additional-data.types';

export function generateZalaczniki(zalacznik?: Zalacznik): Content[] {
  if (!getTable(zalacznik?.BlokDanych).length) {
    return [];
  }

  const result: Content[] = [];
  const definedHeader: HeaderDefine[] = [
    { name: 'PelnaNazwa', title: 'Pełna nazwa', format: FormatTyp.Default },
    { name: 'KRS', title: 'KRS', format: FormatTyp.Default },
    { name: 'REGON', title: 'REGON', format: FormatTyp.Default },
    { name: 'BDO', title: 'BDO', format: FormatTyp.Default },
  ];
  const faWiersze: BlokDanych[] = getTable(zalacznik?.BlokDanych ?? []);
  const content: FormContentState = getContentTable<(typeof faWiersze)[0]>(
    [...definedHeader],
    faWiersze,
    '*'
  );

  result.push(createHeader('Załącznik do Faktury VAT'));

  getTable(zalacznik?.BlokDanych).forEach((blok: BlokDanych, index: number): void => {
    result.push(createSubHeader(`Szczegółowe dane załącznika (${index + 1})`));
    if (blok.ZNaglowek) {
      result.push(
        createLabelText('Nagłówek bloku danych: ', blok.ZNaglowek, FormatTyp.Value, { marginBottom: 8 })
      );
    }
    if (getTable(blok.MetaDane)?.length) {
      result.push(generateKluczWartosc(getTable(blok.MetaDane)));
    }
    if (blok.Tekst?.Akapit) {
      result.push(createLabelText('Opis: ', ' '));
      getTable(blok.Tekst.Akapit).forEach((text: FP): void => {
        if (hasValue(text)) {
          result.push(formatText(text._text, FormatTyp.Value));
        }
      });
    }

    if (getTable(blok.Tabela).length) {
      getTable(blok.Tabela).forEach((tabela: Tabela, index: number): void => {
        if (blok.ZNaglowek?._text) {
          result.push(createSubHeader(`${blok.ZNaglowek?._text} ${index + 1}`));
        }
        if (getTable(tabela.TMetaDane)?.length) {
          result.push({
            stack: generateKluczWartosc(
              getTable(tabela.TMetaDane).map(
                (item: TMetaDane): { ZKlucz: FP | undefined; ZWartosc: FP | undefined } => ({
                  ZKlucz: item.TKlucz,
                  ZWartosc: item.TWartosc,
                })
              )
            ),
            margin: [0, 8, 0, 0],
          });
        }
        if (tabela.Opis) {
          result.push(createLabelText('Opis: ', tabela.Opis));
        }
        if (getTable(tabela.TNaglowek?.Kol).length) {
          result.push(formatText('Tabela', [FormatTyp.GrayBoldTitle, FormatTyp.LabelSmallMargin]));
          result.push(generateTable(tabela));
        }
        if (getTable(tabela.Suma?.SKom).length) {
          result.push(generateSuma(getTable(tabela.Suma?.SKom)));
        }
      });
    }
  });

  if (content.fieldsWithValue.length && content.content) {
    result.push(content.content);
  }

  return createSection(result, false);
}

function generateKluczWartosc(data: MetaDane[]): Content[] {
  const result: Content[] = [];
  const definedHeader: HeaderDefine[] = [
    { name: 'ZKlucz', title: 'Klucz', format: FormatTyp.Default },
    { name: 'ZWartosc', title: 'Wartość', format: FormatTyp.Default },
  ];
  const faWiersze: MetaDane[] = getTable(data ?? []);
  const content: FormContentState = getContentTable<(typeof faWiersze)[0]>(
    [...definedHeader],
    faWiersze,
    '*'
  );

  if (content.fieldsWithValue.length && content.content) {
    result.push(content.content);
  }
  return result;
}

function generateTable(tabela: Tabela): Content[] {
  if (!tabela.TNaglowek?.Kol?.length) {
    return [];
  }

  const result: Content[] = [];
  const Kol: Kol[] = getTable(tabela.TNaglowek.Kol);
  const cutedTableHeader: Kol[][] = chunkArray(Kol);

  cutedTableHeader.forEach((table: Kol[], index: number): void => {
    result.push(createTable(table, tabela.Wiersz ?? [], index, Kol.length));
  });
  return result;
}

function createTable(cols: Kol[], rows: Wiersz[], subTableIndex: number, totalLength: number): ContentTable {
  const definedHeader: Content[] = cols.map((item: Kol): string | ContentText =>
    formatText(item.NKom?._text, FormatTyp.GrayBoldTitle)
  );
  const tableBody: TableCell[] = [];

  rows.forEach((item: Wiersz): void => {
    const WKom: FP[] = getTable(item.WKom);

    while (WKom.length < totalLength) {
      WKom.push({ _text: '' });
    }
    const cuttedRows: FP[][] = chunkArray(WKom ?? []);

    if (cuttedRows.length >= subTableIndex + 1) {
      tableBody.push(
        cuttedRows[subTableIndex].map((subItem: FP, index: number): TableCell => {
          return formatText(
            subItem._text ?? '',
            cols[index]._attributes?.Typ ? TableDataType[cols[index]._attributes.Typ] : FormatTyp.Value
          ) as TableCell;
        })
      );
    }
  });
  const widths: Content[] = definedHeader.map((index: Content): string[] => {
    if (index) {
      return ['*'];
    } else {
      return ['auto'];
    }
  });

  return {
    table: {
      headerRows: 1,
      widths: [...widths] as never[],
      heights: 8,
      body: [[...definedHeader], ...tableBody] as TableCell[][],
    },
    layout: DEFAULT_TABLE_LAYOUT,
    marginTop: 8,
  };
}

function generateSuma(data: FP[]): Content[] {
  const result: Content[] = [];
  const definedHeader: HeaderDefine[] = [
    { name: '', title: 'Podsumowanie tabeli', format: FormatTyp.Default },
  ];
  const faWiersze: FP[] = getTable(data ?? []);
  const content: FormContentState = getContentTable<(typeof faWiersze)[0]>(
    [...definedHeader],
    faWiersze,
    '*',
    [0, 8, 0, 0]
  );

  if (content.fieldsWithValue.length && content.content) {
    result.push(content.content);
  }
  return result;
}

export function chunkArray<T>(columns: T[]): T[][] {
  if (!Array.isArray(columns)) {
    return [];
  }

  const n: number = columns.length;

  if (n <= 7) {
    return [columns];
  } else if (n >= 8 && n <= 14) {
    const half: number = Math.floor(n / 2);

    if (n % 2 === 0) {
      return [columns.slice(0, half), columns.slice(half)];
    } else {
      return [columns.slice(0, half + 1), columns.slice(half + 1)];
    }
  } else {
    const base: number = Math.floor(n / 3);
    const remainder: number = n % 3;

    const splits: number[] = [base, base, base];

    for (let i = 0; i < remainder; i++) {
      splits[i] += 1;
    }

    const result: T[][] = [];
    let idx = 0;

    for (const size of splits) {
      result.push(columns.slice(idx, idx + size));
      idx += size;
    }
    return result;
  }
}
