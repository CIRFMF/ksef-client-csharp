#nullable enable
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Authorization.Rev2;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;
using System.Net;

namespace KSeF.Client.Tests.Core.E2E.KsefToken;

/// <summary>
/// Testy E2E tokenów KSeF w kontrakcie Rev2:
/// POST /tokens/rev2, GET /tokens/rev2, GET /tokens/rev2/{referenceNumber},
/// POST /tokens/{referenceNumber}/approve oraz DELETE /tokens/{referenceNumber}.
/// </summary>
[Collection("KsefTokenScenario")]
public class KsefTokenRev2E2ETests : TestBase
{
    private const int SuccessfulAuthStatusCode = 200;
    private const int TokenStatusTimeoutSeconds = 90;

    private static readonly KsefTokenPermissionType[] InvoicePermissions =
    [
        KsefTokenPermissionType.InvoiceRead,
        KsefTokenPermissionType.InvoiceWrite
    ];

    private string OwnerAccessToken { get; }
    private string Nip { get; }

    public KsefTokenRev2E2ETests()
    {
        Nip = MiscellaneousUtils.GetRandomNip();
        AuthenticationOperationStatusResponse authInfo = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, Nip)
                                          .GetAwaiter()
                                          .GetResult();
        OwnerAccessToken = authInfo.AccessToken.Token;
    }

    /// <summary>
    /// Pełny cykl życia tokena Rev2 z jawnie podanym okresem ważności:
    /// generowanie, aktywacja, weryfikacja pól statusu, uwierzytelnienie tokenem, odczyt własnego statusu,
    /// unieważnienie (DELETE /tokens/{referenceNumber}) i weryfikacja statusu Revoked.
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_GenerateWithValidTo_FullLifecycle_AllStepsSucceed()
    {
        DateTimeOffset requestedValidTo = DateTimeOffset.UtcNow.AddDays(7);
        DateTimeOffset beforeGenerate = DateTimeOffset.UtcNow;

        KsefTokenResponseRev2 generated = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = "E2E Rev2 lifecycle token",
            ValidTo = requestedValidTo
        });

        AuthenticationKsefTokenRev2 active = await WaitForStatusAsync(generated.ReferenceNumber, KsefTokenStatusRev2Code.Active);

        Assert.Equal(generated.ReferenceNumber, active.ReferenceNumber);
        Assert.Equal("E2E Rev2 lifecycle token", active.Description);
        Assert.Equal(KsefTokenStatusRev2Code.Active, active.Status.Code);
        Assert.Equal("Active", active.Status.RawCode);
        Assert.False(string.IsNullOrWhiteSpace(active.Status.DisplayName));
        Assert.Null(active.Failure);
        Assert.Equal(KsefTokenContextIdentifierTypeCodeRev2.Nip, active.ContextIdentifier.Type.Code);
        Assert.Equal(Nip, active.ContextIdentifier.Value);
        Assert.False(string.IsNullOrWhiteSpace(active.AuthorIdentifier.Value));
        Assert.True(active.ValidFrom >= beforeGenerate.AddMinutes(-1), $"ValidFrom={active.ValidFrom:o}");
        Assert.True(active.ValidFrom <= DateTimeOffset.UtcNow.AddMinutes(1), $"ValidFrom={active.ValidFrom:o}");
        Assert.True(Math.Abs((active.ValidTo - requestedValidTo).TotalMinutes) < 1, $"ValidTo={active.ValidTo:o}, oczekiwano ok. {requestedValidTo:o}");
        AssertPermissions(active, KsefTokenPermissionCodeRev2.InvoiceRead, KsefTokenPermissionCodeRev2.InvoiceWrite);

        AuthenticationOperationStatusResponse tokenAuth = await AuthenticateWithKsefTokenAsync(generated.Token, Nip);

        AuthenticationKsefTokenRev2 self = await KsefClient.GetKsefTokenRev2Async(
            generated.ReferenceNumber, tokenAuth.AccessToken.Token, CancellationToken);
        Assert.Equal(generated.ReferenceNumber, self.ReferenceNumber);
        Assert.Equal(KsefTokenStatusRev2Code.Active, self.Status.Code);

        await KsefClient.RevokeKsefTokenAsync(generated.ReferenceNumber, OwnerAccessToken, CancellationToken);

        AuthenticationKsefTokenRev2 revoked = await WaitForStatusAsync(generated.ReferenceNumber, KsefTokenStatusRev2Code.Revoked);
        Assert.Equal(KsefTokenStatusRev2Code.Revoked, revoked.Status.Code);
    }

    /// <summary>
    /// Token z ValidFrom w przyszłości zostaje poprawnie wygenerowany, ale nie jest jeszcze aktywny (status NotYetActive).
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_GenerateWithFutureValidFrom_StatusIsNotYetActive()
    {
        DateTimeOffset validFrom = DateTimeOffset.UtcNow.AddDays(1);
        DateTimeOffset validTo = DateTimeOffset.UtcNow.AddDays(3);

        KsefTokenResponseRev2 generated = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = "E2E Rev2 future token",
            ValidFrom = validFrom,
            ValidTo = validTo
        });

        try
        {
            AuthenticationKsefTokenRev2 notYetActive = await WaitForStatusAsync(generated.ReferenceNumber, KsefTokenStatusRev2Code.NotYetActive);

            Assert.Equal(KsefTokenStatusRev2Code.NotYetActive, notYetActive.Status.Code);
            Assert.True(Math.Abs((notYetActive.ValidFrom - validFrom).TotalMinutes) < 1, $"ValidFrom={notYetActive.ValidFrom:o}");
            Assert.True(Math.Abs((notYetActive.ValidTo - validTo).TotalMinutes) < 1, $"ValidTo={notYetActive.ValidTo:o}");

            QueryKsefTokensResponseRev2 filtered = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                statuses: [KsefTokenStatusFilterRev2.NotYetActive],
                cancellationToken: CancellationToken);
            Assert.Contains(filtered.Tokens, t => t.ReferenceNumber == generated.ReferenceNumber);
        }
        finally
        {
            await TryRevokeTokenAsync(generated.ReferenceNumber);
        }
    }

    /// <summary>
    /// Lista tokenów Rev2 filtruje po validFrom (tokeny z datą rozpoczęcia ≥ filtr) oraz validTo (tokeny z datą zakończenia ≤ filtr).
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_Query_FiltersByValidFromAndValidTo()
    {
        string marker = $"Rev2VF-{Guid.NewGuid():N}"[..16];
        DateTimeOffset tokenValidFrom = DateTimeOffset.UtcNow.AddDays(2);
        DateTimeOffset tokenValidTo = DateTimeOffset.UtcNow.AddDays(10);

        KsefTokenResponseRev2 generated = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = marker,
            ValidFrom = tokenValidFrom,
            ValidTo = tokenValidTo
        });

        try
        {
            AuthenticationKsefTokenRev2 token = await WaitForStatusAsync(generated.ReferenceNumber, KsefTokenStatusRev2Code.NotYetActive);

            QueryKsefTokensResponseRev2 byValidFromInclusive = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                description: marker,
                validFrom: token.ValidFrom.AddMinutes(-1),
                cancellationToken: CancellationToken);
            Assert.Contains(byValidFromInclusive.Tokens, t => t.ReferenceNumber == generated.ReferenceNumber);

            QueryKsefTokensResponseRev2 byValidFromExclusive = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                description: marker,
                validFrom: token.ValidFrom.AddMinutes(1),
                cancellationToken: CancellationToken);
            Assert.DoesNotContain(byValidFromExclusive.Tokens, t => t.ReferenceNumber == generated.ReferenceNumber);

            QueryKsefTokensResponseRev2 byValidToInclusive = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                description: marker,
                validTo: token.ValidTo.AddMinutes(1),
                cancellationToken: CancellationToken);
            Assert.Contains(byValidToInclusive.Tokens, t => t.ReferenceNumber == generated.ReferenceNumber);

            QueryKsefTokensResponseRev2 byValidToExclusive = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                description: marker,
                validTo: token.ValidTo.AddMinutes(-1),
                cancellationToken: CancellationToken);
            Assert.DoesNotContain(byValidToExclusive.Tokens, t => t.ReferenceNumber == generated.ReferenceNumber);

            QueryKsefTokensResponseRev2 byBoth = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                description: marker,
                statuses: [KsefTokenStatusFilterRev2.NotYetActive],
                validFrom: token.ValidFrom.AddMinutes(-1),
                validTo: token.ValidTo.AddMinutes(1),
                cancellationToken: CancellationToken);
            Assert.Contains(byBoth.Tokens, t => t.ReferenceNumber == generated.ReferenceNumber);
            Assert.All(byBoth.Tokens, t => Assert.Equal(KsefTokenStatusRev2Code.NotYetActive, t.Status.Code));
        }
        finally
        {
            await TryRevokeTokenAsync(generated.ReferenceNumber);
        }
    }

    /// <summary>
    /// Walidacja żądania: zbyt krótki opis oraz ValidTo wcześniejsze niż ValidFrom są odrzucane przez API.
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_GenerateWithInvalidRequest_ThrowsKsefApiException()
    {
        KsefApiException shortDescription = await Assert.ThrowsAsync<KsefApiException>(() =>
            KsefClient.GenerateKsefTokenRev2Async(
                new KsefTokenRequestRev2 { Permissions = InvoicePermissions, Description = "abc" },
                OwnerAccessToken,
                CancellationToken));
        Assert.Equal(HttpStatusCode.BadRequest, shortDescription.StatusCode);

        KsefApiException invalidRange = await Assert.ThrowsAsync<KsefApiException>(() =>
            KsefClient.GenerateKsefTokenRev2Async(
                new KsefTokenRequestRev2
                {
                    Permissions = InvoicePermissions,
                    Description = "E2E Rev2 invalid range",
                    ValidFrom = DateTimeOffset.UtcNow.AddDays(5),
                    ValidTo = DateTimeOffset.UtcNow.AddDays(2)
                },
                OwnerAccessToken,
                CancellationToken));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRange.StatusCode);
    }

    /// <summary>
    /// Lista tokenów Rev2 z filtrami: opis, status, autor.
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_Query_FiltersByDescriptionStatusAndAuthor()
    {
        string marker = $"Rev2-{Guid.NewGuid():N}"[..16];

        KsefTokenResponseRev2 rootA = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = $"{marker}-A"
        });
        KsefTokenResponseRev2 rootB = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = $"{marker}-B"
        });

        try
        {
            AuthenticationKsefTokenRev2 rootAStatus = await WaitForStatusAsync(rootA.ReferenceNumber, KsefTokenStatusRev2Code.Active);
            await WaitForStatusAsync(rootB.ReferenceNumber, KsefTokenStatusRev2Code.Active);

            QueryKsefTokensResponseRev2 byDescription = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken, description: marker, cancellationToken: CancellationToken);
            Assert.Contains(byDescription.Tokens, t => t.ReferenceNumber == rootA.ReferenceNumber);
            Assert.Contains(byDescription.Tokens, t => t.ReferenceNumber == rootB.ReferenceNumber);

            QueryKsefTokensResponseRev2 activeOnly = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                statuses: [KsefTokenStatusFilterRev2.Active],
                description: marker,
                cancellationToken: CancellationToken);
            Assert.All(activeOnly.Tokens, t => Assert.Equal(KsefTokenStatusRev2Code.Active, t.Status.Code));

            QueryKsefTokensResponseRev2 byAuthor = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                authorIdentifier: rootAStatus.AuthorIdentifier.Value,
                authorIdentifierType: TokenContextIdentifierType.Nip,
                description: marker,
                cancellationToken: CancellationToken);
            Assert.Contains(byAuthor.Tokens, t => t.ReferenceNumber == rootA.ReferenceNumber);

            QueryKsefTokensResponseRev2 paged = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                description: marker,
                pageSize: 10,
                cancellationToken: CancellationToken);
            Assert.NotEmpty(paged.Tokens);
        }
        finally
        {
            await TryRevokeTokenAsync(rootA.ReferenceNumber);
            await TryRevokeTokenAsync(rootB.ReferenceNumber);
        }
    }

    /// <summary>
    /// Token wygenerowany po uwierzytelnieniu tokenem KSeF trafia do statusu AwaitingApproval:
    /// nie można się nim uwierzytelnić, zatwierdzenie sesją z tokena KSeF jest odrzucane (26005),
    /// a zatwierdzenie inną metodą (OwnerAccessToken) aktywuje token i umożliwia uwierzytelnienie.
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_TokenGeneratedViaKsefToken_RequiresApprovalBeforeAuth()
    {
        string marker = $"Rev2Appr-{Guid.NewGuid():N}"[..17];

        KsefTokenResponseRev2 root = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = $"{marker}-root"
        });

        KsefTokenResponseRev2? child = null;
        try
        {
            await WaitForStatusAsync(root.ReferenceNumber, KsefTokenStatusRev2Code.Active);

            AuthenticationOperationStatusResponse rootAuth = await AuthenticateWithKsefTokenAsync(root.Token, Nip);

            child = await GenerateTokenAsync(
                new KsefTokenRequestRev2
                {
                    Permissions = [KsefTokenPermissionType.InvoiceRead],
                    Description = $"{marker}-child"
                },
                rootAuth.AccessToken.Token);

            AuthenticationKsefTokenRev2 awaitingApproval = await WaitForStatusAsync(
                child.ReferenceNumber,
                KsefTokenStatusRev2Code.AwaitingApproval);
            Assert.Equal("AwaitingApproval", awaitingApproval.Status.RawCode);
            AssertPermissions(awaitingApproval, KsefTokenPermissionCodeRev2.InvoiceRead);

            QueryKsefTokensResponseRev2 filtered = await KsefClient.QueryKsefTokensRev2Async(
                OwnerAccessToken,
                statuses: [KsefTokenStatusFilterRev2.AwaitingApproval],
                description: marker,
                cancellationToken: CancellationToken);
            Assert.Contains(filtered.Tokens, t => t.ReferenceNumber == child.ReferenceNumber);

            await Assert.ThrowsAnyAsync<Exception>(() => AuthenticateWithKsefTokenAsync(child.Token, Nip));

            // Zatwierdzenie sesją uwierzytelnioną tokenem KSeF jest niedozwolone
            KsefApiException approveViaKsefToken = await Assert.ThrowsAsync<KsefApiException>(() =>
                KsefClient.ApproveKsefTokenAsync(child.ReferenceNumber, rootAuth.AccessToken.Token, CancellationToken));
            Assert.Contains("26005", approveViaKsefToken.Message);

            await KsefClient.ApproveKsefTokenAsync(child.ReferenceNumber, OwnerAccessToken, CancellationToken);

            await WaitForStatusAsync(child.ReferenceNumber, KsefTokenStatusRev2Code.Active);

            AuthenticationOperationStatusResponse childAuth = await AuthenticateWithKsefTokenAsync(child.Token, Nip);
            Assert.False(string.IsNullOrWhiteSpace(childAuth.AccessToken?.Token));
        }
        finally
        {
            if (child is not null)
            {
                await TryRevokeTokenAsync(child.ReferenceNumber);
            }

            await TryRevokeTokenAsync(root.ReferenceNumber);
        }
    }

    /// <summary>
    /// Wygaszenie terminu zatwierdzenia (testdata): token w statusie AwaitingApproval przechodzi
    /// w ApprovalExpired, a jego późniejsze zatwierdzenie kończy się błędem 26004.
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_ExpireApproval_MovesTokenToApprovalExpired()
    {
        string marker = $"Rev2Exp-{Guid.NewGuid():N}"[..16];

        KsefTokenResponseRev2 root = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = $"{marker}-root"
        });

        KsefTokenResponseRev2? child = null;
        try
        {
            await WaitForStatusAsync(root.ReferenceNumber, KsefTokenStatusRev2Code.Active);

            AuthenticationOperationStatusResponse rootAuth = await AuthenticateWithKsefTokenAsync(root.Token, Nip);

            child = await GenerateTokenAsync(
                new KsefTokenRequestRev2
                {
                    Permissions = [KsefTokenPermissionType.InvoiceRead],
                    Description = $"{marker}-child"
                },
                rootAuth.AccessToken.Token);

            await WaitForStatusAsync(child.ReferenceNumber, KsefTokenStatusRev2Code.AwaitingApproval);

            await TestDataClient.ExpireTokenApprovalAsync(child.ReferenceNumber, OwnerAccessToken, CancellationToken);

            AuthenticationKsefTokenRev2 expired = await WaitForStatusAsync(child.ReferenceNumber, KsefTokenStatusRev2Code.ApprovalExpired);
            Assert.Equal("ApprovalExpired", expired.Status.RawCode);

            KsefApiException approveExpired = await Assert.ThrowsAsync<KsefApiException>(() =>
                KsefClient.ApproveKsefTokenAsync(child.ReferenceNumber, OwnerAccessToken, CancellationToken));
            Assert.Contains("26004", approveExpired.Message);
        }
        finally
        {
            if (child is not null)
            {
                await TryRevokeTokenAsync(child.ReferenceNumber);
            }

            await TryRevokeTokenAsync(root.ReferenceNumber);
        }
    }

    /// <summary>
    /// Generowanie przez token KSeF: potomny token przechodzi w AwaitingApproval (bez możliwości uwierzytelnienia),
    /// a próba nadania uprawnień, których wywołujący nie posiada, kończy się błędem 26001.
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_GenerateViaKsefToken_EnforcesApprovalAndPermissionRules()
    {
        KsefTokenResponseRev2 root = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = "E2E Rev2 generation rules root"
        });

        KsefTokenResponseRev2? child = null;
        try
        {
            await WaitForStatusAsync(root.ReferenceNumber, KsefTokenStatusRev2Code.Active);
            AuthenticationOperationStatusResponse rootAuth = await AuthenticateWithKsefTokenAsync(root.Token, Nip);

            child = await GenerateTokenAsync(
                new KsefTokenRequestRev2
                {
                    Permissions = [KsefTokenPermissionType.InvoiceRead],
                    Description = "E2E Rev2 generation rules child"
                },
                rootAuth.AccessToken.Token);

            AuthenticationKsefTokenRev2 childStatus = await WaitForStatusAsync(
                child.ReferenceNumber,
                KsefTokenStatusRev2Code.AwaitingApproval);
            Assert.Equal(KsefTokenStatusRev2Code.AwaitingApproval, childStatus.Status.Code);
            Assert.Equal("AwaitingApproval", childStatus.Status.RawCode);
            AssertPermissions(childStatus, KsefTokenPermissionCodeRev2.InvoiceRead);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                AuthenticateWithKsefTokenAsync(child.Token, Nip));

            KsefApiException tooManyPermissions = await Assert.ThrowsAsync<KsefApiException>(() =>
                KsefClient.GenerateKsefTokenRev2Async(
                    new KsefTokenRequestRev2
                    {
                        Permissions = [KsefTokenPermissionType.InvoiceRead, KsefTokenPermissionType.CredentialsManage],
                        Description = "E2E Rev2 too many permissions"
                    },
                    rootAuth.AccessToken.Token,
                    CancellationToken));
            Assert.Contains("26001", tooManyPermissions.Message);
        }
        finally
        {
            if (child is not null)
            {
                await TryRevokeTokenAsync(child.ReferenceNumber);
            }

            await TryRevokeTokenAsync(root.ReferenceNumber);
        }
    }

    /// <summary>
    /// Token uwierzytelniony tokenem KSeF (bez CredentialsManage / CredentialsRead) widzi na liście wyłącznie token użyty do uwierzytelnienia.
    /// </summary>
    [Fact]
    public async Task KsefTokenRev2_QuerySelf_WithoutCredentialsPermissions_ReturnsOnlyOwnToken()
    {
        string marker = $"Rev2-{Guid.NewGuid():N}"[..16];

        KsefTokenResponseRev2 first = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = $"{marker} first"
        });
        KsefTokenResponseRev2 second = await GenerateTokenAsync(new KsefTokenRequestRev2
        {
            Permissions = InvoicePermissions,
            Description = $"{marker} second"
        });

        try
        {
            await WaitForStatusAsync(first.ReferenceNumber, KsefTokenStatusRev2Code.Active);
            await WaitForStatusAsync(second.ReferenceNumber, KsefTokenStatusRev2Code.Active);

            AuthenticationOperationStatusResponse firstAuth = await AuthenticateWithKsefTokenAsync(first.Token, Nip);

            QueryKsefTokensResponseRev2 selfList = await KsefClient.QueryKsefTokensRev2Async(
                firstAuth.AccessToken.Token, cancellationToken: CancellationToken);

            AuthenticationKsefTokenRev2 own = Assert.Single(selfList.Tokens);
            Assert.Equal(first.ReferenceNumber, own.ReferenceNumber);
        }
        finally
        {
            await TryRevokeTokenAsync(first.ReferenceNumber);
            await TryRevokeTokenAsync(second.ReferenceNumber);
        }
    }

    private async Task<KsefTokenResponseRev2> GenerateTokenAsync(KsefTokenRequestRev2 request, string? accessToken = null)
    {
        KsefTokenResponseRev2 response = await KsefClient.GenerateKsefTokenRev2Async(
            request,
            accessToken ?? OwnerAccessToken,
            CancellationToken).ConfigureAwait(false);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        return response;
    }

    private Task<AuthenticationKsefTokenRev2> WaitForStatusAsync(string referenceNumber, KsefTokenStatusRev2Code expectedStatus)
    {
        int attempts = Math.Max(1, (TokenStatusTimeoutSeconds * 1000) / SleepTime);

        return AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetKsefTokenRev2Async(referenceNumber, OwnerAccessToken, CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status.Code == expectedStatus,
            description: $"Token {referenceNumber} -> {expectedStatus}",
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: attempts,
            rateLimitOnException: ex => ex is KsefRateLimitException rateLimit
                ? new AsyncPollingUtils.RateLimitDecision(true, rateLimit.RecommendedDelay)
                : new AsyncPollingUtils.RateLimitDecision(false),
            cancellationToken: CancellationToken);
    }

    private static void AssertPermissions(AuthenticationKsefTokenRev2 token, params KsefTokenPermissionCodeRev2[] expected)
    {
        Assert.NotNull(token.RequestedPermissions);
        Assert.Equal(
            expected.OrderBy(p => p),
            token.RequestedPermissions.Select(p => p.Code).OrderBy(p => p));
        Assert.All(token.RequestedPermissions, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.RawCode));
            Assert.False(string.IsNullOrWhiteSpace(p.DisplayName));
        });
    }

    private async Task TryRevokeTokenAsync(string referenceNumber)
    {
        try
        {
            await KsefClient.RevokeKsefTokenAsync(referenceNumber, OwnerAccessToken, CancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Sprzątanie best-effort - nie maskuje wyniku testu.
        }
    }

    /// <summary>
    /// Przeprowadza pełny proces uwierzytelnienia przy użyciu tokena KSeF (RSA-OAEP SHA-256 na ciągu "token|timestampMs").
    /// </summary>
    private async Task<AuthenticationOperationStatusResponse> AuthenticateWithKsefTokenAsync(string ksefToken, string nip)
    {
        AuthenticationChallengeResponse challenge = await AuthorizationClient.GetAuthChallengeAsync(CancellationToken).ConfigureAwait(false);
        long timestampMs = challenge.Timestamp.ToUnixTimeMilliseconds();

        byte[] tokenBytes = System.Text.Encoding.UTF8.GetBytes($"{ksefToken}|{timestampMs}");
        byte[] encrypted = CryptographyService.EncryptKsefTokenWithRSAUsingPublicKey(tokenBytes);

        AuthenticationKsefTokenRequest request = new()
        {
            Challenge = challenge.Challenge,
            ContextIdentifier = new AuthenticationTokenContextIdentifier
            {
                Type = AuthenticationTokenContextIdentifierType.Nip,
                Value = nip
            },
            EncryptedToken = Convert.ToBase64String(encrypted),
            AuthorizationPolicy = null
        };

        SignatureResponse signature = await AuthorizationClient.SubmitKsefTokenAuthRequestAsync(request, CancellationToken).ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(signature.ReferenceNumber));
        Assert.False(string.IsNullOrWhiteSpace(signature.AuthenticationToken?.Token));

        int statusAttempts = Math.Max(1, (int)(TimeSpan.FromMinutes(2).TotalMilliseconds / SleepTime));
        AuthStatus status = await AsyncPollingUtils.PollAsync(
            async () => await AuthorizationClient.GetAuthStatusAsync(signature.ReferenceNumber, signature.AuthenticationToken.Token, CancellationToken).ConfigureAwait(false),
            result => result is not null && result.Status?.Code == SuccessfulAuthStatusCode,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: statusAttempts,
            rateLimitOnException: ex => ex is KsefRateLimitException rateLimit
                ? new AsyncPollingUtils.RateLimitDecision(true, rateLimit.RecommendedDelay)
                : new AsyncPollingUtils.RateLimitDecision(false),
            cancellationToken: CancellationToken).ConfigureAwait(false);

        Assert.Equal(SuccessfulAuthStatusCode, status.Status.Code);

        AuthenticationOperationStatusResponse tokens = await AuthorizationClient.GetAccessTokenAsync(signature.AuthenticationToken.Token, CancellationToken).ConfigureAwait(false);
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken?.Token));
        return tokens;
    }
}
