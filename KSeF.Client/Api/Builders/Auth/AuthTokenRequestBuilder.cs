using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.Auth;

/// <summary>
/// Udostępnia metody do tworzenia żądań standardowego tokenu uwierzytelniającego.
/// </summary>
/// <remarks>
/// Użyj <see cref="Create"/> aby otrzymać builder, następnie ustaw challenge, kontekst,
/// typ identyfikatora i ewentualną politykę autoryzacji, a na końcu wywołaj <see cref="IAuthTokenRequestBuilderReady.Build"/>.
/// </remarks>
public static class AuthTokenRequestBuilder
{
    /// <summary>
    /// Tworzy nowy builder żądania tokenu uwierzytelniającego.
    /// </summary>
    /// <returns>
    /// Instancja <see cref="IAuthTokenRequestBuilder"/> pozwalająca rozpocząć budowę żądania.
    /// </returns>
    public static IAuthTokenRequestBuilder Create() =>
        AuthTokenRequestBuilderImpl.Create();
}

/// <summary>
/// Pierwszy etap budowy żądania tokenu – ustawienie wartości challenge.
/// </summary>
public interface IAuthTokenRequestBuilder
{
    /// <summary>
    /// Ustawia wartość challenge otrzymaną z usługi uwierzytelniania.
    /// </summary>
    /// <param name="challenge">
    /// Wartość challenge przekazana przez KSeF. Nie może być pusta i musi mieć długość
    /// wymaganą przez <see cref="ValidValues.RequiredChallengeLength"/>.
    /// </param>
    /// <returns>Interfejs pozwalający ustawić kontekst żądania.</returns>
    IAuthTokenRequestBuilderWithChallenge WithChallenge(string challenge);
}

/// <summary>
/// Etap budowy żądania, w którym dodawany jest kontekst tokenu.
/// </summary>
public interface IAuthTokenRequestBuilderWithChallenge
{
    /// <summary>
    /// Ustawia kontekst żądania tokenu na podstawie typu identyfikatora i wartości.
    /// </summary>
    /// <param name="type">Typ identyfikatora kontekstu.</param>
    /// <param name="value">Wartość identyfikatora kontekstu.</param>
    /// <returns>Interfejs pozwalający określić typ identyfikatora podmiotu.</returns>
    IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value);

    /// <summary>
    /// Ustawia kontekst żądania tokenu na podstawie gotowego obiektu identyfikatora.
    /// </summary>
    /// <param name="contextIdentifier">
    /// Obiekt reprezentujący kontekst tokenu. Nie może być null i musi przejść walidację typu/wartości.
    /// </param>
    /// <returns>Interfejs pozwalający określić typ identyfikatora podmiotu.</returns>
    IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier);
}

/// <summary>
/// Etap budowy żądania, w którym wybierany jest typ identyfikatora podmiotu.
/// </summary>
public interface IAuthTokenRequestBuilderWithContext
{
    /// <summary>
    /// Określa typ identyfikatora podmiotu używany w żądaniu tokenu (np. NIP, PESEL).
    /// </summary>
    /// <param name="type">Typ identyfikatora podmiotu.</param>
    /// <returns>
    /// Interfejs gotowy do ustawienia polityki autoryzacji lub zbudowania żądania.
    /// </returns>
    IAuthTokenRequestBuilderReady WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum type);
}

/// <summary>
/// Końcowy etap budowy żądania tokenu – opcjonalna polityka autoryzacji i utworzenie obiektu żądania.
/// </summary>
public interface IAuthTokenRequestBuilderReady
{
    /// <summary>
    /// Ustawia zasady autoryzacji używane przy wydawaniu tokenu (np. dozwolone adresy IP).
    /// </summary>
    /// <param name="authorizationPolicy">
    /// Polityka autoryzacji. Gdy null, żadne dodatkowe ograniczenia nie są stosowane.
    /// </param>
    /// <returns>Ten sam interfejs, umożliwiający dalszą konfigurację lub wywołanie <see cref="Build"/>.</returns>
    IAuthTokenRequestBuilderReady WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy);

    /// <summary>
    /// Tworzy obiekt żądania tokenu na podstawie ustawionych wartości.
    /// </summary>
    /// <returns>
    /// Obiekt <see cref="AuthenticationTokenRequest"/> gotowy do wysłania do KSeF.
    /// </returns>
    AuthenticationTokenRequest Build();
}

