import { Content, ContentTable, ContentText, TableCell } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  formatText,
  generateColumns,
  getTable,
  hasValue,
  verticalSpacing,
} from '../../../shared/PDF-functions';
import { Adnotacje, NoweSrodkiTransportu, Zwolnienie } from '../../types/fa2.types';
import FormatTyp from '../../../shared/enums/common.enum';
import { DEFAULT_TABLE_LAYOUT } from '../../../shared/consts/const';
import { FP } from '../../types/fa1.types';

export function generateAdnotacje(adnotacje?: Adnotacje): Content[] {
  const result: Content[] = [];
  let firstColumn: Content[] = [];
  const secondColumn: Content[] = [];

  if (adnotacje) {
    const zwolnienie: Zwolnienie | undefined = adnotacje.Zwolnienie;

    if (zwolnienie?.P_19?._text === '1') {
      firstColumn.push({
        text: 'Dostawa towarów lub świadczenie usług zwolnionych od podatku na podstawie art. 43 ust. 1, art. 113 ust. 1 i 9 albo przepisów wydanych na podstawie art. 82 ust. 3 lub na podstawie innych przepisów',
      });
      if (zwolnienie.P_19A?._text) {
        firstColumn.push(
          createLabelText(
            'Podstawa zwolnienia od podatku: ',
            'Przepis ustawy albo aktu wydanego na podstawie ustawy, na podstawie którego podatnik stosuje zwolnienie od podatku'
          )
        );
        firstColumn.push(
          createLabelText('Przepis ustawy albo aktu wydanego na podstawie ustawy: ', zwolnienie.P_19A._text)
        );
      }
      if (zwolnienie.P_19B?._text) {
        firstColumn.push(
          createLabelText(
            'Podstawa zwolnienia od podatku: ',
            'Przepis dyrektywy 2006/112/WE, który zwalnia od podatku taką dostawę towarów lub takie świadczenie usług'
          )
        );
        firstColumn.push(createLabelText('Przepis dyrektywy: ', zwolnienie.P_19B._text));
      }
      if (zwolnienie.P_19C?._text) {
        firstColumn.push(
          createLabelText(
            'Podstawa zwolnienia od podatku: ',
            'Inna podstawa prawna wskazującą na to, że dostawa towarów lub świadczenie usług korzysta ze zwolnienia'
          )
        );
        firstColumn.push(createLabelText('Inna podstawa prawna: ', zwolnienie.P_19C._text));
      }
    }

    if (
      adnotacje.NoweSrodkiTransportu?.P_42_5?._text === '1' ||
      adnotacje.NoweSrodkiTransportu?.P_42_5?._text === '2'
    ) {
      let obowiazekVAT: Content[] = [];
      let value: string = ' ';

      if (adnotacje.NoweSrodkiTransportu.P_42_5?._text === '1') {
        value = 'Istnieje obowiązek wystawienia dokumentu VAT-22';
      } else if (adnotacje.NoweSrodkiTransportu.P_42_5?._text === '2') {
        value = 'Nie istnieje obowiązek wystawienia dokumentu VAT-22';
      }
      obowiazekVAT = [
        ...createLabelText('Wewnątrzwspólnotowe dostawy nowych środków transportu: ', value ?? ''),
      ];
      if (obowiazekVAT) {
        firstColumn = [firstColumn, ...obowiazekVAT];
      }
    }

    if (adnotacje.P_18A?._text === '1') {
      secondColumn.push({ text: 'Mechanizm podzielonej płatności' });
    }
    if (adnotacje.P_16?._text === '1') {
      secondColumn.push({ text: 'Metoda kasowa' });
    }
    if (adnotacje.P_18?._text === '1') {
      secondColumn.push({ text: 'Odwrotne obciążenie' });
    }
    if (adnotacje.P_23?._text === '1') {
      secondColumn.push({ text: 'Procedura trójstronna uproszczona' });
    }
    if (adnotacje.PMarzy?.P_PMarzy?._text === '1') {
      let valueMarzy: string = '';

      if (adnotacje.PMarzy.P_PMarzy_3_1?._text === '1') {
        valueMarzy = 'towary używane';
      } else if (adnotacje.PMarzy.P_PMarzy_3_2?._text === '1') {
        valueMarzy = 'dzieła sztuki';
      } else if (adnotacje.PMarzy.P_PMarzy_2?._text === '1') {
        valueMarzy = 'biura podróży';
      } else if (adnotacje.PMarzy.P_PMarzy_3_3?._text === '1') {
        valueMarzy = 'przedmioty kolekcjonerskie i antyki';
      }
      secondColumn.push(createLabelText('Procedura marży: ', valueMarzy));
    }
    if (adnotacje.P_17?._text === '1') {
      secondColumn.push({ text: 'Samofakturowanie' });
    }
    if (firstColumn.length || secondColumn.length) {
      result.push(generateColumns([firstColumn, secondColumn]));
    }

    if (result.length) {
      result.unshift(verticalSpacing(1));
      result.unshift(createHeader('Adnotacje'));
      result.unshift(verticalSpacing(1));
      result.push(verticalSpacing(1));
    }

    if (
      adnotacje.NoweSrodkiTransportu?.P_42_5?._text === '1' ||
      adnotacje.NoweSrodkiTransportu?.P_42_5?._text === '2'
    ) {
      result.push(generateDostawy(adnotacje.NoweSrodkiTransportu));
    }
  }
  return result;
}

