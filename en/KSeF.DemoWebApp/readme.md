# 📄 KSeF Demo API -- Documentation

The application demonstrates integration with **the KSeF SDK** in a .NET environment, client configuration, signing support, and basic REST API configuration. The project demonstrates how to correctly inject KSeF services, configure certificates, and run the API in a KSeF test environment.

## 📁 Configuration (appsettings.json)

```json
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

## 🔧 App configuration ( `appsettings.json` )

### **ApiSettings** section

Key | Type | Required | Description
--- | --- | --- | ---
**BaseUrl** | string | ✔️ | KSeF API base address ( `https://ksef-test.mf.gov.pl` ).
**customHeaders** | Dictionary&lt;string,string&gt; | ❌ | Additional headers sent with every HTTP request.
**ResourcesPath** | string | ❌ | Path to the directory containing `.resx` files. Enables `AddLocalization` .
**DefaultCulture** | string | ❌ | Default locale (e.g. `pl-PL` ).
**SupportedCultures** | string[] | ❌ | List of cultures supported by the backend.
**SupportedUICultures** | string[] | ❌ | List of cultures supported in UI messages.

Documentation on available cultures can be found at: https://learn.microsoft.com/en-us/dotnet/api/system.globalization.culturetypes?view=net-10.0

### **Tools** section

Key | Type | Description
--- | --- | ---
**XMLDirectory** | string | The directory where XML files are stored.

## 🧩 Registering services in Program.cs

```csharp
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
});
builder.Services.AddCryptographyClient();
```

## 🧱 KSeF Services

The following are registered: - IKSeFClient

- ITestDataClient
- IAuthCoordinator
- ILimitsClient
- IVerificationLinkService

## 🔧 Certificates

By default, the following is used: - DefaultCertificateFetcher

- CryptographyService
- HostedService (warmup)

## 🧪 Launching the project

```
dotnet restore
dotnet run --framework net9.0
```

## 📦 Requirements

- .NET 8+

## 🔐 Endpoint: `POST /auth/auth-by-coordinator-with-pz`

The Endpoint is used to start the authentication process in the KSeF test environment using the coordinator and the Trusted Profile signature.

---

## 🧭 Operating Instructions

After calling this endpoint, the application:

1. Creates **an XML file** in the directory specified in the configuration:

    ```
    Tools:XMLDirectory
    ```

2. This file contains data that **must be signed** -- e.g. via a Trusted Profile (TP) or other acceptable authenticated signature.

3. After signing, the file should be saved **in the same directory** , with **the same name** , but with the following extension added:

    ```
    (1)
    ```

    **Example:**

    ```
    request.xml
    request (1).xml
    ```

## 🔐 Endpoint: `POST /auth/auth-step-by-step`

The endpoint is used to conduct a manual, step-by-step authentication process in the KSeF test environment, with the signature executed automatically in the code, rather than by the Trusted Profile or the user. This is a simplified variant, intended solely for testing and simulating the authorization flow.

---

## 🧭 Operating Instructions

After calling this endpoint, the application:

Runs the standard authorization process (similar to auth-by-coordinator-with-PZ), but without generating the XML file to sign.

It does not require a signature from the user – the process does not involve a Trusted Profile (TP), ePUAP or external signing services.

Instead, the authorization signature is performed automatically.

The authorization result is returned without requiring additional user action.
