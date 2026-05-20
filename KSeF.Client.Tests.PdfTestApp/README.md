# Generator PDF dla faktur i UPO KSeF

Narzędzie do automatycznego generowania wizualizacji PDF dla faktur elektronicznych i UPO (Urzędowych Poświadczeń Odbioru) z systemu KSeF.

Dostępne w dwóch wariantach:
- **Node.js wrapper** - minimalistyczny skrypt do bezpośredniego użycia
- **Aplikacja .NET** - wrapper C# dla łatwiejszej integracji z projektami .NET

## Wymagania

- **Node.js** w wersji **22.14.0** lub nowszej
  - Pobierz z: [https://nodejs.org](https://nodejs.org)
  - Sprawdź wersję: `node --version`

- **.NET SDK** w wersji **9.0** lub nowszej *(opcjonalne, tylko dla aplikacji .NET)*
  - Pobierz z: [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
  - Sprawdź wersję: `dotnet --version`

## Instalacja

### Krok 1: Sklonuj repozytorium z submodułami

```bash
git clone --recurse-submodules https://github.com/CIRFMF/ksef-client-csharp.git
cd ksef-client-csharp/KSeF.Client.Tests.PdfTestApp
```

**Jeśli już sklonowałeś repozytorium bez submodułów**, wykonaj:

```bash
git submodule update --init --recursive
```

### Krok 2: Przygotuj submoduł PDF generatora

Projekt korzysta z submodułu `ksef-pdf-generator`, który jest aplikacją Node.js
i musi zostać zbudowany przed uruchomieniem projektu .NET.

#### Opcja 1 — ręcznie (wymagane przy pierwszym uruchomieniu)
```bash
cd Externals/ksef-pdf-generator
npm install
npm run build
```

#### Opcja 2 — automatycznie (przy uruchomieniu aplikacji)

Podczas uruchamiania aplikacji testowej:

```bash
dotnet run --framework net10.0  # lub net8.0, net9.0
```

projekt automatycznie wykona instalację i build submodułu

### Krok 3: Zbuduj projekt

```bash
dotnet build
```

## Użycie

Dostępne są dwa sposoby generowania PDF:

### Opcja 1: Node.js wrapper (bezpośrednie użycie)

Minimalny skrypt Node.js bez dodatkowych zależności (poza jsdom w node_modules generatora).

#### Składnia

```bash
node generate-pdf-wrapper.mjs  <invoice|faktura|upo> <inputXml> <outputPdf> [additionalDataJson]
```

#### Przykłady

```bash
# Faktura z domyślnego przykładu
node generate-pdf-wrapper.mjs invoice .\Externals\ksef-pdf-generator\assets\invoice.xml faktura.pdf

# UPO z przykładu
node generate-pdf-wrapper.mjs upo .\Externals\ksef-pdf-generator\assets\upo.xml upo.pdf

# Faktura z własnego pliku
node generate-pdf-wrapper.mjs invoice C:\mojefaktury\faktura-2024-01.xml output.pdf

# Wszystkie faktury/UPO z folderu (w zależności od wybranej opcji)
node generate-pdf-wrapper.mjs <invoice|upo> C:\mojefaktury

# Faktura z dodatkowymi danymi (numer KSeF, QR code)
node generate-pdf-wrapper.mjs invoice faktura.xml output.pdf '{\"nrKSeF\":\"123-456\",\"qrCode\":\"https://...\"}'
```

### Opcja 2: Aplikacja .NET (wrapper C#)

Wygodny wrapper dla projektów .NET, który wewnętrznie wywołuje Node.js wrapper.

> **Uwaga:** Projekt wspiera .NET 8.0, 9.0 i 10.0. 
Jeśli potrzebujesz wybrać konkretną wersję, dodaj `--framework net10.0` (lub `net8.0`, `net9.0`) po `dotnet run`.

#### Składnia

```bash
dotnet run                                                 # Domyślna faktura
dotnet run -- <ścieżkaXml>                                 # Własna faktura
dotnet run -- <typ> <ścieżkaXml>                           # Z określeniem typu (faktura/invoice/upo)
dotnet run -- <typ> <ścieżkaXml> <additionalDataJson>     # Z dodatkowymi danymi KSeF
```

#### Przykłady

```bash
# Domyślna faktura przykładowa
dotnet run --framework net10.0

# Faktura z własnego pliku
dotnet run --framework net10.0 -- C:\mojefaktury\faktura-2024-01.xml

# UPO
dotnet run --framework net10.0 -- upo .\Externals\ksef-pdf-generator\assets\upo.xml

# Faktura z jawnym określeniem typu
dotnet run --framework net10.0 -- faktura C:\ścieżka\do\faktury.xml

# Wszystkie faktury/UPO z folderu z jawnym typem (w zależności od wybranej opcji)
dotnet run --framework net10.0 -- <faktura|upo> C:\mojefaktury

# Faktura z dodatkowymi danymi (numer KSeF, QR code)
# UWAGA: W PowerShell użyj pojedynczych cudzysłowów dla JSON!
dotnet run --framework net10.0 -- faktura C:\faktura.xml '{\"nrKSeF\":\"1234567890\",\"qrCode\":\"https://...\"}'
```

### Gdzie znajduje się wygenerowany PDF?

- **Node.js wrapper**: W lokalizacji podanej jako parametr `<outputPdf>`
- **Aplikacja .NET**: W katalogu projektu, nazwa z pliku XML (np. `invoice.xml` -> `invoice.pdf`)

## Aktualizacja

### Aktualizacja submodułu ksef-pdf-generator

Aby zaktualizować submoduł `ksef-pdf-generator` do najnowszej wersji:

1. Zaktualizuj submoduł do najnowszej wersji z remote:
   ```bash
   git submodule update --remote
   ```

2. Wyczyść katalogi generowane przez build:
   ```bash
   cd Externals/ksef-pdf-generator
   rm -r node_modules, dist
   rm package-lock.json
   ```

3. Przy pierwszym uruchomieniu aplikacja automatycznie zainstaluje zależności i zbuduje generator:
   ```bash
   cd ../..
   dotnet run --framework net10.0
   ```

## Rozwiązywanie problemów

### Problem z zależnościami Node.js

Jeśli napotkasz błędy związane z brakującymi modułami lub błędami podczas generowania PDF:
1. Przejdź do katalogu `Externals/ksef-pdf-generator`
2. Usuń katalogi `node_modules`, `dist` i plik `package-lock.json`
3. Wykonaj ponownie `npm install` i `npm run build`