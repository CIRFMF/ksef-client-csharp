import { Podmiot3 } from './fa2.types';
import { FP, Podmiot2K } from './fa1.types';

export interface Podmiot3Podmiot2KDto {
  podmiot2KDto?: Podmiot2K;
  fakturaPodmiotNDto: Podmiot3;
}

export interface DaneIdentyfikacyjneTPodmiot2Dto {
  NIP?: FP;
  KodUE?: FP;
  KodKraju?: FP;
  BrakID?: FP;
  PelnaNazwa?: FP;
  NazwaHandlowa?: FP;
  ImiePierwsze?: FP;
  Nazwisko?: FP;
  NrID?: FP;
  NrVatUE?: FP;
}

export interface DaneIdentyfikacyjneTPodmiot2Dto {
  NIP?: FP;
  KodUE?: FP;
  KodKraju?: FP;
  BrakID?: FP;
  Nazwa?: FP;
  NrID?: FP;
  NrVatUE?: FP;
}
