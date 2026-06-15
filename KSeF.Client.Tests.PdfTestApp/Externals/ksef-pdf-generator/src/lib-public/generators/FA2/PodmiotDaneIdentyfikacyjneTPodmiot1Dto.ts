import { Content } from 'pdfmake/interfaces';
import { DaneIdentyfikacyjne } from '../../types/fa2.types';
import { createLabelText } from '../../../shared/PDF-functions';

export function generateDaneIdentyfikacyjneTPodmiot1Dto(daneIdentyfikacyjne: DaneIdentyfikacyjne): Content[] {
  return [
    createLabelText('NIP: ', daneIdentyfikacyjne.NIP),
    createLabelText('Nazwa: ', daneIdentyfikacyjne.Nazwa),
  ];
}
