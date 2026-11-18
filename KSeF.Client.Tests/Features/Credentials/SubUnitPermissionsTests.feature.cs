using KSeF.Client.Api.Builders.SubEntityPermissions;
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

namespace KSeF.Client.Tests.Features.Credentials;


[CollectionDefinition("SubUnitPermissionsTests.feature")]
[Trait("Category", "Features")]
[Trait("Features", "SubUnitPermissionsTests.feature")]
public class SubUnitPermissionsTests : KsefIntegrationTestBase
{
    [Fact]
    [Trait("Scenario", "Nadanie uprawnień jednostce podrzędnej (przedszkolu) oraz dyrektorowi tej jednostki")]
    public async Task TestSubunitPermissionsWorkflow()
    {
        // Arrange
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        string invoiceCreatorNip = MiscellaneousUtils.GetRandomNip();
        string municipalOfficeNip = MiscellaneousUtils.GetRandomNip();
        string kindergartenId = MiscellaneousUtils.GenerateInternalIdentifier($"{municipalOfficeNip}"); /*Przykładowa suma kontrolna zgodna z algorytmem podanym w dokumentacji API*/
		string directorPesel = MiscellaneousUtils.GetRandomPesel();

        // --- Etap 1: Wystawienie faktury przez wykonawcę dla przedszkola (jednostki podrzędnej) ---
        string invoiceCreatorAuthToken = (await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient, invoiceCreatorNip)).AccessToken.Token;

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
            municipalOfficeNip, kindergartenId,
            "invoice-template-fa-3-with-custom-Subject2.xml",
            encryptionData,
            CryptographyService,
            true);

        Assert.NotNull(sendInvoiceResponse);

        SessionStatusResponse sessionStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.GetSessionStatusAsync(
                openSessionResponse.ReferenceNumber,
                invoiceCreatorAuthToken).ConfigureAwait(false),
            result => result?.SuccessfulInvoiceCount is not null && result.InvoiceCount == result.SuccessfulInvoiceCount,
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
            AuthorizationClient, municipalOfficeNip)).AccessToken.Token;

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

        OperationResponse grantOperation = await KsefClient.GrantsPermissionSubUnitAsync(
            grantSubUnitRequest,
            municipalOfficeAuthToken);

        PermissionsOperationStatusResponse grantOperationStatus = await PermissionsUtils.WaitForOperationCompletionAsync(KsefClient,
            grantOperation.ReferenceNumber,
            municipalOfficeAuthToken);

        Assert.Equal(OperationStatusCodeResponse.Success, grantOperationStatus.Status.Code);

        // --- Etap 3: dyrektor w kontekście przedszkola nadaje sobie prawo do odczytu faktur ---
        AuthenticationOperationStatusResponse kindergartenAuthResult = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            directorPesel,
            kindergartenId,
            AuthenticationTokenContextIdentifierType.InternalId);

        string kindergartenAuthToken = kindergartenAuthResult
            .AccessToken.Token;

        GrantPermissionsPersonSubjectIdentifier directorSubject = new()
        {
            Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
            Value = directorPesel
        };

		PersonPermissionSubjectDetails subjectDetails = new PersonPermissionSubjectDetails
		{
			SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
			PersonById = new PersonPermissionPersonById
			{
				FirstName = "Jan",
				LastName = "Testowy"
			}
		};

		OperationResponse grantPersonResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
            KsefClient,
            kindergartenAuthToken,
            directorSubject,
            [PersonPermissionType.InvoiceRead],
			subjectDetails,
			"GrantPermissionToDirector");

        PermissionsOperationStatusResponse grantPersonStatus = await PermissionsUtils.WaitForOperationCompletionAsync(KsefClient, grantPersonResponse.ReferenceNumber, kindergartenAuthToken);
        Assert.Equal(OperationStatusCodeResponse.Success, grantPersonStatus.Status.Code);

        // --- Etap 4: Dyrektor przedszkola wyszukuje faktury ---
        RefreshTokenResponse refreshedAccessTokenResponse = await AuthorizationClient.RefreshAccessTokenAsync(kindergartenAuthResult.RefreshToken.Token);
        kindergartenAuthToken = refreshedAccessTokenResponse.AccessToken.Token;

        InvoiceQueryFilters invoiceQuery = new()
        {
            SubjectType = InvoiceSubjectType.Subject3,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddMonths(-2),
                To = DateTime.UtcNow.AddDays(1),
                DateType = DateType.PermanentStorage,
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
        Assert.Contains(invoiceQueryResponse.Invoices, x => x.ThirdSubjects.Any(y=> y.Identifier.Value == kindergartenId));

        //Sprawdzanie dostępu z poziomu gminy
        invoiceQuery = new InvoiceQueryFilters
        {
            SubjectType = InvoiceSubjectType.Subject2,
            DateRange = new DateRange
            {
                From = DateTime.UtcNow.AddDays(-2),
                To = DateTime.UtcNow.AddDays(1),
                DateType = DateType.PermanentStorage,
            }
        };


        invoiceQueryResponse = await KsefClient.QueryInvoiceMetadataAsync(
            requestPayload: invoiceQuery,
            accessToken: municipalOfficeAuthToken,
            cancellationToken: CancellationToken.None,
            pageOffset: 0,
            pageSize: 30);

        // Assert
        Assert.Contains(invoiceQueryResponse.Invoices, invoice => invoice.Buyer.Identifier.Value == municipalOfficeNip);
    }
}

