import { Content } from 'pdfmake/interfaces';
import { Potwierdzenie } from '../../types/upo-v4_2.types';
import { createLabelText, generateTwoColumns } from '../../../shared/PDF-functions';
import { Position } from '../../../shared/enums/common.enum';

export function generateNaglowekUPO(potwierdzenie: Potwierdzenie): Content[] {
  return [
    generateTwoColumns(
      {
        text: [
          { text: 'Krajowy System ', fontSize: 18 },
          { text: 'e', color: 'red', bold: true, fontSize: 18 },
          { text: '-Faktur', bold: true, fontSize: 18 },
        ],
      },
      [
        {
          text: createLabelText(
            'Nazwa pełna podmiotu, któremu doręczono dokument elektroniczny: ',
            potwierdzenie!.NazwaPodmiotuPrzyjmujacego
          ),
          alignment: Position.RIGHT,
        },
        {
          text: createLabelText(
            'Informacja o dokumencie: ',
            'Dokument został zarejestrowany w systemie teleinformatycznym Ministerstwa Finansów'
          ),
          alignment: Position.RIGHT,
        },
      ]
    ),
  ];
}
