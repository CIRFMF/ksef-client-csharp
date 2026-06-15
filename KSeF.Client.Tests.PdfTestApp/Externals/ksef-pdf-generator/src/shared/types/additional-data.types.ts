import { FP } from '../../lib-public/types/fa3.types';
import { ContentTable, Style } from 'pdfmake/interfaces';
import FormatTyp from '../enums/common.enum';

export interface CreateLabelTextData {
  value: FP | string | number | undefined;
  formatTyp?: FormatTyp | FormatTyp[];
  style?: Style;
  currency?: string;
}

export interface FormContentState {
  content: ContentTable | null;
  fieldsWithValue: string[];
}
