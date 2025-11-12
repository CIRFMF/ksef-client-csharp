using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Api.Builders.IndirectEntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;

/// <summary>
/// Testy dla selektywnego nadawania uprawnień pośrednich w systemie KSeF.
/// Weryfikacja, że uprawnienia nadane selektywnie działają wyłącznie w kontekście wskazanych partnerów.
/// </summary>
[CollectionDefinition("IndirectSelectivePermissionGrant.feature")]
[Trait("Category", "Features")]
[Trait("Features", "IndirectSelectivePermissionGrant.feature")]
public class IndirectSelectivePermissionGrantTests : KsefIntegrationTestBase
{
    private string _firstOwnerAccessToken { get; set; }
    private string _firstOwnerNip { get; set; }

    private string _secondOwnerAccessToken { get; set; }
    private string _secondOwnerNip { get; set; }

    private string _intermediaryAccessToken { get; set; }
    private string _intermediaryNip { get; set; }

    private IndirectEntitySubjectIdentifier _subjectWithPesel { get; } =
        new IndirectEntitySubjectIdentifier
        {
            Type = IndirectEntitySubjectIdentifierType.Pesel
        };

    private IndirectEntitySubjectIdentifier _subjectWithNip { get; } =
        new IndirectEntitySubjectIdentifier
        {
            Type = IndirectEntitySubjectIdentifierType.Nip
        };

    public IndirectSelectivePermissionGrantTests()
    {
        _firstOwnerNip = MiscellaneousUtils.GetRandomNip();
        _secondOwnerNip = MiscellaneousUtils.GetRandomNip();
        _intermediaryNip = MiscellaneousUtils.GetRandomNip();
        _subjectWithPesel.Value = MiscellaneousUtils.GetRandomPesel();
        _subjectWithNip.Value = MiscellaneousUtils.GetRandomNip();
    }

    /// <summary>
    /// Scenariusz kompletnej obsługi selektywnych uprawnień pośrednich E2E:
    /// 1. Nadanie uprawnień z delegacją dla pośrednika przez dwóch właścicieli
    /// 2. Nadanie selektywnych uprawnień pośrednich (PESEL → firstOwner, NIP → secondOwner)
    /// 3. Weryfikacja dostępu w odpowiednich kontekstach oraz braku dostępu w pozostałych
    /// </summary>
    [Fact]
    public async Task SelectiveIndirectPermission_GrantAndVerifySelectiveAccess()
    {
        // Arrange: uwierzytelnienie pierwszego właściciela
        _firstOwnerAccessToken = (await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            _firstOwnerNip)).AccessToken.Token;

