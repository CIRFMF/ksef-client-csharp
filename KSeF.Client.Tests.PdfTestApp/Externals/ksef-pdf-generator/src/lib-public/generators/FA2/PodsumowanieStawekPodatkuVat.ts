import { Content, ContentTable, ContentText, TableCell } from 'pdfmake/interfaces';
import {
  createHeader,
  createSection,
  formatText,
  getNumberRounded,
  getValue,
  hasValue,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Fa, Faktura, FP } from '../../types/fa2.types';
import { TaxSummaryTypes } from '../../types/tax-summary.types';
import { DEFAULT_TABLE_LAYOUT } from '../../../shared/consts/const';

export function generatePodsumowanieStawekPodatkuVat(faktura: Faktura): Content[] {
  const AnyP13P14_5Diff0: boolean =
    hasValue(faktura.Fa?.P_13_1) ||
    hasValue(faktura.Fa?.P_13_2) ||
    hasValue(faktura.Fa?.P_13_3) ||
    hasValue(faktura.Fa?.P_13_4) ||
    (hasValue(faktura.Fa?.P_13_5) && (!hasValue(faktura.Fa?.P_14_5) || getValue(faktura.Fa?.P_14_5) == 0)) ||
    hasValue(faktura.Fa?.P_13_6_1) ||
    hasValue(faktura.Fa?.P_13_6_2) ||
    hasValue(faktura.Fa?.P_13_6_3) ||
    hasValue(faktura.Fa?.P_13_7) ||
    hasValue(faktura.Fa?.P_13_8) ||
    hasValue(faktura.Fa?.P_13_9) ||
    hasValue(faktura.Fa?.P_13_10) ||
    hasValue(faktura.Fa?.P_13_11);
  const AnyP13: boolean =
    hasValue(faktura.Fa?.P_13_1) ||
    hasValue(faktura.Fa?.P_13_2) ||
    hasValue(faktura.Fa?.P_13_3) ||
    hasValue(faktura.Fa?.P_13_4) ||
    hasValue(faktura.Fa?.P_13_5) ||
    hasValue(faktura.Fa?.P_13_6_1) ||
    hasValue(faktura.Fa?.P_13_6_2) ||
    hasValue(faktura.Fa?.P_13_6_3) ||
    hasValue(faktura.Fa?.P_13_7) ||
    hasValue(faktura.Fa?.P_13_8) ||
    hasValue(faktura.Fa?.P_13_9) ||
    hasValue(faktura.Fa?.P_13_10) ||
    hasValue(faktura.Fa?.P_13_11);
  const AnyP_14xW: boolean =
    hasValue(faktura.Fa?.P_14_1W) ||
    hasValue(faktura.Fa?.P_14_2W) ||
    hasValue(faktura.Fa?.P_14_3W) ||
    hasValue(faktura.Fa?.P_14_4W);

  let tableBody: TableCell[] = [];
  const table: ContentTable = {
    table: {
      headerRows: 1,
      widths: [],
      body: [] as TableCell[][],
    },
    layout: DEFAULT_TABLE_LAYOUT,
  };

  const definedHeader: Content[] = [
    ...[{ text: 'Lp.', style: FormatTyp.GrayBoldTitle }],
    ...(AnyP13P14_5Diff0 ? [{ text: 'Stawka podatku', style: FormatTyp.GrayBoldTitle }] : []),
    ...(AnyP13 ? [{ text: 'Kwota netto', style: FormatTyp.GrayBoldTitle }] : []),
    ...(AnyP13P14_5Diff0 ? [{ text: 'Kwota podatku', style: FormatTyp.GrayBoldTitle }] : []),
    ...(AnyP13 ? [{ text: 'Kwota brutto', style: FormatTyp.GrayBoldTitle }] : []),
    ...(AnyP_14xW ? [{ text: 'Kwota podatku PLN', style: FormatTyp.GrayBoldTitle }] : []),
  ];

  const widths: Content[] = [
    ...['auto'],
    ...(AnyP13P14_5Diff0 ? ['*'] : []),
    ...(AnyP13 ? ['*'] : []),
    ...(AnyP13P14_5Diff0 ? ['*'] : []),
    ...(AnyP13 ? ['*'] : []),
    ...(AnyP_14xW ? ['*'] : []),
  ];

  if (faktura?.Fa) {
    const summary: TaxSummaryTypes[] = getSummaryTaxRate(faktura.Fa);

    tableBody = summary.map((item: TaxSummaryTypes): (string | number | ContentText)[] => {
      const data = [];

      data.push(item.no);
      if (AnyP13P14_5Diff0) {
        data.push(item.taxRateString);
      }
      if (AnyP13) {
        data.push(formatText(item.net, FormatTyp.Currency));
      }
      if (AnyP13P14_5Diff0) {
        data.push(formatText(item.tax, FormatTyp.Currency));
      }
      if (AnyP13) {
        data.push(formatText(item.gross, FormatTyp.Currency));
      }
      if (AnyP_14xW) {
        data.push(formatText(item.taxPLN, FormatTyp.Currency));
      }
      return data;
    });
  }
  table.table.body = [[...definedHeader], ...tableBody] as TableCell[][];
  table.table.widths = [...widths] as never[];

  return tableBody.length
    ? createSection([...createHeader('Podsumowanie stawek podatku', [0, 0, 0, 8]), table], false)
    : [];
}

