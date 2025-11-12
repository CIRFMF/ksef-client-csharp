using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

/// <summary>
/// Nadawanie uprawnień do wykonywania operacji komorniczych w kontekście, który na to NIE zezwala.
/// Scenariusz:
/// 1) Uwierzytelnienie w zwykłym kontekście NIP (nie EnforcementAuthority lub CourtBailiff)
/// 2) Próba nadania osobie uprawnienia EnforcementOperations
/// 3) Weryfikacja, że status operacji != 200 (odrzucone)
/// 4) Weryfikacja, że uprawnienie nie pojawiło się w wynikach wyszukiwania
/// </summary>
public class EnforcementOperationsNegativeE2ETests : TestBase
{
    private const string PermissionDescription = "E2E negative grant EnforcementOperations";

    [Fact]
    public async Task GrantEnforcementOperations_InNotAllowedContext_E2E_FailsAndNotVisible()
    {
        // Arrange: zwykły kontekst NIP (brak roli EnforcementAuthority lub CourtBailiff)
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string granteeNip = MiscellaneousUtils.GetRandomNip();

        AuthenticationOperationStatusResponse authorizationInfo = await AuthenticationUtils
            .AuthenticateAsync(AuthorizationClient, SignatureService, ownerNip);

        string accessToken = authorizationInfo.AccessToken.Token;

        // Act: próba nadania uprawnienia EnforcementOperations
        GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
            .Create()
            .WithSubject(new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Nip,
                Value = granteeNip
            })
            .WithPermissions(PersonPermissionType.EnforcementOperations)
            .WithDescription(PermissionDescription)
            .Build();

        OperationResponse grantResponse = await KsefClient.GrantsPermissionPersonAsync(request, accessToken, CancellationToken);

        // Assert: otrzymanie numeru referencyjnego operacji
        Assert.NotNull(grantResponse);
        Assert.False(string.IsNullOrEmpty(grantResponse.ReferenceNumber));

        // Odczytywanie statusu operacji aż będzie różny od 200 (niepowodzenie)
        PermissionsOperationStatusResponse grantStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantResponse.ReferenceNumber, accessToken),
            condition: s => s is not null && s.Status is not null && s.Status.Code != OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromMilliseconds(SleepTime),
            maxAttempts: 60,
            cancellationToken: CancellationToken);

        Assert.NotNull(grantStatus);
        Assert.NotNull(grantStatus.Status);
        Assert.NotEqual(OperationStatusCodeResponse.Success, grantStatus.Status.Code);

        // Potwierdzenie, że uprawnienie nie zostało nadane (nie występuje w wyszukiwaniu)
        PersonPermissionsQueryRequest query = new PersonPermissionsQueryRequest
        {
            PermissionTypes = new List<PersonPermissionType>
            {
                PersonPermissionType.EnforcementOperations
            }
        };

        // Potwierdzenie braku wpisów z opisem
        await Task.Delay(SleepTime);
        PagedPermissionsResponse<PersonPermission> search = await KsefClient.SearchGrantedPersonPermissionsAsync(
            query, accessToken, pageOffset: 0, pageSize: 10, CancellationToken);

        Assert.True(search?.Permissions is null ||
                    !search.Permissions.Any(p => p.Description == PermissionDescription));
    }
}
