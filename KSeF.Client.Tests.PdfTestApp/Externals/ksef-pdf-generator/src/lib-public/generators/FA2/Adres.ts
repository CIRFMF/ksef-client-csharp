import { Content } from 'pdfmake/interfaces';
import { createLabelText, formatText, getKraj } from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { Adres } from '../../types/fa2.types';

export function generateAdres(adres: Adres): Content[] {
  const result: Content[] = [];

  if (adres?.AdresL1) {
    result.push(formatText(adres.AdresL1._text, FormatTyp.Value));
  }
  if (adres?.AdresL2) {
    result.push(formatText(adres.AdresL2._text, FormatTyp.Value));
  }
  if (adres?.KodKraju) {
    result.push(formatText(getKraj(adres.KodKraju._text ?? ''), FormatTyp.Value));
  }
  result.push(...createLabelText('GLN: ', adres.GLN));
  return result;
}
