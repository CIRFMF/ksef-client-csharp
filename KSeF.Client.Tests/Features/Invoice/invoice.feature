Potrzeba biznesowa: Wysłania faktury jako właściciel

@smoke @sendFA
Scenariusz: Posiadając uprawnienie właścicielskie wysyłamy fakturę
Zakładając, że jestem uwierzytelniony w sesji interaktywnej
Jeżeli wyślę plik zgodny ze schemą faktury
Wtedy zostanie on przyjęty przez API i stanie się fakturą z unikalnym numerem KSEF


@smoke @sendFAEncrypted @regresja
Scenariusz: Posiadając uprawnienie właścicielskie wysyłamy szyfrowaną fakturę z nieprawidłowym numerem NIP sprzedawcy
Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
Jeżeli wyślę szyfrowany plik zgodny ze schemą faktury, ale z niepoprawnym numerem NIP sprzedawcy
Wtedy zostanie on odrzucony z powodu braku autoryzacji

@smoke
Scenariusz: Posiadając uprawnienie właścicielskie pytamy o fakturę wysłaną
Zakładając, że jestem uwierzytelniony w sesji interaktywnej
Oraz wyślę plik zgodny ze schemą faktury
Jeżeli zostanie on przyjęty przez API i stanie się fakturą z unikalnym numerem KSEF
Wtedy pytam o fakturę o danym numerze KSEF

@validation @negative @regresja
Szablon scenariusza: Posiadając uprawnienie właścicielskie wysyłamy szyfrowaną fakturę z DataWytworzeniaFa wcześniejszą niż 2025-09-01
Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
Oraz przygotuję plik zgodny ze schemą faktury
Oraz ustawiam w polu "DataWytworzeniaFa" wartość "2025-08-31T00:00:00Z"
Jeżeli wyślę szyfrowany plik
Wtedy zostanie on odrzucony z kodem 445 i komunikatem zawierającym "Błąd weryfikacji semantyki dokumentu faktury"

@validation @negative @regresja
Szablon scenariusza: Posiadając uprawnienie właścicielskie wysyłamy szyfrowaną fakturę z P_1 ustawionym na przyszłą datę
Zakładając, że jestem uwierzytelniony w szyfrowanej sesji interaktywnej
Oraz przygotuję plik zgodny ze schemą faktury
Oraz ustawiam w polu "P_1" wartość równą „jutro” (UTC) w formacie "yyyy-MM-dd"
Jeżeli wyślę szyfrowany plik
Wtedy zostanie on odrzucony z kodem 445 i komunikatem zawierającym "Błąd weryfikacji semantyki dokumentu faktury"

@validation @negative @regresja @metadata
Szablon scenariusza: Zapytanie o metadane faktur z nieprawidłowym pageSize
Zakładając, że jestem uwierzytelniony w sesji interaktywnej
Oraz przygotuję zapytanie o metadane faktur.
Jeżeli wyślę zapytanie o metadane z pageOffset równym "0" i pageSize równym "<pageSize>"
Wtedy otrzymam błąd walidacji parametru "pageSize".

Przykłady:
  | pageSize |
  | 9        |
  | 101      |
  | 999      |

Scenariusz: Podmiot 3 może wyszukać fakturę, w której został wskazany jako ThirdSubject
Zakładając, że wykonawca jest uwierzytelniony w sesji interaktywnej
Oraz wystawia fakturę jednostce identyfikującej się numerem NIP jako Podmiot 3
Oraz faktura zostanie pomyślnie wysłana i przetworzona w KSeF
Jeżeli podmiot 3 uwierzytelni się w KSeF jako organizacja
Oraz wykona zapytanie o metadane faktur jako Podmiot 3 w zadanym zakresie dat
Wtedy odnajdzie fakturę, w której widnieje jako ThirdSubject

@validation @negative @regresja @attachment
Scenariusz: Posiadając uprawnienie właścicielskie wysyłamy fakturę z załącznikiem w sesji online
Zakładając, że jestem uwierzytelniony w sesji interaktywnej online
Oraz przygotuję plik FA3 zgodny ze schemą faktury zawierający załącznik
Jeżeli wyślę fakturę z załącznikiem
Wtedy zostanie ona odrzucona z kodem 415 i komunikatem zawierającym "Brak możliwości wysyłania faktury z załącznikiem"