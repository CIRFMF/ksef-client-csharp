import {
  Column,
  Content,
  ContentQr,
  ContentTable,
  ContentText,
  CustomTableLayout,
  Margins,
  Style,
  TableCell,
  TDocumentDefinitions,
} from 'pdfmake/interfaces';
import { DEFAULT_TABLE_LAYOUT, Kraj } from './consts/const';
import { formatDateTime, getFormaPlatnosciString } from './generators/common/functions';
import { HeaderDefine, PdfFP, PdfOptionField } from './types/pdf-types';
import { FP } from '../lib-public/types/fa3.types';
import { DifferentValues, FilteredKeysOfValues, TypesOfValues } from './types/universal.types';
import { CreateLabelTextData } from './types/additional-data.types';
import FormatTyp, { Answer, Position } from './enums/common.enum';

export function formatText(
  value: number | string | undefined | null,
  format: FormatTyp | FormatTyp[] | null = null,
  options: PdfOptionField = {},
  currency = ''
): ContentText | string {
  if (!value) {
    return '';
  }
  const result: ContentText = { text: value.toString() };

  Object.assign(result, options);

  if (format) {
    result.style = format;
    if (!Array.isArray(format)) {
      formatValue(format, result, value, currency);
    } else {
      format.forEach((item: FormatTyp): void => {
        formatValue(item, result, value, currency);
      });
    }
  }

  return result;
}

export function generateTable<T>(array: T[], keys: Partial<Record<keyof T, string>>): Content {
  const faRows: NonNullable<T>[] = getTable(array);

  const headers: { name: string; title: string; format: FormatTyp }[] = Object.entries(keys).map(
    ([key, value]: [string, unknown]): { name: string; title: string; format: FormatTyp } => {
      return {
        name: key,
        title: value as string,
        format: FormatTyp.Default,
      };
    }
  );

  const table: { content: ContentTable | null; fieldsWithValue: string[] } = getContentTable(
    headers,
    faRows,
    '*'
  );

  return table.content ?? [];
}

function formatValue(
  item: FormatTyp,
  result: ContentText,
  value: number | string | undefined,
  currency = ''
): void {
  switch (item) {
    case FormatTyp.Currency:
      result.text = isNaN(Number(value))
        ? (value as string)
        : `${dotToComma(Number(value).toFixed(2))} ${currency}`;
      result.alignment = Position.RIGHT;
      break;
    case FormatTyp.CurrencyAbs:
      result.text = isNaN(Number(value))
        ? (value as string)
        : `${dotToComma(Math.abs(Number(value)).toFixed(2))} ${currency}`;
      result.alignment = Position.RIGHT;
      break;
    case FormatTyp.CurrencyGreater:
      result.text = isNaN(Number(value))
        ? (value as string)
        : `${dotToComma(Number(value).toFixed(2))} ${currency}`;
      result.fontSize = 10;
      break;
    case FormatTyp.Currency6:
      result.text = isNaN(Number(value))
        ? (value as string)
        : `${dotToComma(Number(value).toFixed(6))} ${currency}`;
      result.alignment = Position.RIGHT;
      break;
    case FormatTyp.DateTime:
      result.text = formatDateTime(value as string);
      break;
    case FormatTyp.Date:
      result.text = formatDateTime(value as string, false, true);
      break;
    case FormatTyp.FormOfPayment:
      result.text = getFormaPlatnosciString({ _text: value as string });
      break;
    case FormatTyp.Boolean:
      result.text = (value as string) === '1' ? Answer.TRUE : Answer.FALSE;
      break;
    case FormatTyp.Percentage:
      result.text = `${value}%`;
      break;
  }
}

function dotToComma(value: string): string {
  return value.replace('.', ',');
}

export function hasValue(value: FP | string | number | undefined): boolean {
  return (
    !!((typeof value !== 'object' && value) || (typeof value === 'object' && value._text)) || value === 0
  );
}

export function getValue(value: FP | string | number | undefined): string | number | undefined {
  if (typeof value === 'object') {
    return value._text;
  }
  return value;
}

export function getNumber(value: FP | string | number | undefined): number {
  const text: string | number | undefined = getValue(value);

  if (!text) {
    return 0;
  }
  if (typeof text === 'number') {
    return text;
  }

  return parseFloat(text.toString());
}

export function getNumberRounded(value: FP | string | number | undefined): number {
  const number: number = getNumber(value);

  return Math.round(number * 100) / 100;
}

