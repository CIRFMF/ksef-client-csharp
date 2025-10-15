using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models.Token;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace KSeF.Client.Api.Services;

public class PersonTokenService : IPersonTokenService
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    public PersonToken MapFromJwt(string jwt)
    {
        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwtToken = handler.ReadJwtToken(jwt);

        IEnumerable<Claim> claims = jwtToken.Claims.ToList();

        string Get(string type) =>
            claims.FirstOrDefault(c => c.Type.Equals(type, StringComparison.OrdinalIgnoreCase))?.Value;

        string[] GetMany(params string[] types) =>
            claims.Where(c => types.Contains(c.Type, StringComparer.OrdinalIgnoreCase))
                  .Select(c => c.Value)
                  .Distinct(StringComparer.OrdinalIgnoreCase)
                  .ToArray();

        DateTimeOffset? exp = jwtToken.Payload.Expiration is { } e
            ? DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(e))
            : null;

        DateTimeOffset? iat = jwtToken.Payload.IssuedAt is { } i
            ? DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(i))
            : null;

        TokenSubjectDetails subjectDetails = TryParseJson<TokenSubjectDetails>(Get("sud"), KseFJsonContext.Default.TokenSubjectDetails);
        TokenIppPolicy ipPolicy = TryParseJson<TokenIppPolicy>(Get("ipp"), KseFJsonContext.Default.TokenIppPolicy);

        string[] per = ParseJsonStringArray(Get("per"));
        string[] pec = ParseJsonStringArray(Get("pec"));
        string[] rol = ParseJsonStringArray(Get("rol"));
        string[] pep = ParseJsonStringArray(Get("pep"));

        string[] roleTypes = new[]
        {
            ClaimTypes.Role, "role", "roles", "permissions",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        string[] classicRoles = GetMany(roleTypes);

        string[] unifiedRoles = classicRoles
            .Concat(per)
            .Concat(rol)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PersonToken
        {
            Issuer = jwtToken.Issuer,
            Audiences = jwtToken.Audiences?.ToArray() ?? Array.Empty<string>(),
            IssuedAt = iat,
            ExpiresAt = exp,

            Roles = unifiedRoles,

            TokenType = Get("typ"),
            ContextIdType = Get("cit"),
            ContextIdValue = Get("civ"),
            AuthMethod = Get("aum"),
            AuthRequestNumber = Get("arn"),
            SubjectDetails = subjectDetails,
            Permissions = per,
            PermissionsExcluded = pec,
            RolesRaw = rol,
            PermissionsEffective = pep,
            IpPolicy = ipPolicy
        };
    }

    private static T TryParseJson<T>(string maybeJson, JsonTypeInfo<T> typeInfo)
    {
        if (string.IsNullOrWhiteSpace(maybeJson)) return default;
        try
        {
            var s = UnwrapIfQuotedJson(maybeJson);
            return JsonSerializer.Deserialize<T>(s, typeInfo);
        }
        catch
        {
            return default;
        }
    }

    private static string[] ParseJsonStringArray(string maybeJsonArray)
    {
        if (string.IsNullOrWhiteSpace(maybeJsonArray)) return Array.Empty<string>();
        try
        {
            string s = UnwrapIfQuotedJson(maybeJsonArray);
            using JsonDocument doc = JsonDocument.Parse(s);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement
                          .EnumerateArray()
                          .Where(e => e.ValueKind == JsonValueKind.String)
                          .Select(e => e.GetString()!)
                          .Where(v => !string.IsNullOrWhiteSpace(v))
                          .Distinct(StringComparer.OrdinalIgnoreCase)
                          .ToArray();
            }
        }
        catch { /* ignore */ }

        if (maybeJsonArray.Contains(','))
            return maybeJsonArray.Split(',')
                                 .Select(x => x.Trim().Trim('"'))
                                 .Where(x => !string.IsNullOrWhiteSpace(x))
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .ToArray();

        return new[] { maybeJsonArray.Trim().Trim('"') };
    }

    private static string UnwrapIfQuotedJson(string s)
    {
        if (s.Length > 1 && s[0] == '"' && s[^1] == '"')
        {
            try
            {
                return JsonSerializer.Deserialize(s, KseFJsonContext.Default.String) ?? s;
            }
            catch
            {
                return s;
            }
        }
        return s;
    }
}