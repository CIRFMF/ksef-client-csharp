using KSeF.Client.Api.Builders.AuthorizationEntityPermissions;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Interfaces.Services;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Authorizations;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Core.Models.TestData;
using KSeF.Client.Core.Models.Token;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography.X509Certificates;
using static KSeF.Client.Core.Models.Permissions.Identifiers.PersonalPermissionsTargetIdentifier;
using static KSeF.Client.Core.Models.Permissions.PersonalPermission;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermission;

public class PersonalPermissions_IndirectSelective_CompleteChain_E2ETests : TestBase
{
    private const int OperationSuccessfulStatusCode = 200;
    private IPersonTokenService _tokenService => Get<IPersonTokenService>();

    /// <summary>
    /// Uwierzytelnienie na uprawnienia nadane w sposób pośredni (selektywnie) z kompletnym łańcuchem (wspólny zakres).
    /// </summary>
    /// <remarks>
    /// 1) Owner NIP -> GRANT dla NIP biura: InvoiceRead.  
    /// 2) Biuro (własny kontekst NIP) -> GRANT dla PESEL pracownika: InvoiceRead.  
    /// 3) Osoba (PESEL) w kontekście NIP właściciela -> QUERY personal/grants (Active) + token: jest „InvoiceRead”.
    /// </remarks>
    [Fact]
    public async Task AuthIndirectSelectiveCompleteChainShouldExposeMatchingEffectivePermission()
    {
        #region Arrange
        string ownerNip = MiscellaneousUtils.GetRandomNip();
        string intermediaryNip = MiscellaneousUtils.GetRandomNip();
        string personPesel = MiscellaneousUtils.GetRandomPesel();
        string descOwnerToIntermediary = $"E2E-Indirect-OwnerToInterm-Read-{intermediaryNip}";
        string descIntermediaryToPerson = $"E2E-Indirect-IntermToPerson-Read-{personPesel}";

        await TestDataClient.CreateSubjectAsync(new SubjectCreateRequest { SubjectNip = ownerNip, Description = $"E2E-Subject-Owner-{ownerNip}" }, CancellationToken);
        await TestDataClient.CreateSubjectAsync(new SubjectCreateRequest { SubjectNip = intermediaryNip, Description = $"E2E-Subject-Interm-{intermediaryNip}" }, CancellationToken);

        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(KsefClient, ownerNip);
        string ownerAccessToken = ownerAuth.AccessToken.Token;

        GrantPermissionsAuthorizationRequest ownerToIntermediary =
            GrantAuthorizationPermissionsRequestBuilder
                .Create()
                .WithSubject(new AuthorizationSubjectIdentifier
                {
                    Type = AuthorizationSubjectIdentifierType.Nip,
                    Value = intermediaryNip
                })
                .WithPermission(AuthorizationPermissionType.RRInvoicing)
                .WithDescription(descOwnerToIntermediary)
				.WithSubjectDetails(new PermissionsAuthorizationSubjectDetails
				{
					FullName = "Podmiot Testowy 1"
				})
				.Build();

        OperationResponse opGrantOwnerToInterm =
            await KsefClient.GrantsAuthorizationPermissionAsync(ownerToIntermediary, ownerAccessToken, CancellationToken);

        await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(opGrantOwnerToInterm.ReferenceNumber, ownerAccessToken),
            condition: r => r.Status.Code == OperationSuccessfulStatusCode,
            "Czekam na GRANT Owner->Intermediary (200)",
            TimeSpan.FromMilliseconds(SleepTime), 60, cancellationToken: CancellationToken);

        AuthenticationOperationStatusResponse intermAuth =
            await AuthenticationUtils.AuthenticateAsync(KsefClient, intermediaryNip);
        string intermediaryAccessToken = intermAuth.AccessToken.Token;

        GrantPermissionsPersonRequest intermToPerson = new()
        {
            SubjectIdentifier = new GrantPermissionsPersonSubjectIdentifier
            {
                Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
                Value = personPesel
            },
            Permissions = [PersonPermissionType.InvoiceRead],
			SubjectDetails = new PersonPermissionSubjectDetails
			{
				SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
				PersonById = new PersonPermissionPersonById
				{
					FirstName = "Jan",
					LastName = "Testowy"
				}
			},
			Description = descIntermediaryToPerson
        };

        OperationResponse opGrantIntermToPerson =
            await KsefClient.GrantsPermissionPersonAsync(intermToPerson, intermediaryAccessToken, CancellationToken);

        await AsyncPollingUtils.PollAsync(
            action: () => KsefClient.OperationsStatusAsync(opGrantIntermToPerson.ReferenceNumber, intermediaryAccessToken),
            condition: r => r.Status.Code == OperationSuccessfulStatusCode,
            "Czekam na GRANT Intermediary->Person (200)",
            TimeSpan.FromMilliseconds(SleepTime), 60, cancellationToken: CancellationToken);

        // AUTH: osoba (PESEL) w kontekście NIP właściciela
        using X509Certificate2 personalCert = SelfSignedCertificateForSignatureBuilder
            .Create().WithGivenName("A").WithSurname("R")
            .WithSerialNumber("PNOPL-" + personPesel).WithCommonName("Indirect Person").Build();

        AuthenticationOperationStatusResponse personAuth =
            await AuthenticationUtils.AuthenticateAsync(
                KsefClient, intermediaryNip, AuthenticationTokenContextIdentifierType.Nip, personalCert);

        string personAccessToken = personAuth.AccessToken.Token;

        PersonalPermissionsQueryRequest query = new()
        {
            ContextIdentifier = new PersonalPermissionsContextIdentifier { Type = PersonalPermissionsContextIdentifierType.Nip, Value = intermediaryNip },
            TargetIdentifier = new PersonalPermissionsTargetIdentifier { Type = PersonalPermissionsTargetIdentifierType.Nip, Value = intermediaryNip },
            PermissionState = PersonPermissionState.Active
        };
        #endregion

        #region Act
        PagedPermissionsResponse<PersonalPermission> page =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(query, personAccessToken, 0, 50, CancellationToken),
                condition: r => r.Permissions != null && r.Permissions.Any(p => p.PermissionScope == PersonalPermissionScopeType.InvoiceRead),
                "Czekam aż pojawi się wpis (Active/InvoiceRead) w kontekście NIP właściciela",
                TimeSpan.FromMilliseconds(SleepTime), 60, cancellationToken: CancellationToken);

        PersonToken token = _tokenService.MapFromJwt(personAccessToken);
        #endregion

        #region Assert
        Assert.NotNull(page);
        Assert.NotEmpty(page.Permissions);
        Assert.Contains(page.Permissions, p => p.PermissionScope == PersonalPermissionScopeType.InvoiceRead);
        #endregion

        #region Cleanup
        foreach (PersonalPermission p in page.Permissions.Where(p => p.PermissionScope == PersonalPermissionScopeType.InvoiceRead))
        {
            OperationResponse revoke =
                await KsefClient.RevokeCommonPermissionAsync(p.Id, intermediaryAccessToken, CancellationToken);
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.OperationsStatusAsync(revoke.ReferenceNumber, intermediaryAccessToken),
                condition: r => r.Status.Code == OperationSuccessfulStatusCode,
                "Czekam na REVOKE Intermediary→Person (200)",
                TimeSpan.FromMilliseconds(SleepTime), 60, cancellationToken: CancellationToken);
        }

        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest { SubjectNip = intermediaryNip }, CancellationToken);
        await TestDataClient.RemoveSubjectAsync(new SubjectRemoveRequest { SubjectNip = ownerNip }, CancellationToken);
        #endregion
    }
}
