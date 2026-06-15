import {
  DaneFaKorygowanej,
  FakturaAttributes as FakturaAttributesFa1,
  KodFormularza as KodFormularzaFa1,
  Stopka as StopkaFa1,
  Umowy as UmowyFa1,
  Zamowienia as ZamowieniaFa1,
  Zenia as ZeniaFa1,
} from './fa1.types';

export interface Faktura {
  _attributes?: FakturaAttributes;
  Naglowek?: Naglowek;
  Podmiot1?: Podmiot1;
  Podmiot2?: Podmiot2;
  Podmiot3?: Podmiot3[];
  PodmiotUpowazniony?: PodmiotUpowazniony;
  Fa?: Fa;
  Stopka?: Stopka;
}

export interface Fa {
  KodWaluty?: FP;
  P_1?: FP;
  P_1M?: FP;
  P_2?: FP;
  WZ?: FP[];
  P_6?: FP;
  OkresFa?: Record<string, FP>;
  P_13_1?: FP;
  P_14_1?: FP;
  P_14_1W?: FP;
  P_13_2?: FP;
  P_14_2?: FP;
  P_14_2W?: FP;
  P_13_3?: FP;
  P_14_3?: FP;
  P_14_3W?: FP;
  P_13_4?: FP;
  P_14_4?: FP;
  P_14_4W?: FP;
  P_13_5?: FP;
  P_14_5?: FP;
  P_13_6_1?: FP;
  P_13_6_2?: FP;
  P_13_6_3?: FP;
  P_13_7?: FP;
  P_13_8?: FP;
  P_13_9?: FP;
  P_13_10?: FP;
  P_13_11?: FP;
  P_15?: FP;
  KursWalutyZ?: FP;
  Adnotacje?: Adnotacje;
  RodzajFaktury?: FP;
  PrzyczynaKorekty?: FP;
  TypKorekty?: FP;
  DaneFaKorygowanej?: DaneFaKorygowanej;
  OkresFaKorygowanej?: FP;
  NrFaKorygowany?: FP;
  Podmiot1K?: Podmiot1K;
  Podmiot2K?: Podmiot2K[];
  P_15ZK?: FP;
  KursWalutyZK?: FP;
  ZaliczkaCzesciowa?: ZaliczkaCzesciowa[];
  FP?: FP;
  TP?: FP;
  DodatkowyOpis?: DodatkowyOpi[];
  FakturaZaliczkowa?: Record<string, FP>[];
  ZwrotAkcyzy?: FP;
  FaWiersz?: Record<string, FP>[];
  Rozliczenie?: Rozliczenie;
  Platnosc?: Platnosc;
  WarunkiTransakcji?: WarunkiTransakcji;
  Zamowienie?: Zamowienie;
}

export interface Adnotacje {
  P_16?: FP;
  P_17?: FP;
  P_18?: FP;
  P_18A?: FP;
  Zwolnienie?: Zwolnienie;
  NoweSrodkiTransportu?: NoweSrodkiTransportu;
  P_23?: FP;
  PMarzy?: PMarzy;
}

export interface NoweSrodkiTransportu {
  P_22?: FP;
  P_42_5?: FP;
  NowySrodekTransportu?: Record<string, FP>[];
  P_22N?: FP;
}

export interface FP {
  _text?: string;
}

export interface PMarzy {
  P_PMarzy?: FP;
  P_PMarzy_2?: FP;
  P_PMarzy_3_1?: FP;
  P_PMarzy_3_2?: FP;
  P_PMarzy_3_3?: FP;
  P_PMarzyN?: FP;
}

export interface Zwolnienie {
  P_19?: FP;
  P_19A?: FP;
  P_19B?: FP;
  P_19C?: FP;
  P_19N?: FP;
}

export interface DodatkowyOpi {
  NrWiersza?: FP;
  Klucz?: FP;
  Wartosc?: FP;
}

export interface Platnosc {
  Zaplacono?: FP;
  DataZaplaty?: FP;
  ZnacznikZaplatyCzesciowej?: FP;
  ZaplataCzesciowa?: ZaplataCzesciowa[];
  TerminPlatnosci?: TerminPlatnosci[];
  FormaPlatnosci?: FP;
  PlatnoscInna?: FP;
  OpisPlatnosci?: FP;
  RachunekBankowy?: RachunekBankowy[];
  RachunekBankowyFaktora?: RachunekBankowy[];
  Skonto?: Skonto;
}

export interface RachunekBankowy {
  NrRB?: FP;
  SWIFT?: FP;
  RachunekWlasnyBanku?: FP;
  NazwaBanku?: FP;
  OpisRachunku?: FP;
}

export interface Skonto {
  WarunkiSkonta?: FP;
  WysokoscSkonta?: FP;
}

