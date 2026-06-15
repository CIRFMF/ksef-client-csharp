import { Content } from 'pdfmake/interfaces';
import { createLabelText, getTable } from '../../../shared/PDF-functions';
import { Podmiot1DaneKontaktowe } from '../../types/fa3.types';

export function generateDaneKontaktowe(daneKontaktowe: Podmiot1DaneKontaktowe[]): Content[] {
  return getTable(daneKontaktowe)?.map((daneKontaktowe) => {
    return [
      createLabelText('E-mail: ', daneKontaktowe.Email),
      createLabelText('Tel.: ', daneKontaktowe.Telefon),
    ];
  });
}
