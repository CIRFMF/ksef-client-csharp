import { Content } from 'pdfmake/interfaces';
import {
  createLabelText,
  formatText,
  getTable,
  hasValue,
  verticalSpacing,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { PodmiotUpowaznionyDaneKontaktowe } from '../../types/fa2.types';

export function generatePodmiotUpowaznionyDaneKontaktowe(
  daneKontaktoweSource: PodmiotUpowaznionyDaneKontaktowe[] | undefined
): Content[] {
  if (!daneKontaktoweSource) {
    return [];
  }
  const result: Content[] = [formatText('Dane kontaktowe', FormatTyp.Description)];
  const daneKontaktowe: PodmiotUpowaznionyDaneKontaktowe[] = getTable(daneKontaktoweSource);

  if (daneKontaktowe.length === 0) {
    return [];
  }
  daneKontaktowe.forEach((kontakt: PodmiotUpowaznionyDaneKontaktowe): void => {
    if (hasValue(kontakt.EmailPU)) {
      result.push(createLabelText('E-mail: ', kontakt.EmailPU));
    }
    if (hasValue(kontakt.TelefonPU)) {
      result.push(createLabelText('Tel.: ', kontakt.TelefonPU));
    }
    result.push(verticalSpacing(1));
  });
  return result;
}