export function createLabelTextArray(data: CreateLabelTextData[]): Content[] {
  return [
    {
      text: data.map((textEl: CreateLabelTextData): string | ContentText =>
        formatText(getValue(textEl.value) ?? '', textEl.formatTyp ?? FormatTyp.Label, {}, textEl.currency)
      ),
    },
  ];
}

export function createLabelText(
  label: string,
  value: FP | string | number | undefined | null,
  formatTyp: FormatTyp | FormatTyp[] = FormatTyp.Value,
  style: Style = {}
): Content[] {
  if (!value || (typeof value === 'object' && !value._text)) {
    return [];
  }
  if (typeof value === 'object') {
    return [
      {
        text: [formatText(label, FormatTyp.Label), formatText(value._text, formatTyp)],
        ...style,
      },
    ];
  }

  return [
    {
      text: [formatText(label, FormatTyp.Label), formatText(value, formatTyp)],
      ...style,
    },
  ];
}

export function createSection(content: Content[], isLineOnTop: boolean, margin?: Margins): Content[] {
  return [
    {
      stack: [
        ...(content.length
          ? [...(isLineOnTop ? [{ stack: [generateLine()], margin: [0, 8, 0, 0] } as Content] : []), content]
          : []),
      ],
      margin: margin ?? [0, 0, 0, 8],
    },
  ];
}

export function createHeader(text: string, margin?: Margins): Content[] {
  return [
    {
      stack: [formatText(text, FormatTyp.HeaderContent)],
      margin: margin ?? [0, 8, 0, 8],
    },
  ];
}

export function createSubHeader(text: string, margin?: Margins): Content[] {
  return [
    {
      stack: [formatText(text, FormatTyp.SubHeaderContent)],
      margin: margin ?? [0, 4, 0, 4],
    },
  ];
}

export function generateStyle(): Partial<TDocumentDefinitions> {
  return {
    styles: {
      columnMarginLeft: {
        margin: [4, 0, 0, 0],
      },
      columnMarginRight: {
        margin: [0, 0, 4, 0],
      },
      GrayBoldTitle: {
        fillColor: '#F6F7FA',
        bold: true,
      },
      GrayTitle: {
        fillColor: '#F6F7FA',
      },
      Label: {
        color: '#343A40',
        bold: true,
      },
      LabelMargin: {
        margin: [0, 12, 0, 1.3],
      },
      LabelSmallMargin: {
        margin: [0, 6, 0, 1.3],
      },
      LabelMedium: {
        color: '#343A40',
        bold: true,
        fontSize: 9,
      },
      LabelGreater: {
        color: '#343A40',
        bold: true,
        fontSize: 10,
      },
      Value: {
        color: '#343A40',
      },
      ValueMedium: {
        color: '#343A40',
        fontSize: 9,
      },
      Bold: {
        fontSize: 9,
        bold: true,
      },
      Description: {
        color: 'blue',
        bold: false,
      },
      HeaderPosition: {
        fontSize: 16,
        bold: true,
      },
      Right: {
        alignment: Position.RIGHT,
      },
      header: {
        fontSize: 12,
        bold: true,
        margin: [0, 12, 0, 5],
      },
      HeaderContent: {
        fontSize: 10,
        bold: true,
      },
      SubHeaderContent: {
        fontSize: 7,
        bold: true,
      },
      TitleContent: {
        fontSize: 10,
        bold: true,
      },
      Link: {
        color: 'blue',
      },
      MarginBottom4: {
        marginBottom: 4,
      },
      MarginBottom8: {
        marginBottom: 8,
      },
      MarginTop4: {
        marginTop: 4,
      },
    },
    defaultStyle: {
      font: 'Roboto',
      fontSize: 7,
      lineHeight: 1.2,
    },
  };
}

export function getTable<T>(data: T | T[]): NonNullable<T>[] {
  if (!data) {
    return [];
  }
  if (Array.isArray(data)) {
    return data as NonNullable<T>[];
  }
  return [data];
}

export function getRowTable(data: string[], formatColumn: FormatTyp | FormatTyp[]): Content[] {
  return data.map((el: string, index: number): Content => {
    if (Array.isArray(formatColumn)) {
      return formatText(el, formatColumn[index] ?? FormatTyp.Default);
    }
    return formatText(el, formatColumn ?? FormatTyp.Default);
  });
}

export function hasColumnsValue<T, K extends FilteredKeysOfValues<T>>(name: K, data: T[]): boolean {
  return data.some((el: T): boolean => {
    return hasValue(el[name] as TypesOfValues);
  });
}

