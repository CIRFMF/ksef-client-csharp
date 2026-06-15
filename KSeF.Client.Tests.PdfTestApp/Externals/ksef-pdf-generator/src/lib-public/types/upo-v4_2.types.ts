export interface Upo {
  _declaration?: Declaration;
  _comment?: string;
  Potwierdzenie?: Potwierdzenie;
}

export interface Potwierdzenie {
  _attributes?: PotwierdzenieAttributes;
  NazwaPodmiotuPrzyjmujacego?: KodFormularza;
  NumerReferencyjnySesji?: KodFormularza;
  Uwierzytelnienie?: Uwierzytelnienie;
  OpisPotwierdzenia?: OpisPotwierdzenia;
  NazwaStrukturyLogicznej?: KodFormularza;
  KodFormularza?: KodFormularza;
  Dokument?: Dokument[];
}

export interface Dokument {
  lp?: number;
  NipSprzedawcy?: KodFormularza;
  NumerKSeFDokumentu?: KodFormularza;
  NumerFaktury?: KodFormularza;
  DataWystawieniaFaktury?: KodFormularza;
  DataPrzeslaniaDokumentu?: KodFormularza;
  DataNadaniaNumeruKSeF?: KodFormularza;
  SkrotDokumentu?: KodFormularza;
}

export interface KodFormularza {
  _text?: string;
}

export interface OpisPotwierdzenia {
  Strona?: KodFormularza;
  LiczbaStron?: KodFormularza;
  ZakresDokumentowOd?: KodFormularza;
  ZakresDokumentowDo?: KodFormularza;
  CalkowitaLiczbaDokumentow?: KodFormularza;
}

export interface Uwierzytelnienie {
  IdKontekstu?: IDKontekstu;
  NumerReferencyjnyTokenaKSeF?: KodFormularza;
  SkrotDokumentuUwierzytelniajacego?: KodFormularza;
}

export interface IDKontekstu {
  Nip?: KodFormularza;
  IdWewnetrzny?: KodFormularza;
  IdZlozonyVatUE?: KodFormularza;
  IdDostawcyUslugPeppol?: KodFormularza;
}

export interface PotwierdzenieAttributes {
  upo?: string;
  xsi?: string;
  wersjaSchemy?: string;
  schemaLocation?: string;
}

export interface Declaration {
  _attributes?: DeclarationAttributes;
}

export interface DeclarationAttributes {
  version?: string;
  encoding?: string;
}