/// <inheritdoc />
internal sealed class AuthTokenRequestBuilderImpl :
    IAuthTokenRequestBuilder,
    IAuthTokenRequestBuilderWithChallenge,
    IAuthTokenRequestBuilderReady,
    IAuthTokenRequestBuilderWithContext
{
    private string _challenge;
    private AuthenticationTokenContextIdentifier _context;
    private AuthenticationTokenAuthorizationPolicy _authorizationPolicy;
    private AuthenticationTokenSubjectIdentifierTypeEnum _authIdentifierType;

    private AuthTokenRequestBuilderImpl() { }

    /// <inheritdoc />
    public IAuthTokenRequestBuilderWithChallenge WithChallenge(string challenge)
    {
        if (string.IsNullOrWhiteSpace(challenge))
        {
            throw new ArgumentNullException(nameof(challenge));
        }
        if (challenge.Length != ValidValues.RequiredChallengeLength)
        {
            throw new ArgumentException($"Podany parametr: {nameof(challenge)} nie ma wymaganej liczby {ValidValues.RequiredChallengeLength} znaków", nameof(challenge));
        }

        _challenge = challenge;
        return this;
    }

    /// <inheritdoc />
    public IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value)
    {
        AuthenticationTokenContextIdentifier context = new() { Type = type, Value = value };
        return WithContext(context);
    }

    /// <inheritdoc />
    public IAuthTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier)
    {
        Guard.ThrowIfNull(contextIdentifier);
        if (!TypeValueValidator.Validate(contextIdentifier))
        {
            throw new ArgumentException($"Nieprawidłowa wartość dla typu {contextIdentifier.Type}", nameof(contextIdentifier));
        }

        _context = contextIdentifier;
        return this;
    }

    /// <inheritdoc />
    public IAuthTokenRequestBuilderReady WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum type)
    {
        _authIdentifierType = type;
        return this;
    }

    /// <inheritdoc />
    public IAuthTokenRequestBuilderReady WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy)
    {
        if (authorizationPolicy is null)
        {
            return this;
        }

        AuthenticationTokenAllowedIps allowedIps = authorizationPolicy.AllowedIps;
        if (allowedIps is null)
        {
            return this;
        }

        foreach (string ipAddress in allowedIps.Ip4Addresses ?? Enumerable.Empty<string>())
        {
            if (!RegexPatterns.Ip4Address.IsMatch(ipAddress))
            {
                throw new ArgumentException($"Nieprawidłowy adres IP: {ipAddress}", nameof(authorizationPolicy));
            }
        }
        foreach (string ipRange in allowedIps.Ip4Ranges ?? Enumerable.Empty<string>())
        {
            if (!RegexPatterns.Ip4Range.IsMatch(ipRange))
            {
                throw new ArgumentException($"Nieprawidłowy zakres adresów IP: {ipRange}", nameof(authorizationPolicy));
            }
        }
        foreach (string ipMask in allowedIps.Ip4Masks ?? Enumerable.Empty<string>())
        {
            if (!RegexPatterns.Ip4Mask.IsMatch(ipMask))
            {
                throw new ArgumentException($"Nieprawidłowy adres IP/maska: {ipMask}", nameof(authorizationPolicy));
            }
        }

        _authorizationPolicy = authorizationPolicy;
        return this;
    }

    /// <inheritdoc />
    public AuthenticationTokenRequest Build()
    {
        if (_challenge is null)
        {
            throw new InvalidOperationException("Challenge nie został ustawiony.");
        }

        if (_context is null)
        {
            throw new InvalidOperationException("Kontekst żądania tokenu nie został ustawiony.");
        }

        return new AuthenticationTokenRequest
        {
            Challenge = _challenge,
            ContextIdentifier = _context,
            SubjectIdentifierType = _authIdentifierType,
            AuthorizationPolicy = _authorizationPolicy,
        };
    }

    /// <inheritdoc />
    public static IAuthTokenRequestBuilder Create() =>
        new AuthTokenRequestBuilderImpl();
}