# **KSeF Client**


## Wstęp – Struktura projektu i technologie

Repozytorium zawiera:

- **Implementacja klienta KSeF 2.0 (KSeF.Client)**
- **Testy integracyjne klienta KSeF 2.0 (KSeF.Client.Tests)**
- **Przykładową aplikację napisaną w ASP.NET (KSeF.DemoWebApp)**

Całość napisana jest w języku **C#** z wykorzystaniem platformy **.NET 8** (lub nowszej). Do komunikacji HTTP wykorzystywany jest RestClient, a rejestracja i konfiguracja klienta KSeF odbywa się przez mechanizm Dependency Injection (Microsoft.Extensions.DependencyInjection).

### Struktura katalogów KSeF.Client


- **Api**  
  Zawiera buildery do tworzenia requestów do API oraz serwisy ułatwiające realizację procesów takich jak uwierzytelnianie czy wysyłka wsadowa.

- **Core**  
  Zawiera modele klas odpowiadających zwrotkom z API, definicje wyjątków, interfejsy oraz modele wykorzystywane przez serwisy.

- **Http**  
  Zawiera implementację klienta KSeF, który realizuje żądania HTTP do API za pomocą RestClienta.

- **DI**  
  Odpowiada za konfigurację i rejestrację klienta KSeF w kontenerze Dependency Injection.  
  **To ten projekt należy dodać do swoich zależności** – posiada on referencje do pozostałych projektów i stanowi główny punkt wejścia do integracji.


### Przykładowa rejestracja klienta KSeF w kontenerze DI 

```csharp
using KSeFClient.DI;

var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddKSeFClient(options =>
{
    options.BaseUrl = KsefEnviromentsUris.DEV;
});
```

Więcej informacji oraz przykłady użycie klienta KSeF 2.0 można znaleźć [tutaj](https://github.com/CIRFMF/ksef-docs).