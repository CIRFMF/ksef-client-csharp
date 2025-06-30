using KSeF.Client.Core.Models.Authorization;
using AuthTokenRequest = KSeF.Client.Core.Models.Authorization.AuthTokenRequest;


namespace KSeFClient.Api.Builders.Auth;

public static class AuthTokenRequestBuilder
{

    public static IAuthTokenRequestBuilder Create() =>
        AuthTokenRequestBuilderImpl.Create();
}

public interface IAuthTokenRequestBuilder
{
    IAuthTokenRequestBuilderWithChallenge WithChallenge(string challenge);
}

public interface IAuthTokenRequestBuilderWithChallenge
{
    IAuthTokenRequestBuilderWithContext WithContext(ContextIdentifierType type, string value);
}
public interface IAuthTokenRequestBuilderWithContext
{
    IAuthTokenRequestBuilderReady WithIdentifierType(SubjectIdentifierTypeEnum type);
}

public interface IAuthTokenRequestBuilderReady
{
    IAuthTokenRequestBuilderReady WithIpAddressPolicy(IpAddressPolicy ipPolicy);
    AuthTokenRequest Build();
}

internal sealed class AuthTokenRequestBuilderImpl :
    IAuthTokenRequestBuilder,
    IAuthTokenRequestBuilderWithChallenge,
    IAuthTokenRequestBuilderReady,
    IAuthTokenRequestBuilderWithContext
{
    private string _challenge;
    private AuthContextIdentifier  _context;
    private IpAddressPolicy _ipPolicy;
    private SubjectIdentifierTypeEnum _authIdentifierType;

    private AuthTokenRequestBuilderImpl() { }

    public IAuthTokenRequestBuilderWithChallenge WithChallenge(string challenge)
    {
        if (string.IsNullOrWhiteSpace(challenge))
            throw new ArgumentException(nameof(challenge));

        _challenge = challenge;
        return this;
    }

    public IAuthTokenRequestBuilderWithContext WithContext(ContextIdentifierType type, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(nameof(value));

        _context = new AuthContextIdentifier  {  Type = type, Value = value };
        return this;
    }

    public IAuthTokenRequestBuilderReady WithIdentifierType(SubjectIdentifierTypeEnum type)
    {
        _authIdentifierType = type;
        return this;
    }

    public IAuthTokenRequestBuilderReady WithIpAddressPolicy(IpAddressPolicy ipPolicy)
    {
        _ipPolicy = ipPolicy ?? throw new ArgumentNullException(nameof(ipPolicy));
        return this;
    }

    public AuthTokenRequest Build()
    {
        if (_challenge is null)
            throw new InvalidOperationException();
        if (_context is null)
            throw new InvalidOperationException();

        return new AuthTokenRequest
        {
            Challenge = _challenge,
            ContextIdentifier = _context,
            SubjectIdentifierType = _authIdentifierType,
            IpAddressPolicy = _ipPolicy,
        };
    }

    public static IAuthTokenRequestBuilder Create() =>
        new AuthTokenRequestBuilderImpl();
}
