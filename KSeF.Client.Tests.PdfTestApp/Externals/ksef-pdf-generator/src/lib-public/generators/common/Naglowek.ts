import { Content, ContentText } from 'pdfmake/interfaces';
import { formatText, generateLine } from '../../../shared/PDF-functions';
import { TRodzajFaktury } from '../../../shared/consts/const';
import { Fa as Fa1 } from '../../types/fa1.types';
import { Fa as Fa2 } from '../../types/fa2.types';
import { Fa as Fa3, Zalacznik } from '../../types/fa3.types';
import FormatTyp, { Position } from '../../../shared/enums/common.enum';
import { AdditionalDataTypes } from '../../types/common.types';

export function generateNaglowek(
  fa?: Fa2 | Fa3 | Fa1,
  additionalData?: AdditionalDataTypes,
  zalacznik?: Zalacznik
): Content[] {
  let invoiceName = '???';

  switch (fa?.RodzajFaktury?._text) {
    case TRodzajFaktury.VAT:
      invoiceName = 'Faktura podstawowa';
      break;
    case TRodzajFaktury.ZAL:
      invoiceName = 'Faktura zaliczkowa';
      break;
    case TRodzajFaktury.ROZ:
      invoiceName = 'Faktura rozliczeniowa';
      break;
    case TRodzajFaktury.KOR_ROZ:
      invoiceName = 'Faktura korygująca rozliczeniową';
      break;
    case TRodzajFaktury.KOR_ZAL:
      invoiceName = 'Faktura korygująca zaliczkową';
      break;
    case TRodzajFaktury.KOR:
      if (fa?.OkresFaKorygowanej != null) {
        invoiceName = 'Faktura korygująca zbiorcza (rabat)';
      } else {
        invoiceName = 'Faktura korygująca';
      }
      break;
    case TRodzajFaktury.UPR:
      invoiceName = 'Faktura uproszczona';
      break;
  }

  return [
    {
      text: [
        { text: 'Krajowy System ', fontSize: 18 },
        { text: 'e', color: 'red', bold: true, fontSize: 18 },
        { text: '-Faktur', bold: true, fontSize: 18 },
      ],
    },
    { ...(formatText('Numer Faktury:', FormatTyp.ValueMedium) as ContentText), alignment: Position.RIGHT },
    { ...(formatText(fa?.P_2?._text, FormatTyp.HeaderPosition) as ContentText), alignment: Position.RIGHT },
    {
      ...(formatText(invoiceName, [FormatTyp.ValueMedium, FormatTyp.Default]) as ContentText),
      alignment: Position.RIGHT,
    },
    ...(additionalData?.nrKSeF
      ? [
          {
            text: [
              formatText('Numer KSEF:', FormatTyp.LabelMedium) as ContentText,
              formatText(additionalData?.nrKSeF, FormatTyp.ValueMedium),
            ],
            alignment: Position.RIGHT,
          } as Content,
        ]
      : []),
    ...(additionalData?.isMobile && zalacznik
      ? [
          { stack: [generateLine()], margin: [0, 8, 0, 8] } as Content,
          {
            text: [
              formatText(
                'Uwaga, faktura zawiera załącznik, jednak ze względu na ograniczenia wizualizacji, nie został on uwzględniony w pliku PDF',
                FormatTyp.Bold
              ),
            ],
          },
        ]
      : []),
  ];
}
