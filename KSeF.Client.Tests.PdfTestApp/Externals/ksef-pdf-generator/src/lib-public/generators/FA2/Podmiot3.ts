import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  formatText,
  generateLine,
  generateTwoColumns,
} from '../../../shared/PDF-functions';
import { Podmiot3 } from '../../types/fa2.types';
import { generateAdres } from './Adres';
import { generateDaneIdentyfikacyjneTPodmiot3Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot3Dto';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import { getRolaString } from '../../../shared/generators/common/functions';
import FormatTyp from '../../../shared/enums/common.enum';

export function generatePodmiot3(podmiot: Podmiot3, index: number): Content[] {
  const result: Content[] = [];

  result.push(generateLine());
  const column1: Content[] = [
    ...createHeader(`Podmiot inny ${index + 1}`),
    createLabelText('Identyfikator nabywcy: ', podmiot.IDNabywcy),
    createLabelText('Numer EORI: ', podmiot.NrEORI),
    ...generateDaneIdentyfikacyjneTPodmiot3Dto(podmiot.DaneIdentyfikacyjne),
    createLabelText('Rola: ', getRolaString(podmiot.Rola, 2)),
    createLabelText('Rola inna: ', podmiot.OpisRoli),
    createLabelText('Udział: ', podmiot.Udzial, [FormatTyp.Percentage]),
  ];

  const column2: Content[] = [];

  if (podmiot.Adres) {
    column2.push(formatText('Adres', [FormatTyp.Label, FormatTyp.LabelMargin]), generateAdres(podmiot.Adres));
  }
  if (podmiot.AdresKoresp) {
    column2.push(
      formatText('Adres do korespondencji', [FormatTyp.Label, FormatTyp.LabelMargin]),
      ...generateAdres(podmiot.AdresKoresp)
    );
  }
  if (podmiot.DaneKontaktowe) {
    column2.push(
      formatText('Dane kontaktowe', [FormatTyp.Label, FormatTyp.LabelMargin]),
      ...generateDaneKontaktowe(podmiot.DaneKontaktowe),
      createLabelText('Numer klienta: ', podmiot.NrKlienta)
    );
  }
  result.push(generateTwoColumns(column1, column2));
  return result;
}
