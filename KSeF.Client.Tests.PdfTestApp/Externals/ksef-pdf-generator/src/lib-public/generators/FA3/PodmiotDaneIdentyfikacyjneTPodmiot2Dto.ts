import { Content } from 'pdfmake/interfaces';
import { createLabelText, createLabelTextArray, formatText } from '../../../shared/PDF-functions';
import FormatTyp from '../../../shared/enums/common.enum';
import { DaneIdentyfikacyjneTPodmiot2Dto } from '../../types/fa2-additional-types';

export function generateDaneIdentyfikacyjneTPodmiot2Dto(
  daneIdentyfikacyjne: DaneIdentyfikacyjneTPodmiot2Dto
): Content[] {
  const result: Content[] = [];

  result.push(createLabelText('NIP: ', daneIdentyfikacyjne.NIP));
  if (daneIdentyfikacyjne.NrVatUE?._text) {
    result.push(
      createLabelTextArray([
        { value: 'Numer VAT-UE: ', formatTyp: FormatTyp.Label },
        { value: daneIdentyfikacyjne.KodUE, formatTyp: FormatTyp.Value },
        { value: ' ' },
        { value: daneIdentyfikacyjne.NrVatUE, formatTyp: FormatTyp.Value },
      ])
    );
  }
  if (daneIdentyfikacyjne.KodKraju?._text) {
    result.push(
      createLabelTextArray([
        { value: 'Identyfikator podatkowy inny: ', formatTyp: FormatTyp.Label },
        { value: daneIdentyfikacyjne.KodKraju, formatTyp: FormatTyp.Value },
        { value: ' ' },
        { value: daneIdentyfikacyjne.NrID, formatTyp: FormatTyp.Value },
      ])
    );
  }
  if (daneIdentyfikacyjne.BrakID?._text === '1') {
    result.push(formatText('Brak identyfikatora', FormatTyp.Label));
  }
  result.push(createLabelText('Nazwa: ', daneIdentyfikacyjne.Nazwa));
  return result;
}
