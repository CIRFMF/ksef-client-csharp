import { Content, ContentTable } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  formatText,
  generateColumns,
  hasValue,
  verticalSpacing,
} from '../../../shared/PDF-functions';
import { Adnotacje } from '../../types/fa1.types';
import FormatTyp from '../../../shared/enums/common.enum';
import { DEFAULT_TABLE_LAYOUT } from '../../../shared/consts/const';

export function generateAdnotacje(adnotacje?: Adnotacje): Content[] {
  const result: Content[] = [];
  let firstColumn: Content[] = [];
  const secondColumn: Content[] = [];

  if (adnotacje) {
    if (adnotacje?.P_19?._text === '1') {
      addToColumn(
        firstColumn,
        secondColumn,
        {
          text: 'Dostawa towarów lub świadczenie usług zwolnionych od podatku na podstawie art. 43 ust. 1, art. 113 ust. 1 i 9 albo przepisów wydanych na podstawie art. 82 ust. 3 lub na podstawie innych przepisów',
        },
        true
      );
      if (adnotacje.P_19A?._text) {
        addToColumn(
          firstColumn,
          secondColumn,
          createLabelText(
            'Podstawa zwolnienia od podatku: ',
            'Przepis ustawy albo aktu wydanego na podstawie ustawy, na podstawie którego podatnik stosuje adnotacje od podatku'
          ),
          true
        );
        addToColumn(
          firstColumn,
          secondColumn,
          createLabelText('Przepis ustawy albo aktu wydanego na podstawie ustawy: ', adnotacje.P_19A._text),
          true
        );
      }
      if (adnotacje.P_19B?._text) {
        addToColumn(
          firstColumn,
          secondColumn,
          createLabelText(
            'Podstawa zwolnienia od podatku: ',
            'Przepis dyrektywy 2006/112/WE, który zwalnia od podatku taką dostawę towarów lub takie świadczenie usług'
          ),
          true
        );
        addToColumn(
          firstColumn,
          secondColumn,
          createLabelText('Przepis dyrektywy: ', adnotacje.P_19B._text),
          true
        );
      }
      if (adnotacje.P_19C?._text) {
        addToColumn(
          firstColumn,
          secondColumn,
          createLabelText(
            'Podstawa zwolnienia od podatku: ',
            'Inna podstawa prawna wskazującą na to, że dostawa towarów lub świadczenie usług korzysta ze zwolnienia'
          ),
          true
        );
        addToColumn(
          firstColumn,
          secondColumn,
          createLabelText('Inna podstawa prawna: ', adnotacje.P_19C._text),
          true
        );
      }
    }

    if (adnotacje.P_18A?._text === '1') {
      addToColumn(firstColumn, secondColumn, { text: 'Mechanizm podzielonej płatności' });
    }
    if (adnotacje.P_16?._text === '1') {
      addToColumn(firstColumn, secondColumn, { text: 'Metoda kasowa' });
    }
    if (adnotacje.P_18?._text === '1') {
      addToColumn(firstColumn, secondColumn, { text: 'Odwrotne obciążenie' });
    }
    if (adnotacje.P_23?._text === '1') {
      addToColumn(firstColumn, secondColumn, { text: 'Procedura trójstronna uproszczona' });
    }

    if (adnotacje.P_PMarzy?._text === '1') {
      let valueMarzy = '';

      if (adnotacje.P_PMarzy_3_1?._text === '1') {
        valueMarzy = 'towary używane';
      } else if (adnotacje.P_PMarzy_3_2?._text === '1') {
        valueMarzy = 'dzieła sztuki';
      } else if (adnotacje.P_PMarzy_2?._text === '1') {
        valueMarzy = 'biura podróży';
      } else if (adnotacje.P_PMarzy_3_3?._text === '1') {
        valueMarzy = 'przedmioty kolekcjonerskie i antyki';
      }
      addToColumn(firstColumn, secondColumn, createLabelText('Procedura marży: ', valueMarzy));
    }

    if (adnotacje.P_17?._text === '1') {
      addToColumn(firstColumn, secondColumn, { text: 'Samofakturowanie' });
    }

    if (adnotacje.P_22?._text === '1') {
      let obowiazekVAT: Content[] = [];

      obowiazekVAT = [...createLabelText('Wewnątrzwspólnotowe dostawy nowych środków transportu', ' ')];
      if (obowiazekVAT) {
        firstColumn = [firstColumn, ...obowiazekVAT];
      }
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

    if (adnotacje.P_22?._text === '1') {
      result.push(generateDostawy(adnotacje));
    }
  }
  return result;
}

export function generateDostawy(adnotacje: Adnotacje): Content[] {
  const result: Content[] = [];
  const table: Content[][] = [];
  const anyP22B =
    hasValue(adnotacje.P_22B) ||
    hasValue(adnotacje.P_22BT) ||
    hasValue(adnotacje.P_22B1) ||
    hasValue(adnotacje.P_22B2) ||
    hasValue(adnotacje.P_22B3) ||
    hasValue(adnotacje.P_22B4);
  const anyP22C: boolean = hasValue(adnotacje.P_22C) || hasValue(adnotacje.P_22C1);
  const anyP22D: boolean = hasValue(adnotacje.P_22D) || hasValue(adnotacje.P_22D1);

  if (hasValue(adnotacje.P_22A)) {
    table.push([
      formatText('Data dopuszczenia nowego środka transportu do użytku', FormatTyp.GrayBoldTitle),
      formatText(adnotacje.P_22A?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(adnotacje.P_22BMK)) {
    table.push([
      formatText('Marka nowego środka transportu', FormatTyp.GrayBoldTitle),
      formatText(adnotacje.P_22BMK?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(adnotacje.P_22BMD)) {
    table.push([
      formatText('Model nowego środka transportu', FormatTyp.GrayBoldTitle),
      formatText(adnotacje.P_22BMD?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(adnotacje.P_22BK)) {
    table.push([
      formatText('Kolor nowego środka transportu', FormatTyp.GrayBoldTitle),
      formatText(adnotacje.P_22BK?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(adnotacje.P_22BNR)) {
    table.push([
      formatText('Numer rejestracyjny nowego środka transportu', FormatTyp.GrayBoldTitle),
      formatText(adnotacje.P_22BNR?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(adnotacje.P_22BRP)) {
    table.push([
      formatText('Rok produkcji nowego środka transportu', FormatTyp.GrayBoldTitle),
      formatText(adnotacje.P_22BRP?._text, FormatTyp.Default),
    ]);
  }
  if (anyP22B) {
    table.push([
      formatText('Rodzaj pojazdu', FormatTyp.GrayBoldTitle),
      formatText(
        'Dostawa dotyczy pojazdów lądowych, o których mowa w art. 2 pkt 10 lit. a ustawy',
        FormatTyp.Default
      ),
    ]);
    if (hasValue(adnotacje.P_22B)) {
      table.push([
        formatText('Przebieg pojazdu', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22B?._text, FormatTyp.Default),
      ]);
    }
    if (hasValue(adnotacje.P_22B1)) {
      table.push([
        formatText('Numer VIN', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22B1?._text, FormatTyp.Default),
      ]);
    }
    if (hasValue(adnotacje.P_22B2)) {
      table.push([
        formatText('Numer nadwozia', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22B2?._text, FormatTyp.Default),
      ]);
    }
    if (hasValue(adnotacje.P_22B3)) {
      table.push([
        formatText('Numer podwozia', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22B3?._text, FormatTyp.Default),
      ]);
    }
    if (hasValue(adnotacje.P_22B4)) {
      table.push([
        formatText('Numer ramy', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22B4?._text, FormatTyp.Default),
      ]);
    }
    if (hasValue(adnotacje.P_22BT)) {
      table.push([
        formatText('Typ nowego środka transportu', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22BT?._text, FormatTyp.Default),
      ]);
    }
  } else if (anyP22C) {
    table.push([
      formatText('Rodzaj pojazdu', FormatTyp.GrayBoldTitle),
      formatText(
        'Dostawa dotyczy jednostek pływających, o których mowa w art. 2 pkt 10 lit. b ustawy',
        FormatTyp.Default
      ),
    ]);
    if (hasValue(adnotacje.P_22C)) {
      table.push([
        formatText('Przebieg pojazdu', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22C?._text, FormatTyp.Default),
      ]);
    }
    if (hasValue(adnotacje.P_22C1)) {
      table.push([
        formatText('Numer kadłuba nowego środka transportu', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22C1?._text, FormatTyp.Default),
      ]);
    }
  } else if (anyP22D) {
    table.push([
      formatText('Rodzaj pojazdu', FormatTyp.GrayBoldTitle),
      formatText(
        'Dostawa dotyczy statków powietrznych, o których mowa w art. 2 pkt 10 lit. c ustawy',
        FormatTyp.Default
      ),
    ]);
    if (hasValue(adnotacje.P_22D)) {
      table.push([
        formatText('Przebieg pojazdu', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22D?._text, FormatTyp.Default),
      ]);
    }
    if (hasValue(adnotacje.P_22D1)) {
      table.push([
        formatText('Numer fabryczny nowego środka transportu<', FormatTyp.GrayBoldTitle),
        formatText(adnotacje.P_22D1?._text, FormatTyp.Default),
      ]);
    }
  }

  if (table.length) {
    result.push([
      {
        unbreakable: true,
        table: {
          body: table,
          widths: ['*', '*'],
        },
        layout: DEFAULT_TABLE_LAYOUT,
      } as ContentTable,
    ]);
  }
  return result;
}

function addToColumn(
  firstColumn: Content[],
  secondColumn: Content[],
  content: Content,
  isFirstColumn?: boolean
): void {
  if (firstColumn.length > secondColumn.length && isFirstColumn) {
    secondColumn.push(content);
    return;
  }
  firstColumn.push(content);
}
