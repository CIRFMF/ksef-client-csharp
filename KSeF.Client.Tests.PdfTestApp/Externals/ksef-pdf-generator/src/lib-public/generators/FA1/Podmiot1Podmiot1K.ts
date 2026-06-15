import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  createSubHeader,
  generateColumns,
  getTable,
  verticalSpacing,
} from '../../../shared/PDF-functions';
import { Podmiot1, Podmiot1K } from '../../types/fa1.types';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjne } from './PodmiotDaneIdentyfikacyjne';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';

export function generatePodmiot1Podmiot1K(podmiot1: Podmiot1, podmiot1K: Podmiot1K): Content[] {
  const result: Content[] = createHeader('Sprzedawca');
  let firstColumn: Content[] = [];
  let secondColumn: Content[] = [];

  firstColumn.push(createSubHeader('Dane identyfikacyjne'), createLabelText('Numer EORI: ', podmiot1.NrEORI));
  if (podmiot1.DaneIdentyfikacyjne) {
    firstColumn.push(...generateDaneIdentyfikacyjne(podmiot1.DaneIdentyfikacyjne));
  }

  if (podmiot1.Email || podmiot1.Telefon) {
    firstColumn.push(generateDaneKontaktowe(podmiot1.Email, getTable(podmiot1.Telefon)));
  }
  if (podmiot1.StatusInfoPodatnika) {
    firstColumn.push(createLabelText('Status podatnika: ', podmiot1.StatusInfoPodatnika));
  }
  if (firstColumn.length) {
    result.push({
      columns: [firstColumn, []],
      columnGap: 20,
    });
  }
  firstColumn = generateCorrectedContent(podmiot1K, 'Treść korygowana');
  secondColumn = generateCorrectedContent(podmiot1, 'Treść korygująca');

  if (podmiot1.AdresKoresp) {
    secondColumn.push(
      generatePodmiotAdres(podmiot1.AdresKoresp, 'Adres do korespondencji', true, [0, 12, 0, 1.3])
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

export function generateCorrectedContent(podmiot: Podmiot1 | Podmiot1K, headerText: string): Content[] {
  const result: Content[] = [];

  result.push(createSubHeader(headerText));

  if (podmiot.PrefiksPodatnika?._text) {
    result.push(createLabelText('Prefiks VAT: ', podmiot.PrefiksPodatnika));
  }
  if (podmiot.DaneIdentyfikacyjne) {
    result.push(...generateDaneIdentyfikacyjne(podmiot.DaneIdentyfikacyjne));
  }
  if (podmiot.Adres) {
    result.push(generatePodmiotAdres(podmiot.Adres, 'Adres', true, [0, 12, 0, 1.3]));
  }
  return result;
}
