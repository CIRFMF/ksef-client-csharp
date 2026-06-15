import { Content, Margins } from 'pdfmake/interfaces';
import { createHeader, createSubHeader } from '../../../shared/PDF-functions';
import { Adres } from '../../types/fa1.types';
import { generateAdres } from './Adres';

export function generatePodmiotAdres(
  podmiotAdres: Adres | undefined,
  headerTitle = 'Adres',
  isSubheader = false,
  headerMargin?: Margins
): Content[] {
  if (!podmiotAdres) {
    return [];
  }
  return [
    ...(isSubheader ? createSubHeader(headerTitle, headerMargin) : createHeader(headerTitle, headerMargin)),
    ...generateAdres(podmiotAdres),
  ];
}
