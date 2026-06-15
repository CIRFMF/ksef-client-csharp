import { Content } from 'pdfmake/interfaces';
import { createLabelText } from '../../../shared/PDF-functions';
import { DaneIdentyfikacyjne } from '../../types/fa3.types';

export function generateDaneIdentyfikacyjneTPodmiot1Dto(daneIdentyfikacyjne: DaneIdentyfikacyjne): Content[] {
  return [
    createLabelText('NIP: ', daneIdentyfikacyjne.NIP),
    createLabelText('Nazwa: ', daneIdentyfikacyjne.Nazwa),
  ];
}
