## Paczka Nuget
23.07.2025

Paczki nuget dostępne są pod adresem: https://github.com/orgs/CIRFMF/packages

Opis paczek:
* KSeF.Client - główna biblioteka klienta z logiką biznesową
* KSeF.Client.Core - modele, interfejsy i wyjątki 

### Wymagania wstępne

Aby korzystać z biblioteki KSeF.Client w swoim projekcie, należy najpierw skonfigurować dostęp do paczek NuGet hostowanych w GitHub Packages organizacji CIRFMF.
Wymaga to autoryzacji przy pomocy osobistego tokena dostępu (Personal Access Token – PAT) z uprawnieniem read:packages.

### 1. Wygenerowanie tokena dostępu (PAT)

Aby uzyskać dostęp do paczek z GitHub Packages, należy utworzyć Personal Access Token (PAT) z odpowiednimi uprawnieniami:

Ścieżka: GitHub -> Settings -> Developer settings -> Personal access tokens -> Tokens (classic) -> Generate new token -> Generate new token (classic)

W sekcji **Select scopes** należy wybrać **read:packages** a następnie wygenerować i skopiować wartość tokena (będzie widoczna tylko raz)

### 2. Dodanie źródła pakietów

Aby móc instalować paczki z GitHub Packages, należy dodać źródło przez **.NET CLI**.

```dotnet nuget add source "https://nuget.pkg.github.com/CIRFMF/index.json" --name github-cirf --username token --password <TOKEN_PAT> --store-password-in-clear-text```

Jeśli źródło zostanie dodane bez uwierzytelnienia, narzędzia (dotnet/Visual Studio) zwrócą błąd 401 Unauthorized lub 403 Forbidden.
Dlatego należy podać PAT już podczas dodawania źródła.

### 3. Pobranie paczek

Po dodaniu źródła możesz zainstalować biblioteki KSeF.Client i KSeF.Client.Core w swoim projekcie. 
Paczka KSeF.Client automatycznie dodaje zależność KSeF.Client.Core, więc w większości przypadków wystarczy zainstalować tylko KSeF.Client.
Instalację paczki można przeprowadzić na dwa sposoby:
* *Visual Studio*
	Ścieżka: Tools -> NuGet Package Manager -> Manage NuGet Packages for Solution
	Wybierz źródło pakietów github-cirf (w prawym górnym rogu okna) oraz zaznacz opcję Include prerelease, aby widzieć wersje RC.

* *.NET CLI*
	dotnet add package KSeF.Client --prerelease
	dotnet add package KSeF.Client.Core	--prerelease