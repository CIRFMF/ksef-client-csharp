import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  formatText,
  generateTwoColumns,
  getTable,
  getValue,
  hasValue,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { PodmiotUpowazniony } from '../../types/fa1.types';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjne } from './PodmiotDaneIdentyfikacyjne';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import { getRolaUpowaznionegoString } from '../../../shared/generators/common/functions';

export function generatePodmiotUpowazniony(podmiot: PodmiotUpowazniony | undefined): Content[] {
  if (!podmiot) {
    return [];
  }
  const result: Content[] = createHeader('Podmiot upoważniony');
  const columnLeft: Content[] = [];
  const columnRight: Content[] = [];

  if (hasValue(podmiot.RolaPU)) {
    columnLeft.push(createLabelText('Rola: ', getRolaUpowaznionegoString(podmiot.RolaPU, 1)));
  }
  if (hasValue(podmiot.NrEORI)) {
    columnLeft.push(createLabelText('Numer EORI: ', podmiot.NrEORI));
  }
  if (podmiot.DaneIdentyfikacyjne) {
    if (hasValue(podmiot.DaneIdentyfikacyjne.NrID)) {
      columnLeft.push(createLabelText('Identyfikator podatkowy inny: ', podmiot.DaneIdentyfikacyjne.NrID));
    }
    if (getValue(podmiot.DaneIdentyfikacyjne.BrakID) === '1') {
      columnLeft.push(createLabelText('Brak identyfikatora ', ' '));
    }
    columnLeft.push(generateDaneIdentyfikacyjne(podmiot.DaneIdentyfikacyjne));
  }

  if (podmiot.Adres) {
    columnRight.push(generatePodmiotAdres(podmiot.Adres, 'Adres', true));
  }
  if (podmiot.AdresKoresp) {
    columnRight.push(generatePodmiotAdres(podmiot.AdresKoresp, 'Adres do korespondencji', true));
  }
  if (podmiot.EmailPU || podmiot.TelefonPU) {
    columnRight.push(
      formatText('Dane kontaktowe', [FormatTyp.Label]),
      ...generateDaneKontaktowe(podmiot.EmailPU, getTable(podmiot.TelefonPU))
    );
  }
  result.push(generateTwoColumns(columnLeft, columnRight));
  return result;
}
