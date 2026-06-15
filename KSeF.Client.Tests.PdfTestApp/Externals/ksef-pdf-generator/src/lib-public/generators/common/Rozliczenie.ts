import { Content, ContentTable } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  createLabelTextArray,
  createSection,
  createSubHeader,
  generateColumns,
  generateTwoColumns,
  getContentTable,
  getTable,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { Rozliczenie as Rozliczenie1, Zenia } from '../../types/fa1.types';
import { Rozliczenie as Rozliczenie2, Rozliczenie as Rozliczenie3 } from '../../types/fa2.types';
import FormatTyp, { Position } from '../../../shared/enums/common.enum';
import { FormContentState } from '../../../shared/types/additional-data.types';

export function generateRozliczenie(
  rozliczenie: Rozliczenie1 | Rozliczenie2 | Rozliczenie3 | undefined,
  KodWaluty: string
): Content[] {
  if (!rozliczenie) {
    return [];
  }
  const obciazenia: Zenia[] = getTable(rozliczenie?.Obciazenia);
  const odliczenia: Zenia[] = getTable(rozliczenie?.Odliczenia);
  const result: Content[] = [];
  const headerOdliczenia: HeaderDefine[] = [
    {
      title: 'Powód odliczenia',
      name: 'Powod',
      format: FormatTyp.Default,
    },
    {
      title: 'Kwota odliczenia',
      name: 'Kwota',
      format: FormatTyp.Currency,
    },
  ];
  const headerObciazenia: HeaderDefine[] = [
    {
      name: 'Powod',
      title: 'Powód obciążenia',
      format: FormatTyp.Default,
    },
    {
      name: 'Kwota',
      title: 'Kwota obciążenia',
      format: FormatTyp.Currency,
    },
  ];
  const tableObciazenia: FormContentState = getContentTable<(typeof obciazenia)[0]>(
    headerObciazenia,
    obciazenia,
    '*',
    undefined,
    20
  );
  const tableOdliczenia: FormContentState = getContentTable<(typeof odliczenia)[0]>(
    headerOdliczenia,
    odliczenia,
    '*',
    undefined,
    20
  );
  const SumaObciazen: Content[] = createLabelText(
    'Suma kwot obciążenia: ',
    rozliczenie.SumaObciazen,
    FormatTyp.Currency,
    {
      alignment: Position.RIGHT,
    }
  );
  const Sumaodliczen: Content[] = createLabelText(
    'Suma kwot odliczenia: ',
    rozliczenie?.SumaOdliczen,
    FormatTyp.Currency,
    {
      alignment: Position.RIGHT,
    }
  );
  const resultObciazenia: (ContentTable | Content[])[] = [
    createSubHeader('Obciążenia'),
    tableObciazenia.content ?? [],
    SumaObciazen,
  ];
  const resultOdliczenia: (ContentTable | Content[])[] = [
    createSubHeader('Odliczenia'),
    tableOdliczenia.content ?? [],
    Sumaodliczen,
  ];

  result.push(createHeader('Rozliczenie', [0, 8, 0, 4]));
  if (obciazenia.length > 0 && odliczenia.length > 0) {
    result.push(generateColumns([resultObciazenia, resultOdliczenia]));
  } else if (obciazenia.length > 0) {
    result.push(generateTwoColumns([resultObciazenia], []));
  } else if (odliczenia.length > 0) {
    result.push(generateTwoColumns([], [resultOdliczenia]));
  }

  if (rozliczenie?.DoZaplaty?._text) {
    result.push({
      stack: createLabelTextArray([
        { value: 'Do zapłaty: ', formatTyp: FormatTyp.LabelGreater },
        { value: rozliczenie?.DoZaplaty, formatTyp: FormatTyp.CurrencyGreater, currency: KodWaluty },
      ]),
      alignment: Position.RIGHT,
      margin: [0, 8, 0, 0],
    });
  } else if (rozliczenie?.DoRozliczenia?._text) {
    result.push({
      stack: createLabelTextArray([
        { value: 'Do rozliczenia: ', formatTyp: FormatTyp.LabelGreater },
        { value: rozliczenie?.DoRozliczenia, formatTyp: FormatTyp.CurrencyGreater, currency: KodWaluty },
      ]),
      alignment: Position.RIGHT,
      marginTop: 8,
    });
  }

  return createSection(result, true);
}
