import { FP as FP3 } from './fa3.types';
import { FP as FP2 } from './fa2.types';

interface FA2FakturaZaliczkowaDataSpozaKSeF {
  NrKSeFZN: FP2;
  NrFaZaliczkowej: FP2;
}

interface FA2FakturaZaliczkowaDataZKSeF {
  NrKSeFFaZaliczkowej: FP2;
}

interface FA3FakturaZaliczkowaDataSpozaKSeF {
  NrKSeFZN: FP3;
  NrFaZaliczkowej: FP3;
}

interface FA3FakturaZaliczkowaDataZKSeF {
  NrKSeFFaZaliczkowej: FP3;
}

export type FA3FakturaZaliczkowaData = FA3FakturaZaliczkowaDataSpozaKSeF | FA3FakturaZaliczkowaDataZKSeF;
export type FA2FakturaZaliczkowaData = FA2FakturaZaliczkowaDataSpozaKSeF | FA2FakturaZaliczkowaDataZKSeF;

export interface AdditionalDataTypes {
  nrKSeF: string;
  qrCode?: string;
  isMobile?: boolean;
}
