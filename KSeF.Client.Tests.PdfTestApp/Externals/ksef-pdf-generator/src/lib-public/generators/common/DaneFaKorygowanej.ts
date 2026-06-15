import { Content } from 'pdfmake/interfaces';
import { TypKorekty } from '../../../shared/consts/const';
import {
  createHeader,
  createLabelText,
  createSection,
  generateTwoColumns,
  getTable,
} from '../../../shared/PDF-functions';
import { DaneFaKorygowanej, Fa as Fa1 } from '../../types/fa1.types';
import { Fa as Fa2 } from '../../types/fa2.types';
import { Fa as Fa3 } from '../../types/fa3.types';

export function generateDaneFaKorygowanej(invoice?: Fa1 | Fa2 | Fa3): Content[] {
  const result: Content[] = [];
  let firstColumn: Content[] = [];
  let secondColumn: Content[] = [];
  let previousSection: boolean = false;

  if (invoice) {
    const daneFakturyKorygowanej: DaneFaKorygowanej[] = getTable(invoice.DaneFaKorygowanej ?? []);

    if (invoice.NrFaKorygowany) {
      firstColumn.push(createLabelText('Poprawny numer faktury korygowanej: ', invoice.NrFaKorygowany));
    }
    if (invoice.PrzyczynaKorekty) {
      firstColumn.push(
        createLabelText('Przyczyna korekty dla faktur korygujących: ', invoice.PrzyczynaKorekty)
      );
    }
    if (invoice.TypKorekty?._text) {
      firstColumn.push(createLabelText('Typ skutku korekty: ', TypKorekty[invoice.TypKorekty._text]));
    }

    if (firstColumn.length) {
      firstColumn.unshift(createHeader('Dane faktury korygowanej'));
    }

    if (daneFakturyKorygowanej?.length === 1) {
      secondColumn.push(createHeader('Dane identyfikacyjne faktury korygowanej'));
      generateCorrectiveData(daneFakturyKorygowanej[0], secondColumn);
      if (firstColumn.length > 0 || secondColumn.length) {
        if (firstColumn.length) {
          result.push(generateTwoColumns(firstColumn, secondColumn));
        } else {
          result.push(generateTwoColumns(secondColumn, []));
        }
        previousSection = true;
      }
      firstColumn = [];
      secondColumn = [];
    } else {
      if (firstColumn.length > 1) {
        result.push(generateTwoColumns(firstColumn, []));
        previousSection = true;
      }
      firstColumn = [];
      daneFakturyKorygowanej?.forEach((item: DaneFaKorygowanej, index: number): void => {
        if (index % 2 === 0) {
          firstColumn.push(createHeader(`Dane identyfikacyjne faktury korygowanej ${index + 1}`));
          generateCorrectiveData(item, firstColumn);
        } else {
          secondColumn.push(createHeader(`Dane identyfikacyjne faktury korygowanej ${index + 1}`));
          generateCorrectiveData(item, secondColumn);
        }
      });
    }
  }

  if (firstColumn.length && secondColumn.length) {
    result.push(createSection([generateTwoColumns(firstColumn, secondColumn)], previousSection));
  }
  return createSection(result, true);
}

function generateCorrectiveData(data: DaneFaKorygowanej, column: Content[]): void {
  if (data.DataWystFaKorygowanej) {
    column.push(
      createLabelText(
        'Data wystawienia faktury, której dotyczy faktura korygująca: ',
        data.DataWystFaKorygowanej
      )
    );
  }
  if (data.NrFaKorygowanej) {
    column.push(createLabelText('Numer faktury korygowanej: ', data.NrFaKorygowanej));
  }
  if (data.NrKSeFFaKorygowanej) {
    column.push(createLabelText('Numer KSeF faktury korygowanej: ', data.NrKSeFFaKorygowanej));
  }
}