export function getDifferentColumnsValue<T, K extends FilteredKeysOfValues<T>>(
  name: K,
  data: T[]
): DifferentValues[] {
  const result: DifferentValues[] = [];

  data.forEach((el: T): void => {
    const val: string | number | undefined = getValue(el[name] as TypesOfValues);

    if (val) {
      const index: number = result.findIndex((el: DifferentValues): boolean => el.value === val);

      if (index < 0) {
        result.push({ value: val, count: 1 });
      } else {
        result[index].count++;
      }
    }
  });
  return result;
}

export function getContentTable<T>(
  headers: HeaderDefine[],
  data: T[],
  defaultWidths: string,
  margin?: Margins,
  wordBreak?: number
): { content: ContentTable | null; fieldsWithValue: string[] } {
  const fieldsWithValue: HeaderDefine[] = headers.filter((header: HeaderDefine): boolean => {
    return data.some((d: T): boolean => {
      const name = header.name as keyof T;

      if (name === '' && (d as FP)?._text) {
        return true;
      }

      if (name === '') {
        return false;
      }

      if (typeof d[name] === 'object' && (d[name] as FP)?._text) {
        return true;
      }

      return !!(typeof d[name] !== 'object' && d[name]);
    });
  });

  if (fieldsWithValue.length < 1) {
    return { content: null, fieldsWithValue: [] };
  }

  const headerRow: Content[] = getRowTable(
    fieldsWithValue.map((header: HeaderDefine): string => header.title),
    FormatTyp.GrayBoldTitle
  );
  const tableBody: (string | ContentText)[][] = data.map((row: T): (string | ContentText)[] => {
    return fieldsWithValue.map((header: HeaderDefine): string | ContentText => {
      const fp = (header.name ? row[header.name as keyof T] : row) as PdfFP;
      const value: string | undefined = typeof fp === 'object' ? fp?._text : fp;

      return formatText(
        makeBreakable(
          header.mappingData && value ? header.mappingData[value] : (value ?? ''),
          wordBreak ?? 40
        ),
        header.format ?? FormatTyp.Default,
        { rowSpan: fp?._rowSpan ?? 1 }
      );
    });
  });

  return {
    fieldsWithValue: fieldsWithValue.map((el: HeaderDefine): string => el.name),
    content: {
      table: {
        headerRows: 1,
        keepWithHeaderRows: 1,
        widths: fieldsWithValue.map((header: HeaderDefine): string => header.width ?? defaultWidths),
        body: [headerRow, ...tableBody] as TableCell[][],
      },
      margin: margin ?? [0, 0, 0, 8],
      layout: DEFAULT_TABLE_LAYOUT,
    },
  };
}

export function generateTwoColumns(kol1: Column, kol2: Column, margin?: Margins): Content {
  return {
    columns: [
      { stack: [kol1], width: '50%' },
      { stack: [kol2], width: '50%' },
    ],
    margin: margin ?? [0, 0, 0, 0],
    columnGap: 20,
  };
}

export function generateColumns(contents: Content[][], style: Style | undefined = undefined): Content {
  const width: string = (100 / contents.length).toFixed(0) + '%';
  const columns: Column = contents.map((content: Content[]) => ({ stack: content, width }));
  const columnStyle: Style = style ? { ...style } : { columnGap: 20 };

  return {
    columns,
    ...columnStyle,
  };
}

export function generateQRCode(qrCode?: string): ContentQr | undefined {
  return qrCode
    ? {
        qr: qrCode,
        fit: 150,
        foreground: 'black',
        background: 'white',
        eccLevel: 'M',
      }
    : undefined;
}

export function verticalSpacing(height: number): ContentText {
  return { text: '\n', fontSize: height };
}

export function getKraj(kod: string): string {
  if (Kraj[kod]) {
    return Kraj[kod];
  }
  return kod;
}

export function generateLine(): Content {
  return {
    table: {
      widths: ['*'],
      body: [[{ text: ' ', fontSize: 1 }]],
    },
    layout: {
      hLineWidth: (i: number): 0 | 1 => (i === 0 ? 1 : 0),
      vLineWidth: (): number => 0,
      hLineColor: function (): string {
        return '#c0bfc1';
      },
      paddingTop: (): number => 0,
      paddingBottom: (): number => 0,
    } as CustomTableLayout,
  };
}

export function makeBreakable(
  value: string | number | undefined,
  wordBreak = 40
): string | number | undefined {
  if (typeof value === 'string') {
    return value.replace(new RegExp(`(.{${wordBreak}})`, 'g'), '$1\u200B');
  }
  return value;
}
