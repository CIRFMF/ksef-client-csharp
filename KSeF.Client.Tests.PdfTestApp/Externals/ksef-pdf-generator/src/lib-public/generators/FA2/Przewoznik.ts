import { Content } from 'pdfmake/interfaces';
import { createHeader, generateTwoColumns } from '../../../shared/PDF-functions';
import { Przewoznik } from '../../types/fa2.types';
import { generatePodmiotAdres } from './PodmiotAdres';
import { generateDaneIdentyfikacyjneTPodmiot2Dto } from './PodmiotDaneIdentyfikacyjneTPodmiot2Dto';

export function generatePrzewoznik(przewoznik: Przewoznik | undefined): Content {
  if (!przewoznik) {
    return [];
  }
  return [
    ...createHeader('Przewoźnik'),
    [
      generateTwoColumns(
        generateDaneIdentyfikacyjneTPodmiot2Dto(przewoznik.DaneIdentyfikacyjne as any),
        generatePodmiotAdres(przewoznik.AdresPrzewoznika, 'Adres przewoźnika', true, [0, 0, 0, 0]),
        [0, 0, 0, 8]
      ),
    ],
  ];
}
