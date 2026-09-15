using KSeF.Client.ClientFactory.DI;
using KSeF.Client.DI;
using KSeF.DemoWebApp.Infrastructure;
using KSeF.DemoWebApp.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi;
using System.Text.Json;
using System.Text.Json.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BackgroundKsefOptions>(
    builder.Configuration.GetSection(BackgroundKsefOptions.SectionName));

// Osobna ścieżka DI dla jobów / BackgroundService (singletonowa fabryka klientów).
builder.Services.RegisterKSeFClientFactory();
builder.Services.AddSingleton<IKsefAuthJobResultStore, KsefAuthJobResultStore>();
builder.Services.AddSingleton<IKsefBackgroundWorkResultStore, KsefBackgroundWorkResultStore>();
builder.Services.AddSingleton<IKsefBackgroundJob, KsefAuthenticationBackgroundJob>();
builder.Services.AddSingleton<IKsefBackgroundJob, KsefInvoiceUpoBackgroundJob>();
builder.Services.AddHostedService<KsefClientBackgroundService>();

builder.Services.AddKSeFClient
    (options =>
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

    options.UseCamelCaseForRequests = builder.Configuration.GetSection("ApiSettings").GetValue<bool>("UseCamelCaseForRequests");
});

builder.Services.AddLighthouseClient(options =>
{
    options.BaseUrl =
        builder.Configuration.GetSection("LighthouseSettings")
                .GetValue<string>("BaseUrl")
                ?? LighthouseEnvironmentsUris.TEST;
});

// UWAGA: w aplikacji webowej używamy AddCryptographyClient do rejestracji CryptographyClient i powiązanych serwisów
// tutaj z domyślnym DefaultCertificateFetcher pobierającym certyfikaty, zobacz dostępne parametry w dokumentacji metody rozszerzającej
builder.Services.AddCryptographyClient();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters
            .Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase))
    );
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	// Umożliwia jednorazowe ustawienie tokenu KSeF w Swaggerze za pomocą „Authorize".
	// Token jest przekazywany w nagłówku Authorization, a AccessTokenMiddleware
	// wykorzystuje go jako wartość parametrów accessToken/token.
	const string securitySchemeId = "ksef-access-token";
    c.AddSecurityDefinition(securitySchemeId, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Access token KSeF (z prefiksem \"Bearer \" lub bez).",
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(securitySchemeId, document, null)] = new List<string>()
    });

    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "KSeF.DemoWebApp.xml"));
    c.CustomSchemaIds(t =>
   (t.Namespace + "_" + t.Name)
     .Replace(".", "_")
     .Replace("+", "_")
     .Replace("`", "_")
 );
});

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    options.SerializerOptions.AllowTrailingCommas = true;

#if NET10_0_OR_GREATER
    options.SerializerOptions.AllowOutOfOrderMetadataProperties = true;
#endif

    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<AccessTokenMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
