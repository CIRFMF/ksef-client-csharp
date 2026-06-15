import { Content, ContentText } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  formatText,
  generateLine,
  generateTwoColumns,
  getContentTable,
  getTable,
  getValue,
  hasValue,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { Platnosc } from '../../types/fa3.types';
import { getFormaPlatnosciString } from '../../../shared/generators/common/functions';
import { generujRachunekBankowy } from './RachunekBankowy';
import FormatTyp from '../../../shared/enums/common.enum';

export function generatePlatnosc(platnosc: Platnosc | undefined): Content {
  if (!platnosc) {
    return [];
  }
  const terminPlatnosci = getTable(platnosc.TerminPlatnosci);

  const zaplataCzesciowaHeader: HeaderDefine[] = [
    {
      name: 'Termin',
      title: 'Termin płatności',
      format: FormatTyp.Default,
    },
  ];

  if (terminPlatnosci.some((termin) => termin.TerminOpis)) {
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

  //  TODO: Add to FA2 and FA1? (KSEF20-15289)
  if (getValue(platnosc.Zaplacono) === '1') {
    table.push(createLabelText('Informacja o płatności: ', 'Zapłacono'));
    table.push(createLabelText('Data zapłaty: ', platnosc.DataZaplaty));
  } else if (
    getValue(platnosc.ZnacznikZaplatyCzesciowej) === '1' ||
    getValue(platnosc.ZnacznikZaplatyCzesciowej) === '2'
  ) {
    table.push(createLabelText('Informacja o płatności: ', 'Zapłata częściowa'));
    table.push(
      createLabelText(
        'Informacja o płatności (kontynuacja): ',
        getValue(platnosc.ZnacznikZaplatyCzesciowej) === '1'
          ? 'Zapłacono w części'
          : 'Zapłacono całość w częściach'
      )
    );
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

  const zaplataCzesciowa = getTable(platnosc.ZaplataCzesciowa);
  const tableZaplataCzesciowa = getContentTable<(typeof zaplataCzesciowa)[0]>(
    zaplataCzesciowaNaglowek,
    zaplataCzesciowa,
    '*',
    undefined,
    20
  );
  const terminPatnosciContent = terminPlatnosci.map((platnosc) => {
    if (!terminPlatnosci.some((termin) => termin.TerminOpis)) {
      return platnosc;
    } else {
      return {
        ...platnosc,
        TerminOpis: {
          _text: `${platnosc.TerminOpis?.Ilosc?._text ?? ''} ${platnosc.TerminOpis?.Jednostka?._text ?? ''} ${platnosc.TerminOpis?.ZdarzeniePoczatkowe?._text ?? ''}`,
        } as any,
      };
    }
  });

  const tableTerminPlatnosci = getContentTable<(typeof terminPlatnosci)[0]>(
    zaplataCzesciowaHeader,
    terminPatnosciContent,
    '*',
    undefined,
    20
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

  if (platnosc.LinkDoPlatnosci) {
    table.push(formatText('Link do płatności bezgotówkowej: ', FormatTyp.Label));
    table.push({
      text: formatText(platnosc.LinkDoPlatnosci._text, FormatTyp.Link),
      link: formatText(platnosc.LinkDoPlatnosci._text, FormatTyp.Link),
    } as ContentText);
  }
  if (platnosc.IPKSeF?._text) {
    table.push(createLabelText('Identyfikator płatności Krajowego Systemu e-Faktur: ', platnosc.IPKSeF));
  }

  table.push(
    generateTwoColumns(
      generujRachunekBankowy(getTable(platnosc.RachunekBankowy), 'Numer rachunku bankowego'),
      generujRachunekBankowy(getTable(platnosc.RachunekBankowyFaktora), 'Numer rachunku bankowego faktora')
    )
  );

  if (platnosc.Skonto) {
    table.push(createHeader('Skonto', [0, 0]));
    table.push(createLabelText('Warunki skonta: ', platnosc.Skonto.WarunkiSkonta));
    table.push(createLabelText('Wysokość skonta: ', platnosc.Skonto.WysokoscSkonta));
  }
  return table;
}
