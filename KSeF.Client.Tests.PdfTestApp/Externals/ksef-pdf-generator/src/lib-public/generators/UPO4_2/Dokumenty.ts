import { Content } from 'pdfmake/interfaces';
import {
  formatText,
  generateLine,
  getContentTable,
  getTable,
  getValue,
  hasValue,
  verticalSpacing,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { Dokument, IDKontekstu, Potwierdzenie } from '../../types/upo-v4_2.types';
import FormatTyp from '../../../shared/enums/common.enum';
import { FormContentState } from '../../../shared/types/additional-data.types';
import { DEFAULT_TABLE_LAYOUT } from '../../../shared/consts/const';

export function generateDokumnetUPO(potwierdzenie: Potwierdzenie): Content[] {
  const dokumenty: Dokument[] = getTable(potwierdzenie.Dokument);

  const result: Content[] = [];
  const table: Content[] = [];

  result.push(verticalSpacing(4));
  result.push(generateLine());
  result.push(verticalSpacing(8));
  result.push(
    formatText('Urzędowe poświadczenie odbioru dokumentu elektronicznego KSeF', FormatTyp.HeaderPosition)
  );
  result.push(verticalSpacing(8));
  if (hasValue(potwierdzenie.NumerReferencyjnySesji)) {
    table.push([
      formatText('Numer referencyjny sesji: ', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.NumerReferencyjnySesji?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.OpisPotwierdzenia?.Strona)) {
    table.push([
      formatText('Strona dokumentu UPO: ', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.OpisPotwierdzenia?.Strona?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.OpisPotwierdzenia?.LiczbaStron)) {
    table.push([
      formatText('Całkowita liczba stron dokumentu UPO: ', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.OpisPotwierdzenia?.LiczbaStron?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.OpisPotwierdzenia?.ZakresDokumentowOd)) {
    table.push([
      formatText('Zakres dokumentów od: ', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.OpisPotwierdzenia?.ZakresDokumentowOd?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.OpisPotwierdzenia?.ZakresDokumentowDo)) {
    table.push([
      formatText('Zakres dokumentów do: ', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.OpisPotwierdzenia?.ZakresDokumentowDo?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.OpisPotwierdzenia?.CalkowitaLiczbaDokumentow)) {
    table.push([
      formatText('Całkowita liczba dokumentów: ', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.OpisPotwierdzenia?.CalkowitaLiczbaDokumentow?._text, FormatTyp.Default),
    ]);
  }
  const idKontekstu: IDKontekstu | undefined = potwierdzenie?.Uwierzytelnienie?.IdKontekstu;

  if (idKontekstu) {
    let typKontekstu: string = '';
    let id: string | number | undefined;

    if (hasValue(idKontekstu.IdDostawcyUslugPeppol)) {
      typKontekstu = 'Identyfikator Peppol';
      id = getValue(idKontekstu.IdDostawcyUslugPeppol);
    }

    if (hasValue(idKontekstu.Nip)) {
      typKontekstu = 'NIP';
      id = getValue(idKontekstu.Nip);
    }

    if (hasValue(idKontekstu.IdWewnetrzny)) {
      typKontekstu = 'Identyfikator wewnętrzny';
      id = getValue(idKontekstu.IdWewnetrzny);
    }

    if (hasValue(idKontekstu.IdZlozonyVatUE)) {
      typKontekstu = 'Identyfikator złożony';
      id = getValue(idKontekstu.IdZlozonyVatUE);
    }
    table.push([
      formatText('Typ kontekstu: ', FormatTyp.GrayBoldTitle),
      formatText(typKontekstu, FormatTyp.Default),
    ]);
    table.push([
      formatText('Identyfikator kontekstu uwierzytelnienia: ', FormatTyp.GrayBoldTitle),
      formatText(id, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.Uwierzytelnienie?.SkrotDokumentuUwierzytelniajacego)) {
    table.push([
      formatText('Skrót dokumentu uwierzytelniającego: ', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.Uwierzytelnienie?.SkrotDokumentuUwierzytelniajacego?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.NazwaStrukturyLogicznej)) {
    table.push([
      formatText(
        'Nazwa pliku XSD struktury logicznej dotycząca przesłanego dokumentu:',
        FormatTyp.GrayBoldTitle
      ),
      formatText(potwierdzenie.NazwaStrukturyLogicznej?._text, FormatTyp.Default),
    ]);
  }
  if (hasValue(potwierdzenie.KodFormularza)) {
    table.push([
      formatText('Kod formularza przedłożonego dokumentu elektronicznego:', FormatTyp.GrayBoldTitle),
      formatText(potwierdzenie.KodFormularza?._text, FormatTyp.Default),
    ]);
  }
  result.push([
    {
      unbreakable: true,
      table: {
        body: table,
        widths: ['auto', '*'],
      },
      layout: DEFAULT_TABLE_LAYOUT,
    } as Content,
  ]);
  result.push(verticalSpacing(8));
  const definedHeader: HeaderDefine[] = [
    { name: 'lp', title: 'Lp.', format: FormatTyp.Default },
    {
      name: 'NumerKSeFDokumentu',
      title: 'Numer identyfikujący fakturę w KSeF',
      format: FormatTyp.Default,
    },
    { name: 'NumerFaktury', title: 'Numer faktury', format: FormatTyp.Default },
    { name: 'NipSprzedawcy', title: 'NIP Sprzedawcy', format: FormatTyp.Default },
    {
      name: 'DataWystawieniaFaktury',
      title: 'Data wystawienia faktury',
      format: FormatTyp.Date,
    },
    {
      name: 'DataPrzeslaniaDokumentu',
      title: 'Data przesłania do KSeF',
      format: FormatTyp.DateTime,
    },
    {
      name: 'DataNadaniaNumeruKSeF',
      title: 'Data nadania numeru KSeF',
      format: FormatTyp.DateTime,
    },
    {
      name: 'SkrotDokumentu',
      title: 'Wartość funkcji skrótu złożonego dokumentu',
      format: FormatTyp.Default,
      width: '*',
    },
  ];
  const documentData: Dokument[] =
    dokumenty.map((doc: Dokument, index: number): Dokument => {
      return { ...doc, lp: index + 1 };
    }) ?? [];

  const tabDocument: FormContentState = getContentTable<(typeof documentData)[0]>(
    definedHeader,
    documentData,
    'auto'
  );

  if (tabDocument.content) {
    result.push(tabDocument.content);
  }
  return result;
}
