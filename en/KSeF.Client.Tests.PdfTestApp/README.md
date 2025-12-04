# PDF generator for invoices and UPO KSeF

A tool for automatically generating PDF visualizations for electronic invoices and UPO (Official Receipt Certificates) from the KSeF system.

Available in two variants:

- **Node.js wrapper** - minimalist script for direct use
- **.NET Application** - C# wrapper for easier integration with .NET projects

## Requirements

- **Node.js** version **22.14.0** or later

    - Download from: [https://nodejs.org](https://nodejs.org)
    - Check version: `node --version`

- **.NET SDK** version **9.0** or later *(optional, for .NET applications only)*

    - Download from: [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
    - Check version: `dotnet --version`

## Installation

### Step 1: Clone the repository with submodules

```bash
git clone --recurse-submodules https://github.com/CIRFMF/ksef-client-csharp.git
cd ksef-client-csharp/KSeF.Client.Tests.PdfTestApp
```

**If you have already cloned the repository without submodules** , do:

```bash
git submodule update --init --recursive
```

### Step 2: Build the project

```bash
dotnet build
```

## Use

There are two ways to generate PDF:

### Option 1: Node.js wrapper (direct use)

A minimal Node.js script with no additional dependencies (except jsdom in the generator's node_modules).

#### Syntax

```bash
node generate-pdf-wrapper.mjs  <invoice|faktura|upo> <inputXml> <outputPdf> [additionalDataJson]
```

#### Examples

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

### Option 2: .NET Application (C# Wrapper)

A convenient wrapper for .NET projects that internally calls a Node.js wrapper.

#### Syntax

```bash
dotnet run                                                 # Domyślna faktura
dotnet run -- <ścieżkaXml>                                 # Własna faktura
dotnet run -- <typ> <ścieżkaXml>                           # Z określeniem typu (faktura/invoice/upo)
dotnet run -- <typ> <ścieżkaXml> <additionalDataJson>     # Z dodatkowymi danymi KSeF
```

#### Examples

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

### Where is the generated PDF located?

- **Node.js wrapper** : In the location given as `<outputPdf>` parameter
- **.NET Application** : In the project directory, name from the XML file (e.g. `invoice.xml` -&gt; `invoice.pdf` )