export function generateDostawy(noweSrodkiTransportu: NoweSrodkiTransportu): Content[] {
  const nowySrodekTransportu: Record<string, FP>[] = getTable(noweSrodkiTransportu.NowySrodekTransportu);
  let tableBody: TableCell[] = [];
  const table: ContentTable = {
    table: {
      headerRows: 1,
      widths: [100, '*'],
      body: [] as TableCell[][],
    },
    layout: DEFAULT_TABLE_LAYOUT,
    marginTop: 4,
  };

  if (nowySrodekTransportu?.length) {
    const definedHeader: Content[] = [
      { text: 'Data dopuszczenia do użytku', style: FormatTyp.GrayBoldTitle },
      { text: 'Opis', style: FormatTyp.GrayBoldTitle },
    ];

    tableBody = nowySrodekTransportu.map((item: Record<string, FP>): (string | ContentText)[] => {
      const value: string[] = [];
      const anyP22B =
        hasValue(item.P_22B) ||
        hasValue(item.P_22BT) ||
        hasValue(item.P_22B1) ||
        hasValue(item.P_22B2) ||
        hasValue(item.P_22B3) ||
        hasValue(item.P_22B4);
      const anyP22C: boolean = hasValue(item.P_22C) || hasValue(item.P_22C1);
      const anyP22D: boolean = hasValue(item.P_22D) || hasValue(item.P_22D1);
      const anyP22N: boolean =
        hasValue(item.P_22B1) || hasValue(item.P_22B2) || hasValue(item.P_22B3) || hasValue(item.P_22B4);

      if (item.P_NrWierszaNST?._text) {
        value.push(item.P_NrWierszaNST._text);
      }
      if (anyP22B) {
        value.push('Dostawa dotyczy pojazdów lądowych, o których mowa w art. 2 pkt 10 lit. a ustawy');
      } else if (anyP22C) {
        value.push('Dostawa dotyczy jednostek pływających, o których mowa w art. 2 pkt 10 lit. b ustawy');
      } else if (anyP22D) {
        value.push('Dostawa dotyczy statków powietrznych, o których mowa w art. 2 pkt 10 lit. c ustawy');
      }
      if (item.DetailsString?._text) {
        value.push(item.DetailsString._text);
      }
      if (anyP22B || anyP22C || anyP22D) {
        value.push(item.P_22B?._text ?? item.P_22C?._text ?? item.P_22D?._text ?? '');
      }
      if (item.P_22C1?._text) {
        value.push(`Numer kadłuba nowego środka transportu:  ${item.P_22C1._text}`);
      }
      if (item.P_22D1?._text) {
        value.push(`Numer fabryczny nowego środka transportu: ${item.P_22D1._text}`);
      }
      if (anyP22N) {
        if (item.P_22B1?._text) {
          value.push(`Numer VIN:  ${item.P_22B1._text}`);
        }
        if (item.P_22B2?._text) {
          value.push(`Numer nadwozia:  ${item.P_22B2._text}`);
        }
        if (item.P_22B3?._text) {
          value.push(`Numer podwozia:  ${item.P_22B3._text}`);
        }
        if (item.P_22B4?._text) {
          value.push(`Numer ramy:  ${item.P_22B4._text}`);
        }
      }
      if (item.P_22BT?._text) {
        value.push(item.P_22BT._text);
      }
      return [formatText(item.P_22A?._text), { text: value.join('\n') }];
    });
    table.table.body = [[...definedHeader], ...tableBody] as TableCell[][];
  }

  return tableBody.length ? [table, verticalSpacing(1)] : [];
}
