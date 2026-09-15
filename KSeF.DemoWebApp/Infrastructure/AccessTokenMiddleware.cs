namespace KSeF.DemoWebApp.Infrastructure;

/// <summary>
/// Przekazuje token z nagłówka <c>Authorization</c> do parametrów <c>accessToken</c> i <c>token</c>.
/// Wartość podana jawnie w query ma pierwszeństwo.
/// Usuwa prefiks <c>Bearer </c> i normalizuje nagłówek do samego tokenu.
/// Działa przed bindowaniem MVC, dzięki czemu token ustawiony w Swaggerze przez <c>Authorize</c>
/// jest dostępny jako parametr akcji kontrolera.
/// </summary>
public sealed class AccessTokenMiddleware(RequestDelegate next)
{
    private const string BearerPrefix = "Bearer ";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues raw)
            && !string.IsNullOrWhiteSpace(raw))
        {
            string token = raw.ToString().Trim();
            if (token.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                token = token[BearerPrefix.Length..].Trim();
            }

            context.Request.Headers.Authorization = token;

            List<KeyValuePair<string, string?>> query = [.. context.Request.Query
                .Where(kv => !IsTokenKey(kv.Key))
                .SelectMany(kv => kv.Value.Select(v => new KeyValuePair<string, string?>(kv.Key, v)))];

            foreach (string tokenKey in new[] { "accessToken", "token" })
            {
                string? provided = context.Request.Query.TryGetValue(tokenKey, out Microsoft.Extensions.Primitives.StringValues existing)
                    ? existing.ToString()
                    : null;
                query.Add(new KeyValuePair<string, string?>(tokenKey, string.IsNullOrEmpty(provided) ? token : provided));
            }

            context.Request.QueryString = QueryString.Create(query);
        }

        await next(context).ConfigureAwait(false);
    }

    private static bool IsTokenKey(string key)
        => string.Equals(key, "accessToken", StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, "token", StringComparison.OrdinalIgnoreCase);
}
