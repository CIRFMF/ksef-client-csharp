import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createSection,
  createSubHeader,
  formatText,
  getContentTable,
  getTable,
  getValue,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { DodatkowyOpi, Fa } from '../../types/fa2.types';
import FormatTyp from '../../../shared/enums/common.enum';
import { FormContentState } from '../../../shared/types/additional-data.types';

export function generateDodatkoweInformacje(faVat: Fa): Content[] {
  const tpLabel: Content[] = [];

  if (getValue(faVat.TP) === '1') {
    tpLabel.push(
      formatText('- Istniejące powiązania między nabywcą a dokonującym dostawy towarów lub usługodawcą')
    );
  }

  const fpLabel: Content[] = [];

  if (getValue(faVat.FP) === '1') {
    fpLabel.push(formatText('- Faktura, o której mowa w art. 109 ust. 3d ustawy'));
  }

  const zwrotAkcyzyLabel: Content[] = [];

  if (getValue(faVat.ZwrotAkcyzy) === '1') {
    zwrotAkcyzyLabel.push(
      formatText(
        '- Informacja dodatkowa związana ze zwrotem podatku akcyzowego zawartego w cenie oleju napędowego'
      )
    );
  }

  const labels: Content[][] = [tpLabel, fpLabel, zwrotAkcyzyLabel].filter(
    (el: Content[]): boolean => el.length > 0
  );
  const table: Content[] = [
    ...createHeader('Dodatkowe informacje'),
    ...labels,
    ...generateDodatkowyOpis(faVat.DodatkowyOpis),
  ];

  return table.length > 1 ? createSection(table, true) : [];
}

function generateDodatkowyOpis(fakturaZaliczkowaData: DodatkowyOpi[] | undefined): Content[] {
  if (!fakturaZaliczkowaData) {
    return [];
  }
  const fakturaZaliczkowa: DodatkowyOpi[] = getTable(fakturaZaliczkowaData)?.map(
    (item: DodatkowyOpi, index: number) => ({
      ...item,
      lp: { _text: index + 1 },
    })
  );
  const table: Content[] = createSubHeader('Dodatkowy opis');

  const fakturaZaliczkowaHeader: HeaderDefine[] = [
    {
      name: 'lp',
      title: 'Lp.',
      format: FormatTyp.Default,
      width: 'auto',
    },
    {
      name: 'NrWiersza',
      title: 'Numer wiersza',
      format: FormatTyp.Default,
      width: 'auto',
    },
    {
      name: 'Klucz',
      title: 'Rodzaj informacji',
      format: FormatTyp.Default,
      width: 'auto',
    },
    {
      name: 'Wartosc',
      title: 'Treść informacji',
      format: FormatTyp.Default,
      width: '*',
    },
  ];
  const tableFakturaZaliczkowa: FormContentState = getContentTable<(typeof fakturaZaliczkowa)[0]>(
    fakturaZaliczkowaHeader,
    fakturaZaliczkowa,
    '*',
    [0, 0, 0, 0]
  );

  if (tableFakturaZaliczkowa.content) {
    table.push(tableFakturaZaliczkowa.content);
  }
  return table;
}
