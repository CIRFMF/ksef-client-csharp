import FormatTyp from '../enums/common.enum';

export interface PdfTableData {
  widths?: string[];
  body: PdfFields[][];
  layout?: PdfLayout;
}

export interface PdfTable {
  table: PdfTableData;
  layout?: PdfLayout;
}

export interface PdfLayout {
  fillColor?: (rowIndex: number, node: any, columnIndex: number) => string | null;
  hLineWidth?: (i: number) => number;
  vLineWidth?: () => number;
  paddingTop?: () => number;
  paddingBottom?: () => number;
  paddingLeft?: () => number;
  paddingRight?: () => number;
}

export interface PdfOptionField {
  alignment?: string;
  bold?: boolean;
  color?: string;
  colSpan?: number;
  decoration?: 'underline' | 'lineThrough' | 'overline';
  fontSize?: number;
  italics?: boolean;
  margin?: [left: number, top: number, right: number, bottom: number];
  rowSpan?: number;
}

export interface PdfOptionFields extends PdfOptionField {
  image?: string;
  content?: PdfFields | PdfFields[];
  columns?: PdfFields | PdfFields[] | PdfFields[][];
  table?: PdfTable;
  text?: string | PdfFields;
  layout?: PdfLayout;
}

export interface HeaderDefine {
  name: string;
  title: string;
  format: FormatTyp;
  width?: string;
  mappingData?: Record<string, string>;
}

export interface PdfFP {
  _text?: string;
  _rowSpan?: number;
}

export type PdfFields = PdfOptionFields | string;
