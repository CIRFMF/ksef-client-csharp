using KSeF.Client.Core.Models.Authorization;

namespace KSeFClient.Api.Builders.Auth;

public static class AuthKsefTokenRequestBuilder
{
    public static IAuthKsefTokenRequestBuilder Create() =>
        AuthKsefTokenRequestBuilderImpl.Create();
}

public interface IAuthKsefTokenRequestBuilder
{
    IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge);
}

public interface IAuthKsefTokenRequestBuilderWithChallenge
{
    IAuthKsefTokenRequestBuilderWithContext WithContext(ContextIdentifierType type, string value);
}

public interface IAuthKsefTokenRequestBuilderWithContext
{
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken);
}

public interface IAuthKsefTokenRequestBuilderWithEncryptedToken
{
    IAuthKsefTokenRequestBuilderWithEncryptedToken WithIpAddressPolicy(IpAddressPolicy ipPolicy); // optional
    AuthKsefTokenRequest Build();
}

internal sealed class AuthKsefTokenRequestBuilderImpl :
    IAuthKsefTokenRequestBuilder,
    IAuthKsefTokenRequestBuilderWithChallenge,
    IAuthKsefTokenRequestBuilderWithContext,
    IAuthKsefTokenRequestBuilderWithEncryptedToken
{
    private string _challenge;
    private AuthContextIdentifier _contextIdentifier;
    private string _encryptedToken;
    private IpAddressPolicy _ipPolicy; // optional

    private AuthKsefTokenRequestBuilderImpl() { }

    public static IAuthKsefTokenRequestBuilder Create() =>
        new AuthKsefTokenRequestBuilderImpl();

    public IAuthKsefTokenRequestBuilderWithChallenge WithChallenge(string challenge)
    {
        if (string.IsNullOrWhiteSpace(challenge))
            throw new ArgumentException(nameof(challenge));

        _challenge = challenge;
        return this;
    }

    public IAuthKsefTokenRequestBuilderWithContext WithContext(ContextIdentifierType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(nameof(value));

        _contextIdentifier = new AuthContextIdentifier { Type = type, Value = value };
        return this;
    }

    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithEncryptedToken(string encryptedToken)
    {
        if (string.IsNullOrWhiteSpace(encryptedToken))
            throw new ArgumentException(nameof(encryptedToken));

        _encryptedToken = encryptedToken;
        return this;
    }

    public IAuthKsefTokenRequestBuilderWithEncryptedToken WithIpAddressPolicy(IpAddressPolicy ipPolicy)
    {
        _ipPolicy = ipPolicy ?? throw new ArgumentNullException(nameof(ipPolicy));
        return this;
    }

    public AuthKsefTokenRequest Build()
    {
        if (_challenge is null || _contextIdentifier is null || _encryptedToken is null)
            throw new InvalidOperationException("Builder is missing required properties.");

        return new AuthKsefTokenRequest
        {
            Challenge = _challenge,
            ContextIdentifier = _contextIdentifier,
            EncryptedToken = _encryptedToken,
            IpAddressPolicy = _ipPolicy // may be null (optional)
        };
    }
}
