import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  createSection,
  formatText,
  generateTwoColumns,
  getContentTable,
  getTable,
} from '../../../shared/PDF-functions';
import { HeaderDefine } from '../../../shared/types/pdf-types';
import { Fa } from '../../types/fa3.types';
import FormatTyp, { Position } from '../../../shared/enums/common.enum';

export function generateRabat(invoice: Fa): Content[] {
  const faRows = getTable(invoice!.FaWiersz);
  const result: Content[] = [];
  const definedHeader: HeaderDefine[] = [
    { name: 'NrWierszaFay', title: 'Lp.', format: FormatTyp.Default },
    { name: 'P_7', title: 'Nazwa towaru lub usługi', format: FormatTyp.Default },
    { name: 'P_8B', title: 'Ilość', format: FormatTyp.Default },
    { name: 'P_8A', title: 'Miara', format: FormatTyp.Default },
  ];
  const tabRabat = getContentTable<(typeof faRows)[0]>(definedHeader, faRows, '*');
  const isNrWierszaFa = tabRabat.fieldsWithValue.includes('NrWierszaFa');

  result.push(
    ...createHeader('Rabat'),
    ...createLabelText('Wartość rabatu ogółem: ', invoice.P_15, FormatTyp.Currency, {
      alignment: Position.RIGHT,
    }),
    generateTwoColumns(
      formatText(
        `Rabat ${isNrWierszaFa ? 'nie ' : ''}dotyczy wszystkich dostaw towarów i wykonanych usług na rzecz tego nabywcy w danym okresie.`,
        FormatTyp.Default
      ),
      ''
    )
  );
  if (tabRabat.fieldsWithValue.length > 0 && tabRabat.content) {
    result.push(tabRabat.content);
  }

  return createSection(result, true);
}
