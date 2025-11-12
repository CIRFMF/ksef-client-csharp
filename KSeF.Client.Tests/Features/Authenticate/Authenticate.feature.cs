using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Tests.Utils;
using KSeF.Client.Api.Builders.Auth;
using KSeF.Client.Core.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.ApiResponses;

namespace KSeF.Client.Tests.Features;

[Collection("Authenticate.feature")]
[Trait("Category", "Features")]
[Trait("Features", "authenticate.feature")]
public class AuthenticateTests : KsefIntegrationTestBase
{
    [Fact]
    [Trait("Scenario", "Uwierzytelnienie za pomocą certyfikatu z identyfikatorem NIP, na uprawnienie właściciel")]
    public async Task GivenOwnerContextAndOwnerPermission_WhenAuthenticatingWithCertificate_ThenAccessTokenReturned()
    {
        string nip = MiscellaneousUtils.GetRandomNip();
        string accessToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, nip)).AccessToken.Token;

        Assert.NotNull(accessToken);

        HashSet<string> per = GetPerAsStringSet(accessToken);
        Assert.Contains("Owner", per);
    }

    [Theory]
    // ===== pesel =====
    [InlineData("pesel", new PersonPermissionType[] { PersonPermissionType.InvoiceWrite })]
    [InlineData("pesel", new PersonPermissionType[] { PersonPermissionType.InvoiceRead })]
    [InlineData("pesel", new PersonPermissionType[] { PersonPermissionType.CredentialsManage })]
    [InlineData("pesel", new PersonPermissionType[] { PersonPermissionType.CredentialsRead })]
    [InlineData("pesel", new PersonPermissionType[] { PersonPermissionType.Introspection })]
    [InlineData("pesel", new PersonPermissionType[] { PersonPermissionType.SubunitManage })]
    // ===== nip =====
    [InlineData("nip", new PersonPermissionType[] { PersonPermissionType.InvoiceWrite })]
    [InlineData("nip", new PersonPermissionType[] { PersonPermissionType.InvoiceRead })]
    [InlineData("nip", new PersonPermissionType[] { PersonPermissionType.CredentialsManage })]
    [InlineData("nip", new PersonPermissionType[] { PersonPermissionType.CredentialsRead })]
    [InlineData("nip", new PersonPermissionType[] { PersonPermissionType.Introspection })]
    [InlineData("nip", new PersonPermissionType[] { PersonPermissionType.SubunitManage })]
    [Trait("Scenario", "Uwierzytelnienie certyfikatem (PESEL/NIP) na różne uprawnienia")]
    public async Task GivenOwnerContextAndPermissionGranted_WhenAuthenticatingAsSubject_ThenAccessTokenReturned(
        string identifierKind,
        PersonPermissionType[] permissions,
        AuthenticationTokenContextIdentifierType contextIdentifierType = AuthenticationTokenContextIdentifierType.Nip)
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string ownerToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip)).AccessToken.Token;

        string delegateNip = MiscellaneousUtils.GetRandomNip();
        string pesel = MiscellaneousUtils.GetRandomPesel();

        GrantPermissionsPersonSubjectIdentifier subjectIdentifier;
        if (identifierKind.Equals("pesel", StringComparison.OrdinalIgnoreCase))
        {
            subjectIdentifier = new GrantPermissionsPersonSubjectIdentifier { Type = GrantPermissionsPersonSubjectIdentifierType.Pesel, Value = pesel };
        }
        else
        {
            subjectIdentifier = new GrantPermissionsPersonSubjectIdentifier { Type = GrantPermissionsPersonSubjectIdentifierType.Nip, Value = delegateNip };
        }
        await PermissionsUtils.GrantPersonPermissionsAsync(KsefClient, ownerToken, subjectIdentifier, permissions);

        AuthenticationChallengeResponse challengeResponse = await AuthorizationClient
            .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(contextIdentifierType, ownerNip)
           .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
           .WithAuthorizationPolicy(new AuthenticationTokenAuthorizationPolicy { /* ... */ })
           .Build();

        string unsignedXml = authTokenRequest.SerializeToXmlString();

        using System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = identifierKind.Equals("pesel", StringComparison.OrdinalIgnoreCase)
            ? SelfSignedCertificateForSignatureBuilder
                .Create()
                .WithGivenName("A")
                .WithSurname("R")
                .WithSerialNumber("PNOPL-" + pesel)
                .WithCommonName("A R")
                .Build()
            : SelfSignedCertificateForSignatureBuilder
                .Create()
                .WithGivenName("Jan")
                .WithSurname("Kowalski")
                .WithSerialNumber("TINPL-" + delegateNip)
                .WithCommonName("Jan Kowalski")
                .Build();
        string signedXml = SignatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await AuthorizationClient
          .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus status = await EnsureAuthenticationCompletedAsync(
            AuthorizationClient,
            authOperationInfo.ReferenceNumber,
            authOperationInfo.AuthenticationToken.Token);

        Assert.Equal(200, status.Status.Code);

        AuthenticationOperationStatusResponse accessToken = await AuthorizationClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
        Assert.NotNull(accessToken);
        HashSet<PersonPermissionType> actual = GetPerAsEnumSet<PersonPermissionType>(accessToken.AccessToken.Token);
        Assert.True(permissions.ToHashSet().IsSubsetOf(actual));
    }

    [Theory]
    // ===== nip =====
    [InlineData(EntityStandardPermissionType.InvoiceRead)]
    [InlineData(EntityStandardPermissionType.InvoiceWrite)]
    [Trait("Scenario", "Uwierzytelnienie za pomocą pieczęci z nip, na różne uprawnienia")]
    public async Task GivenOwnerContextAndPermissionGranted_WhenAuthenticatingAsSubjectEntity_ThenAccessTokenReturned(
        EntityStandardPermissionType permission,
        AuthenticationTokenContextIdentifierType contextIdentifierType = AuthenticationTokenContextIdentifierType.Nip)
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string ownerToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip)).AccessToken.Token;

        string delegateNip = MiscellaneousUtils.GetRandomNip();

        GrantPermissionsEntitySubjectIdentifier subject = new GrantPermissionsEntitySubjectIdentifier
        { Type = GrantPermissionsEntitySubjectIdentifierType.Nip, Value = delegateNip };

        GrantPermissionsEntityRequest request = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithPermissions(EntityPermission.New(permission, true))
            .WithDescription($"Grant {string.Join(", ", permission)} to {subject.Type}:{subject.Value}")
            .Build();

        await KsefClient.GrantsPermissionEntityAsync(request, ownerToken);

        AuthenticationChallengeResponse challengeResponse = await AuthorizationClient
            .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(contextIdentifierType, ownerNip)
           .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
           .WithAuthorizationPolicy(new AuthenticationTokenAuthorizationPolicy { /* ... */ })
           .Build();

        string unsignedXml = authTokenRequest.SerializeToXmlString();

        using System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSealBuilder
            .Create()
            .WithOrganizationName("AR sp. z o.o")
            .WithOrganizationIdentifier("VATPL-" + delegateNip)
            .WithCommonName("A R")
            .Build();

        string signedXml = SignatureService.Sign(unsignedXml, certificate);

        SignatureResponse authOperationInfo = await AuthorizationClient
          .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);

        AuthStatus status = await EnsureAuthenticationCompletedAsync(
            AuthorizationClient,
            authOperationInfo.ReferenceNumber,
            authOperationInfo.AuthenticationToken.Token);

        Assert.Equal(AuthenticationStatusCodeResponse.AuthenticationSuccess, status.Status.Code);

        AuthenticationOperationStatusResponse token = await AuthorizationClient.GetAccessTokenAsync(authOperationInfo.AuthenticationToken.Token);
        Assert.NotNull(token);
        HashSet<EntityStandardPermissionType> entitySet = GetPerAsEnumSet<EntityStandardPermissionType>(token.AccessToken.Token);
        Assert.Contains(permission, entitySet);
    }

    [Fact]
    [Trait("Scenario", "Uwierzytelnienie za pomocą PESEL oraz niepoprawnego certyfikatu z PESEL")]
    public async Task GivenOwnerContextAndWrongCertificate_WhenAuthenticateWithPESEL_ThenError()
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string ownerToken = (await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip)).AccessToken.Token;

        string delegateNip = MiscellaneousUtils.GetRandomNip();
        string pesel = MiscellaneousUtils.GetRandomPesel();

        AuthenticationChallengeResponse challengeResponse = await AuthorizationClient
            .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(AuthenticationTokenContextIdentifierType.Nip, ownerNip)
           .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
           .WithAuthorizationPolicy(new AuthenticationTokenAuthorizationPolicy { /* ... */ })
           .Build();

        string unsignedXml = authTokenRequest.SerializeToXmlString();

        //błąd w certyfikacie
        using System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
           .Create()
           .WithGivenName("A")
           .WithSurname("R")
           .WithSerialNumber("-" + pesel)
           .WithCommonName("A R")
           .Build();

        string signedXml = SignatureService.Sign(unsignedXml, certificate);

        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(async () =>
        {
            SignatureResponse authOperationInfo = await AuthorizationClient
          .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);
        });
    }

    [Fact]
    [Trait("Scenario", "Niepoprawne uwierzytelnienia - brak żądania autoryzacyjnego")]
    public async Task GivenOwnerContextAndNip_WhenAuthenticatingWithWrongData_ThenError()
    {
        string nip = MiscellaneousUtils.GetRandomNip();

        // brak żądania autoryzacyjnego
        string unsignedXml = AuthenticationTokenRequestSerializer.SerializeToXmlString(null);

        using System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
               .Create()
               .WithGivenName("Jan")
               .WithSurname("Kowalski")
               .WithSerialNumber("TINPL-" + nip)
               .WithCommonName("Jan Kowalski")
               .Build();

        string signedXml = SignatureService.Sign(unsignedXml, certificate);
        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(async () =>
        {
            SignatureResponse authOperationInfo = await AuthorizationClient
          .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);
        });
    }

    [Fact]
    [Trait("Scenario", "Niepoprawne uwierzytelnienia - zły nip w podpisie")]
    public async Task GivenOwnerContext_WhenAuthenticatingWithWrongNip_ThenError()
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationChallengeResponse challengeResponse = await AuthorizationClient
                        .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(AuthenticationTokenContextIdentifierType.Nip, ownerNip)
           .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
           .WithAuthorizationPolicy(new AuthenticationTokenAuthorizationPolicy { /* ... */ })
           .Build();

        string unsignedXml = authTokenRequest.SerializeToXmlString();

        //niepoprawny nip
        using System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
               .Create()
               .WithGivenName("Jan")
               .WithSurname("Kowalski")
               .WithSerialNumber("TINPL-111111")
               .WithCommonName("Jan Kowalski")
               .Build();

        string signedXml = SignatureService.Sign(unsignedXml, certificate);

        KsefApiException ex = await Assert.ThrowsAsync<KsefApiException>(async () =>
        {
            SignatureResponse authOperationInfo = await AuthorizationClient
          .SubmitXadesAuthRequestAsync(signedXml, false, CancellationToken.None);
        });
    }

    [Fact]
    [Trait("Scenario", "Niepoprawne uwierzytelnienia - błędny plik autoryzacyjny")]
    public async Task GivenOwnerContext_WhenAuthenticatingWithWrongAuthenticateFile_ThenError()
    {
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string nip = MiscellaneousUtils.GetRandomNip();

        AuthenticationChallengeResponse challengeResponse = await AuthorizationClient
                        .GetAuthChallengeAsync();

        AuthenticationTokenRequest authTokenRequest = AuthTokenRequestBuilder
           .Create()
           .WithChallenge(challengeResponse.Challenge)
           .WithContext(AuthenticationTokenContextIdentifierType.Nip, ownerNip)
           .WithIdentifierType(AuthenticationTokenSubjectIdentifierTypeEnum.CertificateSubject)
           .WithAuthorizationPolicy(new AuthenticationTokenAuthorizationPolicy { /* ... */ })
           .Build();

        string unsignedXml = authTokenRequest.SerializeToXmlString();

        using System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
               .Create()
               .WithGivenName("Jan")
               .WithSurname("Kowalski")
               .WithSerialNumber("TINPL-" + nip)
               .WithCommonName("Jan Kowalski")
               .Build();

        Assert.Throws<ArgumentException>(() => SignatureService.Sign(string.Empty, certificate));
    }

    private static async Task<AuthStatus> EnsureAuthenticationCompletedAsync(
        IAuthorizationClient client,
        string operationReferenceNumber,
        string authToken,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromSeconds(60);
        pollInterval ??= TimeSpan.FromSeconds(1);

        int maxAttempts = (int)Math.Ceiling(timeout.Value.TotalMilliseconds / pollInterval.Value.TotalMilliseconds);

        return await AsyncPollingUtils.PollAsync(
            action: async () =>
            {
                AuthStatus status = await client.GetAuthStatusAsync(operationReferenceNumber, authToken, cancellationToken);
                Console.WriteLine(
                    $"Polling: StatusCode={status.Status.Code}, " +
                    $"Description='{status.Status.Description}'");
                return status;
            },
            condition: status => status.Status.Code == AuthenticationStatusCodeResponse.AuthenticationSuccess,
            delay: pollInterval,
            maxAttempts: maxAttempts,
            cancellationToken: cancellationToken);
    }
    private static HashSet<string> GetPerAsStringSet(string jwtToken)
    {
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);

        string[] perClaims = jwt.Claims
            .Where(c => c.Type == "per")
            .Select(c => c.Value)
            .ToArray();

        if (perClaims.Length == 1 && perClaims[0].TrimStart().StartsWith("["))
        {
            string[] arr = JsonSerializer.Deserialize<string[]>(perClaims[0]) ?? Array.Empty<string>();
            return arr.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return perClaims.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<TEnum> GetPerAsEnumSet<TEnum>(string jwtToken)
          where TEnum : struct, Enum
    {
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(jwtToken);
        string[] perValues = jwt.Claims.Where(c => c.Type == "per").Select(c => c.Value).ToArray();

        IEnumerable<string> raw =
            perValues.Length == 1 && perValues[0].TrimStart().StartsWith("[")
                ? JsonSerializer.Deserialize<string[]>(perValues[0]) ?? Array.Empty<string>()
                : perValues;

        HashSet<TEnum> set = new HashSet<TEnum>();
        foreach (string s in raw)
            if (Enum.TryParse<TEnum>(s, ignoreCase: true, out TEnum e))
                set.Add(e);

        return set;
    }
}
