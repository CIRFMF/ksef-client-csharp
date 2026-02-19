using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Validation;

namespace KSeF.Client.Api.Builders.Auth;

/// <summary>
/// Udostępnia metody do tworzenia żądań tokenu KSeF.
/// </summary>
/// <remarks>
/// Klasa działa jak punkt startowy – użyj <see cref="Create"/> żeby otrzymać builder
/// i krok po kroku zbudować żądanie <see cref="AuthenticationKsefTokenRequest"/>.
/// </remarks>
public static class AuthKsefTokenRequestBuilder
{
    /// <summary>
    /// Tworzy nowy builder żądania tokenu KSeF.
    /// </summary>
    /// <remarks>
    /// Zwrócony obiekt pozwala ustawić challenge, kontekst, zaszyfrowany token
    /// i opcjonalne zasady autoryzacji.
    /// </remarks>
    /// <returns>
    /// Instancja <see cref="IAuthKsefTokenRequestBuilder"/> służąca do budowy żądania tokenu.
    /// </returns>
    public static IAuthKsefTokenRequestBuilder Create() =>
        AuthKsefTokenRequestBuilderImpl.Create();
}

/// <summary>
/// Reprezentuje pierwszy etap budowy żądania tokenu KSeF – ustawienie wartości challenge.
/// </summary>
public interface IAuthKsefTokenRequestBuilder
{
    /// <summary>
    /// Ustawia wartość challenge otrzymaną z usługi uwierzytelniania.
    /// </summary>
    /// <param name="challenge">
    /// Wartość challenge przekazana przez KSeF. Nie może być pusta
    /// i musi mieć długość wymaganą przez <see cref="ValidValues.RequiredChallengeLength"/>.
    /// </param>
    /// <returns>
    /// Interfejs <see cref="IAuthKsefTokenRequestBuilderWithChallenge"/> pozwalający dodać kontekst żądania.
    /// </returns>
    IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge);
}

/// <summary>
/// Etap budowy żądania, w którym do challenge dodawany jest kontekst tokenu.
/// </summary>
public interface IAuthKsefTokenRequestBuilderWithChallenge
{
    /// <summary>
    /// Ustawia kontekst żądania tokenu na podstawie typu identyfikatora i wartości.
    /// </summary>
    /// <param name="type">Typ identyfikatora kontekstu.</param>
    /// <param name="value">Wartość identyfikatora kontekstu.</param>
    /// <returns>
    /// Interfejs <see cref="IAuthKsefTokenRequestBuilderWithContext"/> pozwalający dodać zaszyfrowany token.
    /// </returns>
    IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value);

    /// <summary>
    /// Ustawia kontekst żądania tokenu na podstawie gotowego obiektu identyfikatora.
    /// </summary>
    /// <param name="contextIdentifier">
    /// Obiekt reprezentujący kontekst tokenu. Nie może być null i musi przejść walidację typu/wartości.
    /// </param>
    /// <returns>
    /// Interfejs <see cref="IAuthKsefTokenRequestBuilderWithContext"/> pozwalający dodać zaszyfrowany token.
    /// </returns>
    IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier);
}

/// <summary>
/// Etap budowy żądania, w którym ustawiany jest dodatkowy kontekst i zaszyfrowany token.
/// </summary>
public interface IAuthKsefTokenRequestBuilderWithContext
{
    /// <summary>
    /// Ustawia zaszyfrowany token, który ma zostać użyty w żądaniu.
    /// </summary>
    /// <param name="encryptedToken">
    /// Zaszyfrowany ciąg znaków reprezentujący token. Nie może być pusty.
    /// </param>
    /// <returns>
    /// Interfejs pozwalający dodać zasady autoryzacji lub zbudować żądanie.
    /// </returns>
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken);
}

/// <summary>
/// Końcowy etap budowy żądania tokenu KSeF – ustawienie zasad autoryzacji i utworzenie obiektu żądania.
/// </summary>
public interface IAuthKsefTokenRequestBuilderWithEncryptedToken
{
    /// <summary>
    /// Ustawia zasady autoryzacji używane przy generowaniu tokenu (np. dozwolone adresy IP).
    /// </summary>
    /// <param name="authorizationPolicy">
    /// Polityka autoryzacji. Gdy null, żadne dodatkowe ograniczenia nie są stosowane.
    /// </param>
    /// <returns>
    /// Ten sam interfejs, umożliwiający dalszą konfigurację lub wywołanie <see cref="Build"/>.
    /// </returns>
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy);

    /// <summary>
    /// Tworzy obiekt żądania tokenu KSeF na podstawie ustawionych wartości.
    /// </summary>
    /// <returns>
    /// Obiekt <see cref="AuthenticationKsefTokenRequest"/> gotowy do wysłania do KSeF.
    /// </returns>
    AuthenticationKsefTokenRequest Build();
}

/// <inheritdoc />
internal sealed class AuthKsefTokenRequestBuilderImpl :
    IAuthKsefTokenRequestBuilder,
    IAuthKsefTokenRequestBuilderWithChallenge,
    IAuthKsefTokenRequestBuilderWithContext,
    IAuthKsefTokenRequestBuilderWithEncryptedToken
{
    private string _challenge;
    private AuthenticationTokenContextIdentifier _contextIdentifier;
    private string _encryptedToken;
    private AuthenticationTokenAuthorizationPolicy _authorizationPolicy;

    private AuthKsefTokenRequestBuilderImpl() { }

    /// <inheritdoc />
    public static IAuthKsefTokenRequestBuilder Create() =>
        new AuthKsefTokenRequestBuilderImpl();

    /// <inheritdoc />
    public IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge)
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
    public IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifierType type, string value)
    {
        AuthenticationTokenContextIdentifier context = new() { Type = type, Value = value };
        return WithContext(context);
    }

    /// <inheritdoc />
    public IAuthKsefTokenRequestBuilderWithContext WithContext(AuthenticationTokenContextIdentifier contextIdentifier)
    {
        Guard.ThrowIfNull(contextIdentifier);
        if (!TypeValueValidator.Validate(contextIdentifier))
        {
            throw new ArgumentException($"Nieprawidłowa wartość dla typu {contextIdentifier.Type}", nameof(contextIdentifier));
        }

        _contextIdentifier = contextIdentifier;
        return this;
    }

    /// <inheritdoc />
    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken)
    {
        if (string.IsNullOrWhiteSpace(encryptedToken))
        {
            throw new ArgumentNullException(nameof(encryptedToken));
        }

        _encryptedToken = encryptedToken;
        return this;
    }

    /// <inheritdoc />
    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithAuthorizationPolicy(AuthenticationTokenAuthorizationPolicy authorizationPolicy)
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
    public AuthenticationKsefTokenRequest Build()
    {
        if (_challenge is null || _contextIdentifier is null || _encryptedToken is null)
        {
            throw new InvalidOperationException("Brak wymaganych pól.");
        }

        return new AuthenticationKsefTokenRequest
        {
            Challenge = _challenge,
            ContextIdentifier = _contextIdentifier,
            EncryptedToken = _encryptedToken,
            AuthorizationPolicy = _authorizationPolicy
        };
    }
}