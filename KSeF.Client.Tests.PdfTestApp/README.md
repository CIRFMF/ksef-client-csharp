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

### Krok 2: Zbuduj projekt

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
node generate-pdf-wrapper.mjs invoice .\Externals\ksef-pdf-generator\examples\invoice.xml faktura.pdf

# UPO z przykładu
node generate-pdf-wrapper.mjs upo .\Externals\ksef-pdf-generator\examples\upo.xml upo.pdf

# Faktura z własnego pliku
node generate-pdf-wrapper.mjs invoice C:\mojefaktury\faktura-2024-01.xml output.pdf

# Faktura z dodatkowymi danymi (numer KSeF, QR code)
node generate-pdf-wrapper.mjs invoice faktura.xml output.pdf '{\"nrKSeF\":\"123-456\",\"qrCode\":\"https://...\"}'
```

### Opcja 2: Aplikacja .NET (wrapper C#)

Wygodny wrapper dla projektów .NET, który wewnętrznie wywołuje Node.js wrapper.

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
dotnet run

# Faktura z własnego pliku
dotnet run -- C:\mojefaktury\faktura-2024-01.xml

# UPO
dotnet run -- upo .\Externals\ksef-pdf-generator\examples\upo.xml

# Faktura z jawnym określeniem typu
dotnet run -- faktura C:\ścieżka\do\faktury.xml

# Faktura z dodatkowymi danymi (numer KSeF, QR code)
# UWAGA: W PowerShell użyj pojedynczych cudzysłowów dla JSON!
dotnet run -- faktura C:\faktura.xml '{\"nrKSeF\":\"1234567890\",\"qrCode\":\"https://...\"}'
```

### Gdzie znajduje się wygenerowany PDF?

- **Node.js wrapper**: W lokalizacji podanej jako parametr `<outputPdf>`
- **Aplikacja .NET**: W katalogu projektu, nazwa z pliku XML (np. `invoice.xml` -> `invoice.pdf`)
