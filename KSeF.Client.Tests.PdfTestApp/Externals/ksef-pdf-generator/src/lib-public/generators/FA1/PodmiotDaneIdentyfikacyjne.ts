import { Content } from 'pdfmake/interfaces';
import { createLabelText, getValue, hasValue } from '../../../shared/PDF-functions';
import { DaneIdentyfikacyjneTPodmiot2Dto } from '../../types/fa2-additional-types';

export function generateDaneIdentyfikacyjne(daneIdentyfikacyjne: DaneIdentyfikacyjneTPodmiot2Dto): Content[] {
  const result: Content[] = [];

  result.push(createLabelText('NIP: ', daneIdentyfikacyjne.NIP));
  if (hasValue(daneIdentyfikacyjne.ImiePierwsze) || hasValue(daneIdentyfikacyjne.Nazwisko)) {
    result.push(
      createLabelText(
        '',
        `${getValue(daneIdentyfikacyjne.ImiePierwsze)} ${getValue(daneIdentyfikacyjne.Nazwisko)}`
      )
    );
  }
  if (daneIdentyfikacyjne.PelnaNazwa) {
    result.push(createLabelText('Pełna nazwa: ', daneIdentyfikacyjne.PelnaNazwa));
  }
  if (daneIdentyfikacyjne.Nazwisko) {
    result.push(createLabelText('Nazwa handlowa: ', daneIdentyfikacyjne.NazwaHandlowa));
  }
  return result;
}
