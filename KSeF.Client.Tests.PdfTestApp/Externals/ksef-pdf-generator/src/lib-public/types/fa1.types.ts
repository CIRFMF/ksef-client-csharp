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
  P_13_6?: FP;
  P_13_7?: FP;
  P_15?: FP;
  Adnotacje?: Adnotacje;
  RodzajFaktury?: FP;
  PrzyczynaKorekty?: FP;
  TypKorekty?: FP;
  DaneFaKorygowanej?: DaneFaKorygowanej[];
  OkresFaKorygowanej?: FP;
  NrFaKorygowany?: FP;
  Podmiot1K?: Podmiot1K;
  Podmiot2K?: Podmiot2K[];
  FP?: FP;
  TP?: FP;
  DodatkowyOpis?: DodatkowyOpi[];
  NrFaZaliczkowej?: FP[];
  ZwrotAkcyzy?: FP;
  FaWiersze?: FaWiersze;
  Rozliczenie?: Rozliczenie;
  Platnosc?: Platnosc;
  WarunkiTransakcji?: WarunkiTransakcji;
  Zamowienie?: Zamowienie;
}

export interface FP {
  _text?: string;
}

export interface Adnotacje extends Record<string, FP | undefined> {
  P_16?: FP;
  P_17?: FP;
  P_18?: FP;
  P_18A?: FP;
  P_19?: FP;
  P_19A?: FP;
  P_19B?: FP;
  P_19C?: FP;
  P_19N?: FP;
  P_23?: FP;
  P_PMarzy?: FP;
  P_PMarzy_2?: FP;
  P_PMarzy_3_1?: FP;
  P_PMarzy_3_2?: FP;
  P_PMarzy_3_3?: FP;
  P_PMarzyN?: FP;
}

export interface DaneFaKorygowanej {
  DataWystFaKorygowanej?: FP;
  NrFaKorygowanej?: FP;
  NrKSeFFaKorygowanej?: FP;
}

export interface DodatkowyOpi {
  Klucz?: FP;
  Wartosc?: FP;
}

export interface FaWiersze {
  LiczbaWierszyFaktury?: FP;
  WartoscWierszyFaktury1?: FP;
  WartoscWierszyFaktury2?: FP;
  FaWiersz?: Record<string, FP>[];
}

export interface Platnosc {
  Zaplacono?: FP;
  DataZaplaty?: FP;
  ZaplataCzesciowa?: FP;
  PlatnosciCzesciowe?: PlatnosciCzesciowe[];
  TerminyPlatnosci?: TerminyPlatnosci[];
  FormaPlatnosci?: FP;
  PlatnoscInna?: FP;
  OpisPlatnosci?: FP;
  RachunekBankowy?: Record<string, FP>[];
  RachunekBankowyFaktora?: Record<string, FP>[];
  Skonto?: Skonto;
}

export interface PlatnosciCzesciowe {
  KwotaZaplatyCzesciowej?: FP;
  DataZaplatyCzesciowej?: FP;
}

export interface Skonto {
  WarunkiSkonta?: FP;
  WysokoscSkonta?: FP;
}

export interface TerminyPlatnosci {
  TerminPlatnosci?: FP;
  TerminPlatnosciOpis?: FP;
}

export interface Podmiot1K {
  PrefiksPodatnika?: FP;
  DaneIdentyfikacyjne?: DaneIdentyfikacyjne;
  Adres?: Adres;
}

export interface Adres {
  AdresPol?: Record<string, FP>;
  AdresZagr?: Record<string, FP>;
}

export interface DaneIdentyfikacyjne {
  NIP?: FP;
  ImiePierwsze?: FP;
  Nazwisko?: FP;
  PelnaNazwa?: FP;
  NazwaHandlowa?: FP;
}

export interface Podmiot2K {
  PrefiksNabywcy?: FP;
  DaneIdentyfikacyjne?: Record<string, FP>;
  Adres?: Adres;
}

export interface Rozliczenie {
  Obciazenia?: Zenia[];
  SumaObciazen?: FP;
  Odliczenia?: Zenia[];
  SumaOdliczen?: FP;
  DoZaplaty?: FP;
  DoRozliczenia?: FP;
}

export interface Zenia {
  Kwota?: FP;
  Powod?: FP;
}

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

export interface Umowy {
  DataUmowy?: FP;
  NrUmowy?: FP;
}

export interface Zamowienia {
  DataZamowienia?: FP;
  NrZamowienia?: FP;
}

export interface Zamowienie {
  LiczbaWierszyZamowienia?: FP;
  WartoscZamowienia?: FP;
  ZamowienieWiersz?: Record<string, FP>[];
}

export interface Naglowek {
  KodFormularza?: KodFormularza;
  WariantFormularza?: FP;
  DataWytworzeniaFa?: FP;
  SystemInfo?: FP;
}

export interface KodFormularza {
  _attributes?: KodFormularzaAttributes;
  _text?: string;
}

export interface KodFormularzaAttributes {
  kodSystemowy?: string;
  wersjaSchemy?: string;
}

export interface Podmiot1 {
  PrefiksPodatnika?: FP;
  NrEORI?: FP;
  DaneIdentyfikacyjne?: DaneIdentyfikacyjne;
  Adres?: Adres;
  AdresKoresp?: Adres;
  Email?: FP;
  Telefon?: FP[];
  StatusInfoPodatnika?: FP;
}

export interface Podmiot2 {
  PrefiksNabywcy?: FP;
  NrEORI?: FP;
  DaneIdentyfikacyjne?: Record<string, FP>;
  Adres?: Adres;
  AdresKoresp?: Adres;
  Email?: FP;
  Telefon?: FP[];
  NrKlienta?: FP;
}

export interface Podmiot3 {
  NrEORI?: FP;
  DaneIdentyfikacyjne?: Record<string, FP>;
  Adres?: Adres;
  AdresKoresp?: Adres;
  Email?: FP;
  Telefon?: FP[];
  Rola?: FP;
  RolaInna?: FP;
  OpisRoli?: FP;
  Udzial?: FP;
  NrKlienta?: FP;
}

export interface PodmiotUpowazniony {
  NrEORI?: FP;
  DaneIdentyfikacyjne?: Record<string, FP>;
  Adres?: Adres;
  AdresKoresp?: Adres;
  EmailPU?: FP;
  TelefonPU?: FP[];
  RolaPU?: FP;
}

export interface Stopka {
  Informacje?: Informacje[];
  Rejestry?: Rejestry[];
}

export interface Informacje {
  StopkaFaktury?: FP;
}

export interface Rejestry {
  PelnaNazwa?: FP;
  KRS?: FP;
  REGON?: FP;
  BDO?: FP;
}

export interface FakturaAttributes {
  tns?: string;
  xsi?: string;
  schemaLocation?: string;
}
