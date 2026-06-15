import {
  FA1RolaPodmiotu3,
  FA2RolaPodmiotu3,
  FA3RolaPodmiotu3,
  FormaPlatnosci,
  RodzajTransportu,
  TRolaPodmiotuUpowaznionegoFA1,
  TRolaPodmiotuUpowaznionegoFA2,
  TRolaPodmiotuUpowaznionegoFA3,
  TypLadunku,
  TypRachunkowWlasnych,
} from '../../consts/const';
import { FP as FP2 } from '../../../lib-public/types/fa2.types';

export function getRolaString(rola: FP2 | undefined, FA: 1 | 2 | 3): string {
  if (!rola || !rola._text) {
    return '';
  }
  switch (FA) {
    case 1:
      return FA1RolaPodmiotu3[rola._text as keyof typeof FA1RolaPodmiotu3].split('-')[0] ?? '';
    case 2:
      return FA2RolaPodmiotu3[rola._text as keyof typeof FA2RolaPodmiotu3].split('-')[0] ?? '';
    case 3:
      return FA3RolaPodmiotu3[rola._text as keyof typeof FA3RolaPodmiotu3].split('-')[0] ?? '';
  }
}

export function getRolaUpowaznionegoString(rola: FP2 | undefined, FA: 1 | 2 | 3): string {
  if (!rola || !rola._text) {
    return '';
  }
  switch (FA) {
    case 1:
      return TRolaPodmiotuUpowaznionegoFA1[rola._text].split('-')[0] ?? '';
    case 2:
      return TRolaPodmiotuUpowaznionegoFA2[rola._text].split('-')[0] ?? '';
    case 3:
      return TRolaPodmiotuUpowaznionegoFA3[rola._text].split('-')[0] ?? '';
  }
}

export function getFormaPlatnosciString(formaPlatnosci: FP2 | undefined): string {
  if (!formaPlatnosci || !formaPlatnosci._text) {
    return '';
  }
  return FormaPlatnosci[formaPlatnosci._text as keyof typeof FormaPlatnosci] ?? '';
}

export function getRodzajTransportuString(rodzajTransportu: FP2 | undefined): string {
  if (!rodzajTransportu || !rodzajTransportu._text) {
    return '';
  }
  return RodzajTransportu[rodzajTransportu._text as keyof typeof RodzajTransportu] ?? '';
}

export function getOpisTransportuString(opisTransportu: FP2 | undefined): string {
  if (!opisTransportu || !opisTransportu._text) {
    return '';
  }
  return TypLadunku[opisTransportu._text as keyof typeof TypLadunku] ?? '';
}

export function getTypRachunkowWlasnych(typRachonkowWlasnych: FP2 | undefined): string {
  if (!typRachonkowWlasnych || !typRachonkowWlasnych._text) {
    return '';
  }
  return TypRachunkowWlasnych[typRachonkowWlasnych._text as keyof typeof TypRachunkowWlasnych] ?? '';
}

export function formatDateTime(data?: string, withoutSeconds?: boolean, withoutTime?: boolean): string {
  if (!data) {
    return '';
  }
  const dateTime: Date = new Date(data);

  if (isNaN(dateTime.getTime())) {
    return data;
  }

  const year: number = dateTime.getFullYear();
  const month: string = (dateTime.getMonth() + 1).toString().padStart(2, '0');
  const day: string = dateTime.getDate().toString().padStart(2, '0');
  const hours: string = dateTime.getHours().toString().padStart(2, '0');
  const minutes: string = dateTime.getMinutes().toString().padStart(2, '0');
  const seconds: string = dateTime.getSeconds().toString().padStart(2, '0');

  if (withoutTime) {
    return `${year}-${month}-${day}`;
  } else if (withoutSeconds) {
    return `${year}-${month}-${day} ${hours}:${minutes}`;
  }
  return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}

export function getDateTimeWithoutSeconds(isoDate?: FP2): string {
  if (!isoDate?._text) {
    return '';
  }
  return formatDateTime(isoDate._text, true);
}
