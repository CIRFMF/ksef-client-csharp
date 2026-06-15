import { Content } from 'pdfmake/interfaces';
import { createHeader, createLabelText, formatText, getTable } from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Podmiot2 } from '../../types/fa3.types';
import { generateAdres } from './Adres';
import { generateDaneIdentyfikacyjneTPodmiot2Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot2Dto';
import { generateDaneKontaktowe } from './PodmiotDaneKontaktowe';
import { DaneIdentyfikacyjneTPodmiot2Dto } from '../../types/fa2-additional-types';

export function generatePodmiot2(podmiot2: Podmiot2): Content[] {
  const result: Content[] = createHeader('Nabywca');

  result.push(
    createLabelText('Identyfikator nabywcy: ', podmiot2.IDNabywcy),
    createLabelText('Numer EORI: ', podmiot2.NrEORI)
  );
  if (podmiot2.DaneIdentyfikacyjne) {
    result.push(
      ...generateDaneIdentyfikacyjneTPodmiot2Dto(
        podmiot2.DaneIdentyfikacyjne as DaneIdentyfikacyjneTPodmiot2Dto
      )
    );
  }

  if (podmiot2.Adres) {
    result.push(formatText('Adres', [FormatTyp.Label, FormatTyp.LabelMargin]), generateAdres(podmiot2.Adres));
  }
  if (podmiot2.AdresKoresp) {
    result.push(
      formatText('Adres do korespondencji', [FormatTyp.Label, FormatTyp.LabelMargin]),
      ...generateAdres(podmiot2.AdresKoresp)
    );
  }
  if (podmiot2.DaneKontaktowe || podmiot2.NrKlienta) {
    result.push(
      formatText('Dane kontaktowe', [FormatTyp.Label, FormatTyp.LabelMargin]),
      ...generateDaneKontaktowe(podmiot2.DaneKontaktowe ?? []),
      createLabelText('Numer klienta: ', podmiot2.NrKlienta)
    );

    const daneKontaktowe = getTable(podmiot2.DaneKontaktowe);

    if (daneKontaktowe.length) {
      createLabelText(
        'Faktura dotyczy jednostki podrzędnej JST: ',
        daneKontaktowe[0].JST?._text === '1' ? 'TAK' : 'NIE'
      );
      createLabelText(
        'Faktura dotyczy członka grupy GV: ',
        daneKontaktowe[0].GV?._text === '1' ? 'TAK' : 'NIE'
      );
    }
  }
  return result;
}
