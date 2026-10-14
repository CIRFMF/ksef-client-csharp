using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Api.Builders.SubEntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Tests.Utils;
using CoreEntityPermission = KSeF.Client.Core.Models.Permissions.Entity.EntityPermission;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermission;

public class PersonalPermissionsWithInternalIdFilterE2ETests : TestBase
{
    private const int ExpectedPermissionsCount = 2;

    /// <summary>
    /// Scenariusz E2E weryfikujący:
    /// - poprawkę w API zwracającą w liście uprawnień (POST /v2/permissions/query/personal/grants)
    ///   również uprawnienia podmiotowe InvoiceRead/InvoiceWrite nadane z canDelegate=false,
    /// - filtrowanie wyników po InternalId,
    /// - dopuszczalną długość identyfikatora InternalId (16 znaków).
    ///
    /// Kroki:
    /// 1. Utworzenie podmiotu głównego (VatGroup) z jednostką podrzędną
    /// 2. Nadanie dyrektorowi (PESEL) uprawnień administracyjnych do jednostki podrzędnej w kontekście InternalId
    /// 3. Uwierzytelnienie dyrektora w kontekście InternalId
    /// 4. Nadanie dyrektorowi uprawnień osobistych InvoiceRead/InvoiceWrite w kontekście InternalId
    /// 5. Nadanie w tym samym kontekście uprawnień podmiotowych InvoiceRead/InvoiceWrite z canDelegate=false
    /// 6. Weryfikacja listy uprawnień bez filtra (powinny zawierać InvoiceRead/InvoiceWrite z CanDelegate=false)
    /// 7. Weryfikacja listy uprawnień z filtrem InternalId (powinny zawierać InvoiceRead/InvoiceWrite z CanDelegate=false)
    /// 8. Sprzątanie danych testowych
    /// </summary>
    [Fact]
    public async Task PersonalPermissionsWithInternalIdFilterShouldReturnPermissions()
    {
        // Przygotowanie danych
        string municipalOfficeNip = MiscellaneousUtils.GetRandomNip();
        string subunitNip = MiscellaneousUtils.GetRandomNip();
        string kindergartenId = MiscellaneousUtils.GenerateInternalIdentifier($"{municipalOfficeNip}");
        string directorPesel = MiscellaneousUtils.GetRandomPesel();

        // Weryfikacja kontraktu: InternalId ma długość 16 (zmiana maxLength z 10 na 16)
        Assert.Equal(16, kindergartenId.Length);

        // Utworzenie podmiotu głównego z jednostką podrzędną
        SubjectCreateRequest createSubjectRequest = new()
        {
            SubjectNip = municipalOfficeNip,
            SubjectType = SubjectType.VatGroup,
            Subunits =
            [
                new SubjectSubunit
                {
                    SubjectNip = subunitNip,
                    Description = "Przedszkole testowe"
                }
            ],
            Description = "Gmina testowa"
        };

        await TestDataClient.CreateSubjectAsync(createSubjectRequest);

        // 1) Uwierzytelnienie jako podmiot główny (właściciel kontekstu)
        AuthenticationOperationStatusResponse municipalOfficeAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, municipalOfficeNip);

        string municipalOfficeAuthToken = municipalOfficeAuth.AccessToken.Token;

        // 2) Nadanie dyrektorowi uprawnień administracyjnych do jednostki podrzędnej w kontekście InternalId
        GrantPermissionsSubunitRequest grantSubUnitRequest = GrantSubunitPermissionsRequestBuilder
            .Create()
            .WithSubject(new SubunitSubjectIdentifier
            {
                Type = SubUnitSubjectIdentifierType.Pesel,
                Value = directorPesel
            })
            .WithContext(new SubunitContextIdentifier
            {
                Type = SubunitContextIdentifierType.InternalId,
                Value = kindergartenId
            })
            .WithSubunitName("Przedszkole Testowe")
            .WithDescription("Sub-unit permission grant")
            .WithSubjectDetails(new SubunitSubjectDetails
            {
                SubjectDetailsType = PermissionsSubunitSubjectDetailsType.PersonByIdentifier,
                PersonById = new PermissionsSubunitPersonByIdentifier { FirstName = "Jan", LastName = "Kowalski" }
            })
            .Build();

        OperationResponse grantSubunitOperation = await KsefClient.GrantsPermissionSubUnitAsync(
            grantSubUnitRequest,
            municipalOfficeAuthToken,
            CancellationToken);

        Assert.NotNull(grantSubunitOperation);
        Assert.False(string.IsNullOrWhiteSpace(grantSubunitOperation.ReferenceNumber));

