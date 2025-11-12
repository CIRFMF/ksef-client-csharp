using KSeF.Client.Api.Builders.SubUnitPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.Permissions.SubUnit;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.OnlineSession;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;


[CollectionDefinition("SubUnitPermissionsTests.feature")]
[Trait("Category", "Features")]
[Trait("Features", "SubUnitPermissionsTests.feature")]
public class SubUnitPermissionsTests : KsefIntegrationTestBase
{
    [Fact]
    [Trait("Scenario", "Nadanie uprawnień jednostce podrzędnej (przedszkolu) oraz dyrektorowi tej jednostki")]
    public async Task Test_Subunit_Permissions_Workflow()
    {
        // Arrange
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        string invoiceCreatorNip = MiscellaneousUtils.GetRandomNip();
        string municipalOfficeNip = MiscellaneousUtils.GetRandomNip();
        string kindergartenId = $"{municipalOfficeNip}-12345";
        string directorPesel = MiscellaneousUtils.GetRandomPesel();

        // --- Etap 1: Wystawienie faktury przez wykonawcę dla przedszkola (jednostki podrzędnej) ---
        string invoiceCreatorAuthToken = (await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, SignatureService, invoiceCreatorNip)).AccessToken.Token;

        OpenOnlineSessionResponse openSessionResponse = await OnlineSessionUtils.OpenOnlineSessionAsync(
            KsefClient,
            encryptionData,
            invoiceCreatorAuthToken,
            SystemCode.FA3);

        Assert.NotNull(openSessionResponse?.ReferenceNumber);

        SendInvoiceResponse sendInvoiceResponse = await OnlineSessionUtils.SendInvoiceAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            invoiceCreatorAuthToken,
            invoiceCreatorNip,
            municipalOfficeNip,
            "invoice-template-fa-3-with-custom-Subject2.xml",
            encryptionData,
            CryptographyService);

        Assert.NotNull(sendInvoiceResponse);

        SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollAsync(
            async () => await OnlineSessionUtils.GetOnlineSessionStatusAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                invoiceCreatorAuthToken),
            result => result is not null && result.InvoiceCount == result.SuccessfulInvoiceCount,
            delay: TimeSpan.FromMilliseconds(2*SleepTime),
            maxAttempts: 60);

        Assert.NotNull(sessionStatus);
        Assert.Equal(sessionStatus.InvoiceCount, sessionStatus.SuccessfulInvoiceCount);

        SessionInvoicesResponse invoices = await KsefClient.GetSessionInvoicesAsync(
            openSessionResponse.ReferenceNumber,
            invoiceCreatorAuthToken,
            pageSize: 10);

        Assert.NotEmpty(invoices.Invoices);
        await Task.Delay(10 * SleepTime);

        // --- Etap 2: Gmina nadaje uprawnienia dyrektorowi przedszkola do zarządzania uprawnieniami w kontekście przedszkola---
        string municipalOfficeAuthToken = (await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, SignatureService, municipalOfficeNip)).AccessToken.Token;

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
            .Build();

        OperationResponse grantOperation = await KsefClient.GrantsPermissionSubUnitAsync(
            grantSubUnitRequest,
            municipalOfficeAuthToken);

        bool grantOperationStatus = await PermissionsUtils.ConfirmOperationSuccessAsync(KsefClient,
            grantOperation,
            municipalOfficeAuthToken);

        Assert.NotNull(grantOperationStatus);

        // --- Etap 3: dyrektor w kontekście przedszkola nadaje sobie prawo do odczytu faktur ---
        AuthenticationOperationStatusResponse kindergartenAuthResult = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            directorPesel,
            kindergartenId,
            AuthenticationTokenContextIdentifierType.InternalId);

        string kindergartenAuthToken = kindergartenAuthResult
            .AccessToken.Token;

        GrantPermissionsPersonSubjectIdentifier directorSubject = new GrantPermissionsPersonSubjectIdentifier
        {
            Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
            Value = directorPesel
        };

        OperationResponse grantPersonResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
            KsefClient,
            kindergartenAuthToken,
            directorSubject,
            new[] { PersonPermissionType.InvoiceRead },
            "GrantPermissionToDirector");

        PermissionsOperationStatusResponse grantPersonStatus = await AsyncPollingUtils.PollAsync(
                async () => await PermissionsUtils.GetPermissionsOperationStatusAsync(KsefClient,grantPersonResponse.ReferenceNumber, kindergartenAuthToken),
                status => status is not null &&
                         status.Status is not null &&
                         status.Status.Code == OperationStatusCodeResponse.Success,
                delay: TimeSpan.FromSeconds(5),
                maxAttempts: 60,
                cancellationToken: CancellationToken.None);

        Assert.NotNull(grantPersonStatus);
        Assert.Equal((int)200, grantPersonStatus.Status.Code);

        // --- Etap 4: Dyrektor przedszkola wyszukuje faktury ---
        RefreshTokenResponse refreshedAccessTokenResponse = await AuthorizationClient.RefreshAccessTokenAsync(kindergartenAuthResult.RefreshToken.Token);
        kindergartenAuthToken = refreshedAccessTokenResponse.AccessToken.Token;

        InvoiceQueryFilters invoiceQuery = new InvoiceQueryFilters
        {
            SubjectType = InvoiceSubjectType.Subject3,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddMonths(-2),
                To = DateTime.UtcNow.AddMonths(5),
                DateType = DateType.Issue
            }
        };

        await Task.Delay(20* SleepTime);

        PagedInvoiceResponse invoiceQueryResponse = await KsefClient.QueryInvoiceMetadataAsync(
            requestPayload: invoiceQuery,
            accessToken: kindergartenAuthToken,
            cancellationToken: CancellationToken.None,
            pageOffset: 0,
            pageSize: 30);

        // Assert
        Assert.True(invoiceQueryResponse.Invoices.Any(x => x.ThirdSubjects.Any(y=> y.Identifier.Value == kindergartenId)));

        //Sprawdzanie dostępu z poziomu gminy
        invoiceQuery = new InvoiceQueryFilters
        {
            SubjectType = InvoiceSubjectType.Subject2,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddMonths(-2),
                To = DateTime.UtcNow.AddMonths(5),
                DateType = DateType.Issue
            }
        };


        invoiceQueryResponse = await KsefClient.QueryInvoiceMetadataAsync(
            requestPayload: invoiceQuery,
            accessToken: municipalOfficeAuthToken,
            cancellationToken: CancellationToken.None,
            pageOffset: 0,
            pageSize: 30);

        // Assert
        Assert.True(invoiceQueryResponse.Invoices.Any(x=> x.Buyer.Identifier.Value == municipalOfficeNip));
    }
}

