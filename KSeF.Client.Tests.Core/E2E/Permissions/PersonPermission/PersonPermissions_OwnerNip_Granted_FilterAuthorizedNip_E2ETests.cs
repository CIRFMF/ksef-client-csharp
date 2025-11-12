using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

public class PersonPermissions_OwnerNip_Granted_FilterAuthorizedNip_E2ETests : TestBase
{

    /// <summary>
    /// E2E: „Nadane uprawnienia” (właściciel, kontekst NIP) z filtrowaniem po NIP uprawnionego.
    /// Przebieg: utwórz podmiot i osobę → nadaj uprawnienie → uwierzytelnij właściciela → zapytaj o nadane (z filtrem NIP) → asercje → odbierz uprawnienie → porządki.
    /// </summary>
    /// <remarks>
    /// <list type="number">
    /// <item><description>Seed: Subject (NIP właściciela) + Person (NIP+PESEL) przez testdata.</description></item>
    /// <item><description>GRANT (real API persons) dla NIP uprawnionego → poll (200).</description></item>
    /// <item><description>QUERY: nadane w bieżącym kontekście + filtr NIP uprawnionego.</description></item>
    /// <item><description>ASSERT: dopasowanie po AuthorizedIdentifier=NIP oraz AuthorIdentifier=NIP właściciela.</description></item>
    /// <item><description>Cleanup: REVOKE (real API) + remove person/subject (testdata).</description></item>
    /// </list>
    /// </remarks>
    [Fact]
    public async Task Search_Granted_AsOwnerNip_FilterByAuthorizedNip_ShouldReturnPageWithMatch()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string authorizedNip = MiscellaneousUtils.GetRandomNip();
        string authorizedPesel = MiscellaneousUtils.GetRandomPesel();

        // Subject (kontekst właściciela) – testdata setup
        await TestDataClient.CreateSubjectAsync(new SubjectCreateRequest
        {
            SubjectNip = ownerNip,
            Description = $"E2E-Subject-{ownerNip}"
        }, CancellationToken);

        // Osoba uprawniona – testdata setup (NIP + PESEL, żeby grant był możliwy deterministycznie)
        await TestDataClient.CreatePersonAsync(new PersonCreateRequest
        {
            Nip = authorizedNip,
            Pesel = authorizedPesel,
            IsBailiff = false,
            Description = $"E2E-Person-{authorizedNip}"
        }, CancellationToken);

        // Auth po stronie właściciela (kontekst NIP)
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        // Grant (REAL API): persons/grants – uprawnienie InvoiceRead dla osoby identyfikowanej NIP
        GrantPermissionsPersonSubjectIdentifier subject = new GrantPermissionsPersonSubjectIdentifier
        {
            Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
            Value = authorizedNip
        };

        GrantPermissionsPersonRequest grantRequest =
            GrantPersonPermissionsRequestBuilder
                .Create()
                .WithSubject(subject)
                .WithPermissions(PersonPermissionType.InvoiceRead)
                .WithDescription($"E2E-Grant-Read-{authorizedNip}")
                .Build();

        OperationResponse grantOp =
            await KsefClient.GrantsPermissionPersonAsync(grantRequest, ownerAccessToken, CancellationToken);

        // Poll na status 200
        PermissionsOperationStatusResponse grantStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(grantOp.ReferenceNumber, ownerAccessToken),
                result => result is not null && result.Status is not null && result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Czekam aż pojawi się wpis (Nadane/NIP uprawnionego)",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // Query: granted w bieżącym kontekście + filtr po NIP uprawnionego
        PersonPermissionsQueryRequest request = new PersonPermissionsQueryRequest
        {
            ContextIdentifier = new PersonPermissionsContextIdentifier
            {
                Type = PersonPermissionsContextIdentifierType.Nip,
                Value = ownerNip
            },
            TargetIdentifier = new PersonPermissionsTargetIdentifier
            {
                Type = PersonPermissionsTargetIdentifierType.Nip,
                Value = ownerNip
            },
            AuthorizedIdentifier = new PersonPermissionsAuthorizedIdentifier
            {
                Type = PersonAuthorizedIdentifierType.Nip,
                Value = authorizedNip
            },
            PermissionState = PersonPermissionState.Active,
            QueryType = PersonQueryType.PermissionsGrantedInCurrentContext
        };
        #endregion

        #region Act
        // Poll na pojawienie się grantu w wynikach
        PagedPermissionsResponse<PersonPermission> page =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.SearchGrantedPersonPermissionsAsync(
                    request, ownerAccessToken, pageOffset: 0, pageSize: 50, cancellationToken: CancellationToken),
                result => result is not null && result.Permissions is not null && result.Permissions.Count > 0,
                description: "Wait until granted person permissions (filter by Authorized NIP) are available",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        PersonPermission? matching = page.Permissions.FirstOrDefault(p =>
            p is not null
            && p.AuthorizedIdentifier is not null
            && p.AuthorIdentifier is not null
            && p.AuthorizedIdentifier.Type == PersonPermissionAuthorizedIdentifierType.Nip
            && string.Equals(p.AuthorizedIdentifier.Value, authorizedNip, StringComparison.Ordinal)
            && string.Equals(p.AuthorIdentifier.Value, ownerNip, StringComparison.Ordinal)       
            && string.Equals(p.PermissionScope,Enum.GetName(PersonPermissionType.InvoiceRead), StringComparison.Ordinal));
        #endregion

        #region Assert
        Assert.NotNull(page);
        Assert.NotNull(page.Permissions);
        Assert.NotNull(matching);
        #endregion

        #region Cleanup
        // Revoke (REAL API): common/grants/{id}
        OperationResponse revokeOp =
            await KsefClient.RevokeCommonPermissionAsync(matching.Id, ownerAccessToken, CancellationToken);

        PermissionsOperationStatusResponse revokeStatus =
            await AsyncPollingUtils.PollAsync(
                async () => await KsefClient.OperationsStatusAsync(revokeOp.ReferenceNumber, ownerAccessToken),
                result => result is not null && result.Status is not null && result.Status.Code == OperationStatusCodeResponse.Success,
                description: "Wait for PERSON revoke 200",
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 60,
                cancellationToken: CancellationToken);

        // testdata cleanup
        await TestDataClient.RemovePersonAsync(new PersonRemoveRequest
        {
            Nip = authorizedNip
        }, CancellationToken);

        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest
        {
            SubjectNip = ownerNip
        }, CancellationToken);
        #endregion
    }
}
