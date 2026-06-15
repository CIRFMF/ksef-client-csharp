import { Content } from 'pdfmake/interfaces';
import {
  createHeader,
  createLabelText,
  generateTwoColumns,
  getTable,
  hasValue,
} from '../../../shared/PDF-functions';
import { Podmiot3Podmiot2KDto } from '../../types/fa2-additional-types';
import { getRolaString } from '../../../shared/generators/common/functions';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjneTPodmiot2Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot2Dto';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import FormatTyp from '../../../shared/enums/common.enum';
import { Podmiot1DaneKontaktowe, Podmiot2K, Podmiot3 } from '../../types/fa2.types';
import { Adres } from '../../types/fa1.types';

export function generateDaneIdentyfikacyjneTPodmiot3Dto(
  podmiot2KDto: Podmiot3Podmiot2KDto | undefined,
  index: number
): Content[] {
  if (!podmiot2KDto) {
    return [];
  }
  const podmiot1: Podmiot3 = podmiot2KDto.fakturaPodmiotNDto;
  const podmiot1DaneKontaktowe: Podmiot1DaneKontaktowe[] = getTable(podmiot1.DaneKontaktowe);
  const podmiot1K: (Podmiot2K & { Adres?: Adres }) | undefined = podmiot2KDto.podmiot2KDto;
  const result: Content[] = createHeader(`Podmiot inny ${index + 1}`);

  if (
    hasValue(podmiot1.NrEORI) ||
    hasValue(podmiot1.Rola) ||
    hasValue(podmiot1.OpisRoli) ||
    hasValue(podmiot1?.Udzial)
  ) {
    result.push(
      ...createHeader('Dane identyfikacyjne'),
      createLabelText('Numer EORI: ', podmiot1.NrEORI),
      createLabelText('Rola: ', getRolaString(podmiot1.Rola, 2)),
      createLabelText('Rola inna: ', podmiot1.OpisRoli),
      createLabelText('Udział: ', podmiot1.Udzial, FormatTyp.Currency6)
    );
  }

  if (podmiot1DaneKontaktowe.length > 0 || hasValue(podmiot1.NrKlienta)) {
    result.push(generateDaneKontaktowe(podmiot1.DaneKontaktowe ?? []));
    result.push(createLabelText('Numer klienta: ', podmiot1.NrKlienta));
  }
  const columns1: Content[] = [
    ...createHeader('Treść korygowana'),
    createLabelText('Identyfikator nabywcy: ', podmiot1K?.IDNabywcy),
  ];

  if (podmiot1K?.DaneIdentyfikacyjne) {
    columns1.push(generateDaneIdentyfikacyjneTPodmiot2Dto(podmiot1K.DaneIdentyfikacyjne));
  }
  if (podmiot1K?.Adres) {
    columns1.push(generatePodmiotAdres(podmiot1K.Adres));
  }
  const columns2: Content[] = [
    ...createHeader('Treść korygująca'),
    createLabelText('Identyfikator nabywcy: ', podmiot1?.IDNabywcy),
  ];

  if (podmiot1?.DaneIdentyfikacyjne) {
    columns2.push(generateDaneIdentyfikacyjneTPodmiot2Dto(podmiot1.DaneIdentyfikacyjne));
  }
  if (podmiot1?.Adres) {
    columns2.push(generatePodmiotAdres(podmiot1.Adres));
  }

  if (podmiot1.AdresKoresp != null) {
    columns2.push(generatePodmiotAdres(podmiot1.AdresKoresp, 'Adres korespondencyjny'));
  }
  result.push(generateTwoColumns(columns1, columns2));
  return result;
}
