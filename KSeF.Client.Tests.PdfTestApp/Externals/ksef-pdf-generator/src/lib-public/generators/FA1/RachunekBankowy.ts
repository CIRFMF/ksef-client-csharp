import { Content, ContentTable } from 'pdfmake/interfaces';
import {
  createHeader,
  createSection,
  formatText,
  getValue,
  hasValue,
  makeBreakable,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { FP } from '../../types/fa1.types';
import { getTypRachunkowWlasnych } from '../../../shared/generators/common/functions';
import { DEFAULT_TABLE_LAYOUT } from '../../../shared/consts/const';

export const generujRachunekBankowy: (accounts?: Record<string, FP>[], title?: string) => Content[] = (
  accounts?: Record<string, FP>[],
  title?: string
): Content[] => {
  const result: Content[] = [];

  if (!accounts?.length) {
    return [];
  }

  accounts.forEach((account: Record<string, FP>, index: number): void => {
    const table: Content[][] = [];
    const base: Content[] = createHeader(
      title ? `${title} ${accounts?.length > 1 ? ++index : ''}` : '',
      [0, 12, 0, 8]
    );

    if (hasValue(account.NrRBZagr)) {
      table.push([
        formatText('Format rachunku', FormatTyp.GrayBoldTitle),
        formatText('Zagraniczny', FormatTyp.Default),
      ]);
    } else if (hasValue(account.NrRBPL)) {
      table.push([
        formatText('Format rachunku', FormatTyp.GrayBoldTitle),
        formatText('Polski', FormatTyp.Default),
      ]);
    }
    if (hasValue(account.NrRBPL)) {
      table.push([
        formatText('Pełny numer rachunku w standardzie NRB', FormatTyp.GrayBoldTitle),
        formatText(getValue(account.NrRBPL), FormatTyp.Default),
      ]);
    }
    if (hasValue(account.NrRBZagr)) {
      table.push([
        formatText('Pełny numer rachunku zagranicznego', FormatTyp.GrayBoldTitle),
        formatText(getValue(account.NrRBZagr), FormatTyp.Default),
      ]);
    }
    table.push([
      formatText('Kod SWIFT', FormatTyp.GrayBoldTitle),
      formatText(getValue(account.SWIFT), FormatTyp.Default),
    ]);
    table.push([
      formatText('Rachunek własny banku', FormatTyp.GrayBoldTitle),
      formatText(makeBreakable(getTypRachunkowWlasnych(account.RachunekWlasnyBanku), 20), FormatTyp.Default),
    ]);
    table.push([
      formatText('Nazwa banku', FormatTyp.GrayBoldTitle),
      formatText(makeBreakable(getValue(account.NazwaBanku), 20), FormatTyp.Default),
    ]);
    result.push([
      ...base,
      {
        unbreakable: true,
        table: {
          body: table,
          widths: ['*', 'auto'],
        },
        layout: DEFAULT_TABLE_LAYOUT,
      } as ContentTable,
    ]);
  });

  return createSection(result, false);
};