        PermissionsOperationStatusResponse grantSubunitStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantSubunitOperation.ReferenceNumber, municipalOfficeAuthToken),
            condition: status => status?.Status?.Code == OperationStatusCodeResponse.Success,
            cancellationToken: CancellationToken);

        Assert.Equal(OperationStatusCodeResponse.Success, grantSubunitStatus.Status.Code);

        // 3) Uwierzytelnienie dyrektora w kontekście InternalId
        AuthenticationOperationStatusResponse kindergartenAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            directorPesel,
            kindergartenId,
            AuthenticationTokenContextIdentifierType.InternalId);

        string kindergartenAuthToken = kindergartenAuth.AccessToken.Token;

        // 4) Nadanie uprawnień osobistych InvoiceRead/InvoiceWrite w kontekście InternalId
        GrantPermissionsPersonSubjectIdentifier directorSubject = new()
        {
            Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
            Value = directorPesel
        };

        PersonPermissionSubjectDetails subjectDetails = new()
        {
            SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
            PersonById = new PersonPermissionPersonById
            {
                FirstName = "Jan",
                LastName = "Dyrektor"
            }
        };

        OperationResponse grantPersonResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
            KsefClient,
            kindergartenAuthToken,
            directorSubject,
            [PersonPermissionType.InvoiceRead, PersonPermissionType.InvoiceWrite],
            subjectDetails,
            "Nadanie InvoiceRead i InvoiceWrite dyrektorowi w kontekście InternalId");

        Assert.NotNull(grantPersonResponse);
        Assert.False(string.IsNullOrWhiteSpace(grantPersonResponse.ReferenceNumber));

        PermissionsOperationStatusResponse grantPersonStatus = await AsyncPollingUtils.PollAsync(
            action: () => PermissionsUtils.GetPermissionsOperationStatusAsync(
                KsefClient,
                grantPersonResponse.ReferenceNumber,
                kindergartenAuthToken),
            condition: status => status is not null
                && status.Status is not null
                && status.Status.Code == OperationStatusCodeResponse.Success,
            cancellationToken: CancellationToken);

        Assert.Equal(OperationStatusCodeResponse.Success, grantPersonStatus.Status.Code);

        // 5) Nadanie uprawnień podmiotowych (entity) InvoiceRead/InvoiceWrite z canDelegate=false
        GrantPermissionsEntitySubjectIdentifier entitySubject = new()
        {
            Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
            Value = municipalOfficeNip
        };

        GrantPermissionsEntityRequest grantEntityPermissionsRequest = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(entitySubject)
            .WithPermissions(
                CoreEntityPermission.New(EntityStandardPermissionType.InvoiceRead, canDelegate: false),
                CoreEntityPermission.New(EntityStandardPermissionType.InvoiceWrite, canDelegate: false))
            .WithDescription("Uprawnienia podmiotowe InvoiceRead/InvoiceWrite bez delegowania")
            .WithSubjectDetails(new PermissionsEntitySubjectDetails { FullName = $"Podmiot {municipalOfficeNip}" })
            .Build();

        OperationResponse grantEntityOperation = await KsefClient.GrantsPermissionEntityAsync(
            grantEntityPermissionsRequest,
            kindergartenAuthToken,
            CancellationToken);

        Assert.NotNull(grantEntityOperation);
        Assert.False(string.IsNullOrWhiteSpace(grantEntityOperation.ReferenceNumber));

        PermissionsOperationStatusResponse grantEntityStatus = await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(grantEntityOperation.ReferenceNumber, kindergartenAuthToken),
            condition: status => status?.Status?.Code == OperationStatusCodeResponse.Success,
            cancellationToken: CancellationToken);

        Assert.Equal(OperationStatusCodeResponse.Success, grantEntityStatus.Status.Code);

        // 6) Weryfikacja listy uprawnień bez filtra
        PersonalPermissionsQueryRequest queryWithoutFilter = new();

        PersonalPermissionsQueryResponse personalPermissions =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryWithoutFilter,
                    kindergartenAuthToken),
                condition: r => r is not null
                    && r.Permissions is not null
                    && r.Permissions.Count >= ExpectedPermissionsCount,
                cancellationToken: CancellationToken);

        Assert.NotNull(personalPermissions);
        Assert.NotEmpty(personalPermissions.Permissions);

        // Uprawnienia InvoiceRead/InvoiceWrite powinny być zwrócone również wtedy, gdy canDelegate=false
        Assert.Contains(personalPermissions.Permissions,
            p => p.PermissionScope == PersonalPermission.PersonalPermissionScopeType.InvoiceRead && p.CanDelegate == false);
        Assert.Contains(personalPermissions.Permissions,
            p => p.PermissionScope == PersonalPermission.PersonalPermissionScopeType.InvoiceWrite && p.CanDelegate == false);

        // 7) Weryfikacja listy uprawnień z filtrem ContextIdentifier=InternalId
        PersonalPermissionsQueryRequest queryWithInternalIdFilter = new()
        {
            ContextIdentifier = new PersonalPermissionsContextIdentifier
            {
                Type = PersonalPermissionsContextIdentifierType.InternalId,
                Value = kindergartenId
            }
        };

        PersonalPermissionsQueryResponse permissionsWithInternalIdFilter =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    queryWithInternalIdFilter,
                    kindergartenAuthToken),
                condition: r => r is not null
                    && r.Permissions is not null
                    && r.Permissions.Count >= ExpectedPermissionsCount,
                cancellationToken: CancellationToken);

        Assert.NotNull(permissionsWithInternalIdFilter);
        Assert.NotEmpty(permissionsWithInternalIdFilter.Permissions);

        Assert.Contains(permissionsWithInternalIdFilter.Permissions,
            p => p.PermissionScope == PersonalPermission.PersonalPermissionScopeType.InvoiceRead && p.CanDelegate == false);
        Assert.Contains(permissionsWithInternalIdFilter.Permissions,
            p => p.PermissionScope == PersonalPermission.PersonalPermissionScopeType.InvoiceWrite && p.CanDelegate == false);

        // 8) Sprzątanie danych testowych
        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest { SubjectNip = municipalOfficeNip }, CancellationToken);
    }
}
