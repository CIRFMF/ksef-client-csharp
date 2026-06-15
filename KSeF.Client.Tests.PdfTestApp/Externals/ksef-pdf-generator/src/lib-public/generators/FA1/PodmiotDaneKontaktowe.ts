import { Content } from 'pdfmake/interfaces';
import { createLabelText } from '../../../shared/PDF-functions';
import { FP } from '../../types/fa1.types';

export function generateDaneKontaktowe(email?: FP, telefon?: FP[]): Content[] {
  const result: Content[] = [];

  if (email) {
    result.push(createLabelText('Email: ', email));
  }
  if (telefon) {
    telefon.forEach((item) => {
      result.push(createLabelText('Tel.: ', `${item}\n`));
    });
  }
  return result;
}