export interface TerminPlatnosci {
  Termin?: FP;
  TerminOpis?: FP;
}

export interface ZaplataCzesciowa {
  KwotaZaplatyCzesciowej?: FP;
  DataZaplatyCzesciowej?: FP;
}

export interface Podmiot1K {
  PrefiksPodatnika?: FP;
  DaneIdentyfikacyjne?: DaneIdentyfikacyjne;
  Adres?: Adres;
}

export interface Adres {
  KodKraju?: FP;
  AdresL1?: FP;
  AdresL2?: FP;
  GLN?: FP;
}

export interface DaneIdentyfikacyjne {
  NIP?: FP;
  Nazwa?: FP;
}

export interface Podmiot2K {
  DaneIdentyfikacyjne?: Record<string, FP>;
  Adres?: Adres;
  IDNabywcy?: FP;
}

export interface Rozliczenie {
  Obciazenia?: Zenia[];
  SumaObciazen?: FP;
  Odliczenia?: Zenia[];
  SumaOdliczen?: FP;
  DoZaplaty?: FP;
  DoRozliczenia?: FP;
}

export interface Zenia extends ZeniaFa1 {}

export interface WarunkiTransakcji {
  Umowy?: Umowy[];
  Zamowienia?: Zamowienia[];
  NrPartiiTowaru?: FP[];
  WarunkiDostawy?: FP;
  KursUmowny?: FP;
  WalutaUmowna?: FP;
  Transport?: Transport[];
  PodmiotPosredniczacy?: FP;
}

export interface Transport {
  RodzajTransportu?: FP;
  TransportInny?: FP;
  OpisInnegoTransportu?: FP;
  Przewoznik?: Przewoznik;
  NrZleceniaTransportu?: FP;
  OpisLadunku?: FP;
  LadunekInny?: FP;
  OpisInnegoLadunku?: FP;
  JednostkaOpakowania?: FP;
  DataGodzRozpTransportu?: FP;
  DataGodzZakTransportu?: FP;
  WysylkaZ?: Adres;
  WysylkaPrzez?: Adres[];
  WysylkaDo?: Adres;
}

export interface Przewoznik {
  DaneIdentyfikacyjne?: Record<string, FP>;
  AdresPrzewoznika?: Adres;
}

export interface Umowy extends UmowyFa1 {}

export interface Zamowienia extends ZamowieniaFa1 {}

export interface ZaliczkaCzesciowa {
  P_6Z?: FP;
  P_15Z?: FP;
  KursWalutyZW?: FP;
}

export interface Zamowienie {
  WartoscZamowienia?: FP;
  ZamowienieWiersz?: Record<string, FP>[];
}

export interface Naglowek {
  KodFormularza?: KodFormularza;
  WariantFormularza?: FP;
  DataWytworzeniaFa?: FP;
  SystemInfo?: FP;
}

export interface KodFormularza extends KodFormularzaFa1 {}

export interface Podmiot1 {
  PrefiksPodatnika?: FP;
  NrEORI?: FP;
  DaneIdentyfikacyjne?: DaneIdentyfikacyjne;
  Adres?: Adres;
  AdresKoresp?: Adres;
  DaneKontaktowe?: Podmiot1DaneKontaktowe[];
  StatusInfoPodatnika?: FP;
}

export interface Podmiot1DaneKontaktowe {
  Email?: FP;
  Telefon?: FP;
}

export interface Podmiot2 {
  NrEORI?: FP;
  DaneIdentyfikacyjne?: Record<string, FP>;
  Adres?: Adres;
  AdresKoresp?: Adres;
  DaneKontaktowe?: Podmiot1DaneKontaktowe[];
  NrKlienta?: FP;
  IDNabywcy?: FP;
}

export interface Podmiot3 {
  IDNabywcy?: FP;
  NrEORI?: FP;
  DaneIdentyfikacyjne?: Record<string, FP>;
  Adres?: Adres;
  AdresKoresp?: Adres;
  DaneKontaktowe?: Podmiot1DaneKontaktowe[];
  Rola?: FP;
  RolaInna?: FP;
  OpisRoli?: FP;
  Udzial?: FP;
  NrKlienta?: FP;
}

export interface PodmiotUpowazniony {
  NrEORI?: FP;
  DaneIdentyfikacyjne?: DaneIdentyfikacyjne;
  Adres?: Adres;
  AdresKoresp?: Adres;
  DaneKontaktowe?: PodmiotUpowaznionyDaneKontaktowe[];
  RolaPU?: FP;
}

export interface PodmiotUpowaznionyDaneKontaktowe {
  EmailPU?: FP;
  TelefonPU?: FP;
}

export interface Stopka extends StopkaFa1 {}

export interface FakturaAttributes extends FakturaAttributesFa1 {}
