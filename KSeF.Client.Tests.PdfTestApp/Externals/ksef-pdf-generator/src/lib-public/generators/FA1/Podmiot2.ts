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
import { Podmiot2 } from '../../types/fa1.types';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjne } from './PodmiotDaneIdentyfikacyjne';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import { DaneIdentyfikacyjneTPodmiot2Dto } from '../../types/fa2-additional-types';

export function generatePodmiot2(podmiot2: Podmiot2): Content[] {
  const result: Content[] = createHeader('Nabywca');

  result.push(createLabelText('Numer EORI: ', podmiot2.NrEORI));
  if (hasValue(podmiot2.PrefiksNabywcy)) {
    result.push(createLabelText('Prefiks VAT: ', podmiot2.PrefiksNabywcy));
  }
  if (podmiot2.DaneIdentyfikacyjne) {
    if (hasValue(podmiot2.DaneIdentyfikacyjne.NrID)) {
      result.push(createLabelText('Identyfikator podatkowy inny: ', podmiot2.DaneIdentyfikacyjne.NrID));
    }
    if (getValue(podmiot2.DaneIdentyfikacyjne.BrakID) === '1') {
      result.push(createLabelText('Brak identyfikatora ', ' '));
    }
    result.push(
      ...generateDaneIdentyfikacyjne(podmiot2.DaneIdentyfikacyjne as DaneIdentyfikacyjneTPodmiot2Dto)
    );
  }

  if (podmiot2.Adres) {
    result.push(generatePodmiotAdres(podmiot2.Adres, 'Adres', true, [0, 12, 0, 1.3]));
  }
  if (podmiot2.AdresKoresp) {
    result.push(
      ...generatePodmiotAdres(podmiot2.AdresKoresp, 'Adres do korespondencji', true, [0, 12, 0, 1.3])
    );
  }
  if (podmiot2.Email || podmiot2.Telefon) {
    result.push(
      formatText('Dane kontaktowe', [FormatTyp.Label, FormatTyp.LabelMargin]),
      ...generateDaneKontaktowe(podmiot2.Email, getTable(podmiot2.Telefon))
    );
  }
  if (podmiot2.NrKlienta) {
    result.push(createLabelText('Numer klienta: ', podmiot2.NrKlienta));
  }
  return result;
}
