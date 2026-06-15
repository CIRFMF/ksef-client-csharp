import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  generateLine,
  generateTwoColumns,
  getContentTable,
  getTable,
  hasValue,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { Platnosc, TerminPlatnosci, ZaplataCzesciowa } from '../../types/fa2.types';
import { getFormaPlatnosciString } from '../../../shared/generators/common/functions';
import { generujRachunekBankowy } from './RachunekBankowy';
import FormatTyp from '../../../shared/enums/common.enum';
import { FP } from '../../types/fa1.types';
import { FormContentState } from '../../../shared/types/additional-data.types';

export function generatePlatnosc(platnosc: Platnosc | undefined): Content {
  if (!platnosc) {
    return [];
  }

  const terminPlatnosci: TerminPlatnosci[] = getTable(platnosc.TerminPlatnosci);

  const zaplataCzesciowaHeader: HeaderDefine[] = [
    {
      name: 'Termin',
      title: 'Termin płatności',
      format: FormatTyp.Default,
    },
  ];

  if (terminPlatnosci.some((termin: TerminPlatnosci): FP | undefined => termin.TerminOpis)) {
    zaplataCzesciowaHeader.push({ name: 'TerminOpis', title: 'Opis płatności', format: FormatTyp.Default });
  }

  const zaplataCzesciowaNaglowek: HeaderDefine[] = [
    {
      name: 'DataZaplatyCzesciowej',
      title: 'Data zapłaty częściowej',
      format: FormatTyp.Default,
    },
    { name: 'KwotaZaplatyCzesciowej', title: 'Kwota zapłaty częściowej', format: FormatTyp.Currency },
    { name: 'FormaPlatnosci', title: 'Forma płatności', format: FormatTyp.FormOfPayment },
  ];

  const table: Content[] = [generateLine(), ...createHeader('Płatność')];

  if (platnosc.Zaplacono?._text === '1') {
    table.push(createLabelText('Informacja o płatności: ', 'Zapłacono'));
    table.push(createLabelText('Data zapłaty: ', platnosc.DataZaplaty));
  } else if (platnosc.ZnacznikZaplatyCzesciowej?._text === '1') {
    table.push(createLabelText('Informacja o płatności: ', 'Zapłata częściowa'));
  } else {
    table.push(createLabelText('Informacja o płatności: ', 'Brak zapłaty'));
  }

  if (hasValue(platnosc.FormaPlatnosci)) {
    table.push(createLabelText('Forma płatności: ', getFormaPlatnosciString(platnosc.FormaPlatnosci)));
  } else {
    if (platnosc.OpisPlatnosci?._text) {
      table.push(createLabelText('Forma płatności: ', 'Płatność inna'));
      table.push(createLabelText('Opis płatności innej: ', platnosc.OpisPlatnosci));
    }
  }

  const zaplataCzesciowa: ZaplataCzesciowa[] = getTable(platnosc.ZaplataCzesciowa);
  const tableZaplataCzesciowa: FormContentState = getContentTable<(typeof zaplataCzesciowa)[0]>(
    zaplataCzesciowaNaglowek,
    zaplataCzesciowa,
    '*'
  );

  const terminPatnosciContent: TerminPlatnosci[] = terminPlatnosci.map(
    (platnosc: TerminPlatnosci): TerminPlatnosci => {
      if (!terminPlatnosci.some((termin: TerminPlatnosci): FP | undefined => termin.TerminOpis)) {
        return platnosc;
      } else {
        return {
          ...platnosc,
          TerminOpis: {
            _text: `${platnosc.Termin?._text ?? ''}`,
          } as any,
        };
      }
    }
  );

  const tableTerminPlatnosci: FormContentState = getContentTable<(typeof terminPlatnosci)[0]>(
    zaplataCzesciowaHeader,
    terminPatnosciContent,
    '*'
  );

  if (zaplataCzesciowa.length > 0 && terminPlatnosci.length > 0) {
    table.push(
      generateTwoColumns(
        tableZaplataCzesciowa.content ?? [],
        tableTerminPlatnosci.content ?? [],
        [0, 4, 0, 0]
      )
    );
  } else if (terminPlatnosci.length > 0) {
    if (tableTerminPlatnosci.content) {
      table.push(generateTwoColumns([], tableTerminPlatnosci.content));
    }
  } else if (zaplataCzesciowa.length > 0 && tableZaplataCzesciowa.content) {
    table.push(tableZaplataCzesciowa.content);
  }

  table.push(
    generateTwoColumns(
      generujRachunekBankowy(
        getTable(platnosc.RachunekBankowy as Record<string, FP>[]),
        'Numer rachunku bankowego'
      ),
      generujRachunekBankowy(
        getTable(platnosc.RachunekBankowyFaktora as Record<string, FP>[]),
        'Numer rachunku bankowego faktora'
      )
    )
  );

  if (platnosc.Skonto) {
    table.push(createHeader('Skonto', [0, 0]));
    table.push(createLabelText('Warunki skonta: ', platnosc.Skonto.WarunkiSkonta));
    table.push(createLabelText('Wysokość skonta: ', platnosc.Skonto.WysokoscSkonta));
  }
  return table;
}
