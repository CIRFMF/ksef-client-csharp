import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  createSection,
  formatText,
  generateTwoColumns,
  getTable,
  getValue,
  hasValue,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Podmiot3 } from '../../types/fa1.types';
import { getRolaString } from '../../../shared/generators/common/functions';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjne } from './PodmiotDaneIdentyfikacyjne';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';

export function generatePodmiot3(podmiot: Podmiot3, index: number): Content[] {
  const result: Content[] = [];

  const column1: Content[] = [
    ...createHeader(`Podmiot inny ${index + 1}`),
    createLabelText('Numer EORI: ', podmiot.NrEORI),
  ];

  if (hasValue(podmiot.DaneIdentyfikacyjne?.NrID)) {
    column1.push(createLabelText('Identyfikator podatkowy inny: ', podmiot.DaneIdentyfikacyjne?.NrID));
  }
  if (getValue(podmiot.DaneIdentyfikacyjne?.BrakID) === '1') {
    column1.push(createLabelText('Brak identyfikatora ', ' '));
  }
  if (podmiot.DaneIdentyfikacyjne) {
    column1.push(...generateDaneIdentyfikacyjne(podmiot.DaneIdentyfikacyjne));
  }
  column1.push([
    createLabelText('Rola: ', getRolaString(podmiot.Rola, 1)),
    createLabelText('Rola inna: ', podmiot.OpisRoli),
    createLabelText('Udział: ', podmiot.Udzial, [FormatTyp.Percentage]),
  ]);
  const column2: Content[] = [];

  if (podmiot.Adres) {
    column2.push(generatePodmiotAdres(podmiot.Adres, 'Adres', true, [0, 12, 0, 1.3]));
  }
  if (podmiot.AdresKoresp) {
    column2.push(
      ...generatePodmiotAdres(podmiot.AdresKoresp, 'Adres do korespondencji', true, [0, 12, 0, 1.3])
    );
  }
  if (podmiot.Email || podmiot.Telefon) {
    column2.push(
      formatText('Dane kontaktowe', [FormatTyp.Label, FormatTyp.LabelMargin]),
      ...generateDaneKontaktowe(podmiot.Email, getTable(podmiot.Telefon))
    );
  }
  if (podmiot.NrKlienta) {
    column2.push(createLabelText('Numer klienta: ', podmiot.NrKlienta));
  }
  result.push(generateTwoColumns(column1, column2));
  return createSection(result, true);
}
