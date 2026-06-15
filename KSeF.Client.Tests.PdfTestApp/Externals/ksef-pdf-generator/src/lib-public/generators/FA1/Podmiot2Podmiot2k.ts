import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  createSubHeader,
  generateColumns,
  getTable,
  getValue,
  hasValue,
  verticalSpacing,
} from '../../../shared/PDF-functions';
import { Podmiot2, Podmiot2K } from '../../types/fa1.types';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjne } from './PodmiotDaneIdentyfikacyjne';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';

export function generatePodmiot2Podmiot2K(podmiot2: Podmiot2, podmiot2K: Podmiot2K): Content[] {
  const result: Content[] = createHeader('Nabywca');
  let firstColumn: Content[] = [];
  let secondColumn: Content[] = [];

  firstColumn.push(createSubHeader('Dane identyfikacyjne'), createLabelText('Numer EORI: ', podmiot2.NrEORI));
  if (podmiot2.DaneIdentyfikacyjne) {
    firstColumn.push(...generateDaneIdentyfikacyjne(podmiot2.DaneIdentyfikacyjne));
  }

  if (podmiot2.Email || podmiot2.Telefon) {
    firstColumn.push(generateDaneKontaktowe(podmiot2.Email, getTable(podmiot2.Telefon)));
  }
  if (podmiot2.NrKlienta) {
    firstColumn.push(createLabelText('Numer klienta: ', podmiot2.NrKlienta));
  }
  if (firstColumn.length) {
    result.push({
      columns: [firstColumn, []],
      columnGap: 20,
    });
  }
  firstColumn = generateCorrectedContent(podmiot2K, 'Treść korygowana');
  secondColumn = generateCorrectedContent(podmiot2, 'Treść korygująca');
  if (podmiot2.AdresKoresp) {
    secondColumn.push(
      generatePodmiotAdres(podmiot2.AdresKoresp, 'Adres do korespondencji', true, [0, 12, 0, 1.3])
    );
  }

  if (firstColumn.length || secondColumn.length) {
    result.push(generateColumns([firstColumn, secondColumn]));
  }
  if (result.length) {
    result.push(verticalSpacing(1));
  }
  return result;
}

export function generateCorrectedContent(podmiot: Podmiot2 | Podmiot2K, headerText: string): Content[] {
  const result: Content[] = [];

  result.push(createSubHeader(headerText));

  if (hasValue(podmiot.PrefiksNabywcy)) {
    result.push(createLabelText('Prefiks VAT: ', podmiot.PrefiksNabywcy));
  }
  if (podmiot.DaneIdentyfikacyjne) {
    if (hasValue(podmiot.DaneIdentyfikacyjne.NrID)) {
      result.push(createLabelText('Identyfikator podatkowy inny: ', podmiot.DaneIdentyfikacyjne.NrID));
    }
    if (getValue(podmiot.DaneIdentyfikacyjne.BrakID) === '1') {
      result.push(createLabelText('Brak identyfikatora ', ' '));
    }
    result.push(...generateDaneIdentyfikacyjne(podmiot.DaneIdentyfikacyjne));
  }
  if (podmiot.Adres) {
    result.push(generatePodmiotAdres(podmiot.Adres, 'Adres', true, [0, 12, 0, 1.3]));
  }
  return result;
}
