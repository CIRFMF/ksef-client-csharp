# 📄 KSeF Demo API -- Dokumentacja

Aplikacja prezentuje integrację z **KSeF SDK** w środowisku .NET,
konfigurację klienta, obsługę podpisu oraz podstawową konfigurację REST
API. Projekt pokazuje, jak poprawnie wstrzykiwać serwisy KSeF,
konfigurować certyfikaty oraz uruchomić API w środowisku testowym KSeF.

## 📁 Konfiguracja (appsettings.json)

``` json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ApiSettings": {
    "BaseUrl": "https://ksef-test.mf.gov.pl",
    "customHeaders": {},
    "ResourcesPath": "Resources",
    "DefaultCulture": "pl-PL",
    "SupportedCultures": [
      "pl-PL",
      "en-US"
    ],
    "SupportedUICultures": [
      "pl-PL",
      "en-US"
    ]
  },
  "Tools": {
    "XMLDirectory": ""
  }
}
```


## 🔧 Konfiguracja aplikacji (`appsettings.json`)

### Sekcja **ApiSettings**

| Klucz                   | Typ                       | Wymagane | Opis                                                          |
| ----------------------- | ------------------------- | -------- | ------------------------------------------------------------- |
| **BaseUrl**             | string                    | ✔️       | Bazowy adres API KSeF (`https://ksef-test.mf.gov.pl`).    |
| **customHeaders**       | Dictionary<string,string> | ❌        | Dodatkowe nagłówki wysyłane w każdym żądaniu HTTP.            |
| **ResourcesPath**       | string                    | ❌        | Ścieżka katalogu z plikami `.resx`. Włącza `AddLocalization`. |
| **DefaultCulture**      | string                    | ❌        | Domyślne ustawienia regionalne (np. `pl-PL`).                     |
| **SupportedCultures**   | string[]                  | ❌        | Lista kultur obsługiwanych przez backend.                     |
| **SupportedUICultures** | string[]                  | ❌        | Lista kultur obsługiwanych w komunikatach UI.                 |
| **UseCamelCaseForRequests** | bool                  | ❌        | Domyślne ustawienie wielkości pierwszych liter w komunikacji z api                 |

Dokumentację na temat dostępnych kultur można znaleźć pod adresem : https://learn.microsoft.com/en-us/dotnet/api/system.globalization.culturetypes?view=net-10.0

### Sekcja **Tools**

| Klucz                 | Typ    | Opis                                                                            |
| --------------------- | ------ | ------------------------------------------------------------------------------- |
| **XMLDirectory**      | string | Katalog, w którym przechowywane są pliki XML.                                   |



## 🧩 Rejestracja usług w Program.cs

``` csharp
builder.Services.AddKSeFClient(options =>
{
    options.BaseUrl =
        builder.Configuration.GetSection("ApiSettings")
                .GetValue<string>("BaseUrl")
                ?? KsefEnvironmentsUris.TEST;

    options.CustomHeaders =
        builder.Configuration
                .GetSection("ApiSettings:customHeaders")
                .Get<Dictionary<string, string>>()
              ?? new Dictionary<string, string>();

    options.ResourcesPath = builder.Configuration.GetSection("ApiSettings")
                .GetValue<string>("ResourcesPath") ?? null;

    options.DefaultCulture = builder.Configuration.GetSection("ApiSettings")
            .GetValue<string>("DefaultCulture") ?? null;

    options.SupportedCultures = builder.Configuration.GetSection("ApiSettings").GetSection("SupportedCultures").Get<string[]>() ?? null;

    options.SupportedUICultures = builder.Configuration.GetSection("ApiSettings").GetSection("SupportedUICultures").Get<string[]>() ?? null;

    options.UseCamelCaseForRequests = builder.Configuration.GetSection("ApiSettings").GetSection("UseCamelCaseForRequests").Get<bool>() ?? null;
});
builder.Services.AddCryptographyClient();
```


## 🧱 Serwisy KSeF

Rejestrowane są m.in.: - IKSeFClient
- ITestDataClient
- IAuthCoordinator
- ILimitsClient
- IVerificationLinkService

## 🔧 Certyfikaty

Domyślnie używany jest: - DefaultCertificateFetcher
- CryptographyService
- HostedService (warmup)

## 🧪 Uruchamianie projektu

    dotnet restore
    dotnet run --framework net9.0


## 📦 Wymagania

-   .NET 8+



## 🔐 Endpoint: `POST /auth/auth-by-coordinator-with-pz`

Endpoint służy do rozpoczęcia procesu uwierzytelniania w środowisku testowym KSeF przy użyciu
koordynatora oraz podpisu Profilem Zaufanym.

------------------------------------------------------------------------

## 🧭 Instrukcja działania

Po wywołaniu tego endpointu aplikacja:

1.  Tworzy **plik XML** w katalogu określonym w konfiguracji:

        Tools:XMLDirectory

2.  Ten plik zawiera dane, które **muszą zostać podpisane** -- np.
    za pośrednictwem Profilu Zaufanego (PZ) lub innym dopuszczalnym 
    podpisem uwierzytelnionym.

3.  Po podpisaniu plik należy zapisać **w tym samym katalogu**, z
    **identyczną nazwą**, ale z dodaną końcówką:

        (1)

    **Przykład:**

        request.xml
        request (1).xml



## 🔐 Endpoint: `POST /auth/auth-step-by-step`
Endpoint służy do przeprowadzenia ręcznego, krokowego procesu uwierzytelniania w środowisku testowym KSeF, przy 
czym podpis jest wykonywany automatycznie w kodzie, a nie przez Profil Zaufany czy użytkownika.
Jest to wariant uproszczony, służący wyłącznie testowaniu i symulacji przepływu autoryzacyjnego.

------------------------------------------------------------------------

## 🧭 Instrukcja działania
Po wywołaniu tego endpointu aplikacja:

Uruchamia standardowy proces autoryzacji (podobny do auth-by-coordinator-with-PZ), lecz bez generowania pliku XML do podpisania.

Nie oczekuje podpisu od użytkownika — proces nie angażuje Profilu Zaufanego (PZ), ePUAP ani zewnętrznych usług podpisujących.

Zamiast tego podpis autoryzacyjny jest wykonywany automatycznie.

Wynik autoryzacji jest zwracany bez konieczności dodatkowych działań użytkownika.

