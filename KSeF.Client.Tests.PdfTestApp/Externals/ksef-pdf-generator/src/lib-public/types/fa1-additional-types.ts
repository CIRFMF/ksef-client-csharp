import { FP, Podmiot2K, Podmiot3 } from './fa1.types';
import { ContentTable } from 'pdfmake/interfaces';

export interface Podmiot3Podmiot2KDto {
  podmiot2KDto?: Podmiot2K;
  fakturaPodmiotNDto: Podmiot3;
}
export type FakturaZaliczkowa = {
  lp: {
    _text: number;
  };
  Klucz?: FP;
  Wartosc?: FP;
};

export type TableWithFields = {
  content: ContentTable | null;
  fieldsWithValue: string[];
};

export type TerminPlatnosciContent = {
  TerminPlatnosciOpis: {
    key: string;
  };
  TerminPlatnosci?: FP;
};