        // Arrange: uwierzytelnienie drugiego właściciela
        _secondOwnerAccessToken = (await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            _secondOwnerNip)).AccessToken.Token;

        // Act: 1) Nadanie uprawnień pośrednich przez właścicieli dla pośrednika
        // Act: nadanie uprawnień dla pośrednika przez pierwszego właściciela
        OperationResponse firstOwnerGrantResponse =
            await GrantPermissionsWithDelegationToIntermediaryAsync(_firstOwnerAccessToken);

        Assert.NotNull(firstOwnerGrantResponse);
        Assert.False(string.IsNullOrEmpty(firstOwnerGrantResponse.ReferenceNumber),
            "Numer referencyjny powinien zostać wygenerowany");

        PermissionsOperationStatusResponse firstOwnerGrantStatus =
            await WaitForOperationSuccessAsync(firstOwnerGrantResponse.ReferenceNumber, _firstOwnerAccessToken);

        Assert.NotNull(firstOwnerGrantStatus);
        Assert.Equal(OperationStatusCodeResponse.Success, firstOwnerGrantStatus.Status.Code);

        // Act: nadanie uprawnień dla pośrednika przez drugiego właściciela
        OperationResponse secondOwnerGrantResponse =
            await GrantPermissionsWithDelegationToIntermediaryAsync(_secondOwnerAccessToken);

        Assert.NotNull(secondOwnerGrantResponse);
        Assert.False(string.IsNullOrEmpty(secondOwnerGrantResponse.ReferenceNumber),
            "Numer referencyjny powinien zostać wygenerowany");

        PermissionsOperationStatusResponse secondOwnerGrantStatus =
            await WaitForOperationSuccessAsync(secondOwnerGrantResponse.ReferenceNumber, _secondOwnerAccessToken);

        Assert.NotNull(secondOwnerGrantStatus);
        Assert.Equal(OperationStatusCodeResponse.Success, secondOwnerGrantStatus.Status.Code);

        // Arrange: Uwierzytelnienie pośrednika
        _intermediaryAccessToken = (await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            _intermediaryNip)).AccessToken.Token;

        // Act: 2) Nadanie SELEKTYWNYCH uprawnień pośrednich
        // Act: nadanie selektywnych uprawnień pośrednich dla PESEL w kontekście firstOwner
        OperationResponse peselGrantResponse =
            await GrantSelectiveIndirectPermissionsAsync(_subjectWithPesel, _firstOwnerNip);

        Assert.NotNull(peselGrantResponse);
        Assert.False(string.IsNullOrEmpty(peselGrantResponse.ReferenceNumber),
            "Numer referencyjny powinien zostać wygenerowany");

        PermissionsOperationStatusResponse peselGrantStatus =
            await WaitForOperationSuccessAsync(peselGrantResponse.ReferenceNumber, _intermediaryAccessToken);

        Assert.NotNull(peselGrantStatus);
        Assert.Equal(OperationStatusCodeResponse.Success, peselGrantStatus.Status.Code);

        // Act: nadanie selektywnych uprawnień pośrednich dla NIP w kontekście secondOwner
        OperationResponse nipGrantResponse =
            await GrantSelectiveIndirectPermissionsAsync(_subjectWithNip, _secondOwnerNip);

        Assert.NotNull(nipGrantResponse);
        Assert.False(string.IsNullOrEmpty(nipGrantResponse.ReferenceNumber),
            "Numer referencyjny powinien zostać wygenerowany");

        PermissionsOperationStatusResponse nipGrantStatus =
            await WaitForOperationSuccessAsync(nipGrantResponse.ReferenceNumber, _intermediaryAccessToken);

        Assert.NotNull(nipGrantStatus);
        Assert.Equal(OperationStatusCodeResponse.Success, nipGrantStatus.Status.Code);

        // Act: uwierzytelnienie PESEL w kontekście firstOwner
        AuthenticationOperationStatusResponse peselAuthInFirstContext =
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                _subjectWithPesel.Value,
                _firstOwnerNip);

        PersonToken peselTokenInFirstContext = TokenService.MapFromJwt(peselAuthInFirstContext.AccessToken.Token);

        // Act: 3) Weryfikacja nadanych uprawnień w odpowiednich kontekstach
        // Assert: weryfikacja uprawnień PESEL w kontekście firstOwner
        Assert.True(
            peselTokenInFirstContext.Permissions.Contains(
                EntityStandardPermissionType.InvoiceRead.ToString()),
            "Token powinien zawierać uprawnienie InvoiceRead");

        Assert.True(
            peselTokenInFirstContext.Permissions.Contains(
                EntityStandardPermissionType.InvoiceWrite.ToString()),
            "Token powinien zawierać uprawnienie InvoiceWrite");

        // Act & Assert: próba uwierzytelnienia PESEL w kontekście secondOwner - oczekiwany błąd
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                _subjectWithPesel.Value,
                _secondOwnerNip);
        });

        // Act: uwierzytelnienie NIP w kontekście secondOwner
        AuthenticationOperationStatusResponse nipInSecondContext = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            _subjectWithNip.Value,
            _secondOwnerNip);

        PersonToken nipTokenInSecondContext = TokenService.MapFromJwt(nipInSecondContext.AccessToken.Token);

        // Assert: weryfikacja uprawnień NIP w kontekście secondOwner
        Assert.True(
            nipTokenInSecondContext.Permissions.Contains(
                EntityStandardPermissionType.InvoiceRead.ToString()),
            "Token powinien zawierać uprawnienie InvoiceRead");

        Assert.True(
            nipTokenInSecondContext.Permissions.Contains(
                EntityStandardPermissionType.InvoiceWrite.ToString()),
            "Token powinien zawierać uprawnienie InvoiceWrite");

        // Act & Assert: próba uwierzytelnienia NIP w kontekście firstOwner - oczekiwany błąd
        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                _subjectWithNip.Value,
                _firstOwnerNip);
        });
    }

    /// <summary>
    /// Nadanie uprawnień InvoiceRead i InvoiceWrite z możliwością delegacji dla pośrednika.
    /// </summary>
    /// <param name="ownerAccessToken">Token dostępu właściciela nadającego uprawnienia</param>
    /// <returns>Odpowiedź operacji zawierająca numer referencyjny do śledzenia statusu</returns>
    private async Task<OperationResponse> GrantPermissionsWithDelegationToIntermediaryAsync(
        string ownerAccessToken)
    {
        GrantPermissionsEntityRequest request = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(
                new GrantPermissionsEntitySubjectIdentifier
                {
                    Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                    Value = _intermediaryNip
                }
            )
            .WithPermissions(
                EntityPermission.New(EntityStandardPermissionType.InvoiceRead, canDelegate: true),
                EntityPermission.New(EntityStandardPermissionType.InvoiceWrite, canDelegate: true)
            )
            .WithDescription("E2E test - nadanie uprawnień z delegacją dla pośrednika")
            .Build();

        return await KsefClient.GrantsPermissionEntityAsync(request, ownerAccessToken, CancellationToken.None);
    }

    /// <summary>
    /// Nadanie selektywnych uprawnień pośrednich w kontekście konkretnego partnera.
    /// </summary>
    /// <param name="subject">Identyfikator podmiotu otrzymującego uprawnienia (NIP lub PESEL)</param>
    /// <param name="targetPartnerNip">NIP partnera określający selektywny kontekst dostępu</param>
    /// <returns>Odpowiedź operacji zawierająca numer referencyjny do śledzenia statusu</returns>
    private async Task<OperationResponse> GrantSelectiveIndirectPermissionsAsync(
        IndirectEntitySubjectIdentifier subject,
        string targetPartnerNip)
    {
        GrantPermissionsIndirectEntityRequest request = GrantIndirectEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(subject)
            .WithContext(
                new IndirectEntityTargetIdentifier
                {
                    Type = IndirectEntityTargetIdentifierType.Nip,
                    Value = targetPartnerNip
                }
            )
            .WithPermissions(
                IndirectEntityStandardPermissionType.InvoiceRead,
                IndirectEntityStandardPermissionType.InvoiceWrite
            )
            .WithDescription($"E2E test - selektywne przekazanie uprawnień dla partnera {targetPartnerNip}")
            .Build();

        return await KsefClient.GrantsPermissionIndirectEntityAsync(request, _intermediaryAccessToken, CancellationToken.None);
    }

    /// <summary>
    /// Oczekiwanie na pomyślne zakończenie operacji z wykorzystaniem pollingu.
    /// </summary>
    /// <param name="operationReferenceNumber">Numer referencyjny operacji do monitorowania</param>
    /// <param name="accessToken">Token dostępu używany do autoryzacji zapytań o status operacji</param>
    /// <returns>Status operacji po osiągnięciu kodu sukcesu 200</returns>
    private Task<PermissionsOperationStatusResponse> WaitForOperationSuccessAsync(
        string operationReferenceNumber,
        string accessToken)
        => AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(operationReferenceNumber, accessToken),
            condition: r => r.Status.Code == OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken.None
        );
}