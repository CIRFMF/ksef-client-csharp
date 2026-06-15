import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  formatText,
  getTable,
  getValue,
  hasValue,
} from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Podmiot1 } from '../../types/fa1.types';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjne } from './PodmiotDaneIdentyfikacyjne';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import { TAXPAYER_STATUS } from '../../../shared/consts/const';

export function generatePodmiot1(podmiot1: Podmiot1): Content[] {
  const result: Content[] = createHeader('Sprzedawca');

  result.push(
    createLabelText('Numer EORI: ', podmiot1.NrEORI),
    createLabelText('Prefiks VAT: ', podmiot1.PrefiksPodatnika)
  );
  if (podmiot1.DaneIdentyfikacyjne) {
    result.push(...generateDaneIdentyfikacyjne(podmiot1.DaneIdentyfikacyjne));
  }

  if (podmiot1.Adres) {
    result.push(generatePodmiotAdres(podmiot1.Adres, 'Adres', true, [0, 12, 0, 1.3]));
  }
  if (podmiot1.AdresKoresp) {
    result.push(
      ...generatePodmiotAdres(podmiot1.AdresKoresp, 'Adres do korespondencji', true, [0, 12, 0, 1.3])
    );
  }
  if (podmiot1.Email || podmiot1.Telefon) {
    result.push(
      formatText('Dane kontaktowe', [FormatTyp.Label, FormatTyp.LabelMargin]),
      ...generateDaneKontaktowe(podmiot1.Email, getTable(podmiot1.Telefon))
    );
  }
  if (hasValue(podmiot1.StatusInfoPodatnika)) {
    const statusInfo: string = TAXPAYER_STATUS[getValue(podmiot1.StatusInfoPodatnika)!];

    result.push(createLabelText('Status podatnika: ', statusInfo));
  }
  return result;
}