export function getSummaryTaxRate(fa: Fa): TaxSummaryTypes[] {
  const summary: TaxSummaryTypes[] = [];

  const AnyP13_1P14_1P14_1WDiff0: boolean =
    hasValueAndDiff0(fa?.P_13_1) || hasValueAndDiff0(fa?.P_14_1) || hasValueAndDiff0(fa?.P_14_1W);
  const AnyP13_2P14_2P14_2WDiff0: boolean =
    hasValueAndDiff0(fa?.P_13_2) || hasValueAndDiff0(fa?.P_14_2) || hasValueAndDiff0(fa?.P_14_2W);
  const AnyP13_3P14_3P14_3WDiff0: boolean =
    hasValueAndDiff0(fa?.P_13_3) || hasValueAndDiff0(fa?.P_14_3) || hasValueAndDiff0(fa?.P_14_3W);
  const AnyP13_4P14_4P14_4WDiff0: boolean =
    hasValueAndDiff0(fa?.P_13_4) || hasValueAndDiff0(fa?.P_14_4) || hasValueAndDiff0(fa?.P_14_4W);
  const AnyP13_5P14_5Diff0: boolean = hasValueAndDiff0(fa?.P_13_5) || hasValueAndDiff0(fa?.P_14_5);
  const AnyP13_6_1Diff0: boolean = hasValueAndDiff0(fa?.P_13_6_1);
  const AnyP13_6_2Diff0: boolean = hasValueAndDiff0(fa?.P_13_6_2);
  const AnyP13_6_3Diff0: boolean = hasValueAndDiff0(fa?.P_13_6_3);
  const AnyP13_7Diff0: boolean = hasValueAndDiff0(fa?.P_13_7);
  const AnyP13_8Diff0: boolean = hasValueAndDiff0(fa?.P_13_8);
  const AnyP13_9Diff0: boolean = hasValueAndDiff0(fa?.P_13_9);
  const AnyP13_10Diff0: boolean = hasValueAndDiff0(fa?.P_13_10);
  const AnyP13_11Diff0: boolean = hasValueAndDiff0(fa?.P_13_11);
  let no: number = 1;

  if (AnyP13_1P14_1P14_1WDiff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_1).toFixed(2),
      gross: (getNumberRounded(fa.P_13_1) + getNumberRounded(fa.P_14_1)).toFixed(2),
      tax: getNumberRounded(fa.P_14_1).toFixed(2),
      taxPLN: getNumberRounded(fa.P_14_1W).toFixed(2),
      taxRateString: '23% lub 22%',
    });
    no++;
  }

  if (AnyP13_2P14_2P14_2WDiff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_2).toFixed(2),
      gross: (getNumberRounded(fa.P_13_2) + getNumberRounded(fa.P_14_2)).toFixed(2),
      tax: getNumberRounded(fa.P_14_2).toFixed(2),
      taxPLN: getNumberRounded(fa.P_14_2W).toFixed(2),
      taxRateString: '8% lub 7%',
    });
    no++;
  }

  if (AnyP13_3P14_3P14_3WDiff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_3).toFixed(2),
      gross: (getNumberRounded(fa.P_13_3) + getNumberRounded(fa.P_14_3)).toFixed(2),
      tax: getNumberRounded(fa.P_14_3).toFixed(2),
      taxPLN: getNumberRounded(fa.P_14_3W).toFixed(2),
      taxRateString: '5%',
    });
    no++;
  }

  if (AnyP13_4P14_4P14_4WDiff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_4).toFixed(2),
      gross: (getNumberRounded(fa.P_13_4) + getNumberRounded(fa.P_14_4)).toFixed(2),
      tax: getNumberRounded(fa.P_14_4).toFixed(2),
      taxPLN: getNumberRounded(fa.P_14_4W).toFixed(2),
      taxRateString: '4% lub 3% lub oo',
    });
    no++;
  }

  if (AnyP13_5P14_5Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_5).toFixed(2),
      gross: getNumberRounded(fa.P_13_5).toFixed(2),
      tax: getNumberRounded(fa.P_14_5).toFixed(2),
      taxPLN: '',
      taxRateString: getValue(fa.P_14_5) != 0 ? 'niepodlegające opodatkowaniu' : '',
    });
    no++;
  }

  if (AnyP13_6_1Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_6_1).toFixed(2),
      gross: getNumberRounded(fa.P_13_6_1).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: '0% - krajowe',
    });
    no++;
  }

  if (AnyP13_6_2Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_6_2).toFixed(2),
      gross: getNumberRounded(fa.P_13_6_2).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: '0% - wdt',
    });
    no++;
  }

  if (AnyP13_6_3Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_6_2).toFixed(2),
      gross: getNumberRounded(fa.P_13_6_2).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: '0% - eksport',
    });
    no++;
  }

  if (AnyP13_7Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_7).toFixed(2),
      gross: getNumberRounded(fa.P_13_7).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: 'zwolnione z opodatkowania',
    });
    no++;
  }

  if (AnyP13_8Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_8).toFixed(2),
      gross: getNumberRounded(fa.P_13_8).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: 'np z wyłączeniem art. 100 ust 1 pkt 4 ustawy',
    });
    no++;
  }

  if (AnyP13_9Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_9).toFixed(2),
      gross: getNumberRounded(fa.P_13_9).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: 'np na podstawie art. 100 ust. 1 pkt 4 ustawy',
    });
    no++;
  }

  if (AnyP13_10Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_10).toFixed(2),
      gross: getNumberRounded(fa.P_13_10).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: 'odwrotne obciążenie',
    });
    no++;
  }

  if (AnyP13_11Diff0) {
    summary.push({
      no,
      net: getNumberRounded(fa.P_13_11).toFixed(2),
      gross: getNumberRounded(fa.P_13_11).toFixed(2),
      tax: '0.00',
      taxPLN: '',
      taxRateString: 'marża',
    });
    no++;
  }

  return summary;
}

function hasValueAndDiff0(value: FP | string | number | undefined): boolean {
  return hasValue(value) && getValue(value) != 0;
}
