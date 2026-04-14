using KSeF.Client.Api.Builders.EntityPermissions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Entity;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Permissions.IndirectPermission
{
    /// <summary>
    /// Testy E2E scenariusza nadawania uprawnień typu AllPartners.
    /// Dwa podmioty nadają biuru rachunkowemu uprawnienia do delegowania, a biuro przekazuje uprawnienie AllPartners pracownikowi.
    /// Test weryfikuje uwierzytelnienie, przyznanie uprawnień, dostęp pracownika do kontekstów oraz cofnięcie uprawnień.
    /// </summary>
    [Collection("IndirectPermissionScenario")]
    public class AllPartnersIndirectPermissionE2ETests : TestBase
    {
        private const string GrantPermissionsEntityDescription = "AuthorizationWithAllPartnersPermissionsTest";

        /// <summary>
        /// Sprawdza scenariusz nadania uprawnień typu AllPartners i ich działanie.
        /// Scenariusz: dwa podmioty nadają biuru rachunkowemu uprawnienia do delegowania, a biuro przekazuje uprawnienie AllPartners pracownikowi.
        /// Pracownik powinien mieć dostęp do kontekstów wszystkich podmiotów, które udzieliły biuru uprawnień do delegowania.
        /// </summary>
        [Fact]
        public async Task AllPartnersIndirectPermission_E2E_GrantAuthenticateRevoke()
        {
            string accountingOfficeNip = MiscellaneousUtils.GetRandomNip();
            string firstCompanyNip = MiscellaneousUtils.GetRandomNip();
            string secondCompanyNip = MiscellaneousUtils.GetRandomNip();
            string employeePesel = MiscellaneousUtils.GetRandomPesel();
            string grantorAccessToken;
            string[] contextNips = new[] { firstCompanyNip, secondCompanyNip };

            // Arrange: uwierzytelnienie biura rachunkowego (podmiot delegujący)
            AuthenticationOperationStatusResponse accountingOfficeAuth =
                await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, accountingOfficeNip).ConfigureAwait(false);
            string accountingOfficeToken = accountingOfficeAuth.AccessToken.Token;

            foreach (string contextNip in contextNips)
            {
                AuthenticationOperationStatusResponse grantorAuth =
                    await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextNip).ConfigureAwait(false);
                grantorAccessToken = grantorAuth.AccessToken.Token;

                GrantPermissionsEntitySubjectIdentifier GrantPermissionsEntitySubjectIdentifierSubject = new GrantPermissionsEntitySubjectIdentifier()
                {
                    Type = GrantPermissionsEntitySubjectIdentifierType.Nip,
                    Value = accountingOfficeNip
                };

                GrantPermissionsEntityRequest request = GrantEntityPermissionsRequestBuilder
                            .Create()
                            .WithSubject(GrantPermissionsEntitySubjectIdentifierSubject)
                            .WithPermissions(
                                Client.Core.Models.Permissions.Entity.EntityPermission.New(EntityStandardPermissionType.InvoiceRead, true),
                                Client.Core.Models.Permissions.Entity.EntityPermission.New(EntityStandardPermissionType.InvoiceWrite, true)
                            )
                            .WithDescription(GrantPermissionsEntityDescription)
                            .WithSubjectDetails(new PermissionsEntitySubjectDetails
                            {
                                FullName = $"Entity {GrantPermissionsEntitySubjectIdentifierSubject.Value}"
                            })
                            .Build();

                OperationResponse grantPermissionActionResult = await KsefClient.GrantsPermissionEntityAsync(request, grantorAuth.AccessToken.Token).ConfigureAwait(false);
                Assert.NotNull(grantPermissionActionResult);

                PermissionsOperationStatusResponse grantOperationStatus = await AsyncPollingUtils.PollAsync(
                    async () => await KsefClient.OperationsStatusAsync(grantPermissionActionResult.ReferenceNumber, grantorAuth.AccessToken.Token).ConfigureAwait(false),
                    status => status is not null &&
                             status.Status is not null &&
                             status.Status.Code == OperationStatusCodeResponse.Success,
                    delay: TimeSpan.FromSeconds(1),
                    maxAttempts: 60,
                    cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }

            // Act: nadanie uprawnień AllPartners pracownikowi przez biuro rachunkowe
            IndirectEntitySubjectIdentifier subject = new()
            {
                Type = IndirectEntitySubjectIdentifierType.Pesel,
                Value = employeePesel
            };

            IndirectEntityTargetIdentifier target = new() { Type = IndirectEntityTargetIdentifierType.AllPartners };

            OperationResponse grantAllPartnersResponse = await PermissionsUtils.GrantIndirectPermissionsAsync(KsefClient, accountingOfficeToken,
                subject, target, new[] { IndirectEntityStandardPermissionType.InvoiceRead }, GrantPermissionsEntityDescription).ConfigureAwait(false);

            Assert.NotNull(grantAllPartnersResponse);

            foreach (string contextNip in contextNips)
            {
                AuthenticationOperationStatusResponse employeeAuth =
                    await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, employeePesel, contextNip).ConfigureAwait(false);
                Assert.NotNull(employeeAuth);
                Assert.NotNull(employeeAuth.AccessToken);
                Assert.NotNull(employeeAuth.AccessToken.Token);
            }

            // Act: cofnięcie nadanych uprawnień (sprzątanie po teście)
            foreach (string contextNip in contextNips)
            {
                AuthenticationOperationStatusResponse accountingOfficeInContextAuth =
                    await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, contextNip).ConfigureAwait(false);
                grantorAccessToken = accountingOfficeInContextAuth.AccessToken.Token;

                IReadOnlyList<Client.Core.Models.Permissions.PersonPermission> permissionsAfterGranting = await PermissionsUtils.SearchPersonPermissionsAsync(
                       KsefClient,
                       grantorAccessToken,
                       PersonQueryType.PermissionsGrantedInCurrentContext,
                       PersonPermissionState.Active).ConfigureAwait(false);

                foreach (Client.Core.Models.Permissions.PersonPermission permission in permissionsAfterGranting)
                {
                    if (permission.Description == GrantPermissionsEntityDescription)
                    {
                        OperationResponse revokeResponse = await PermissionsUtils.RevokePersonPermissionAsync(KsefClient, grantorAccessToken, permission.Id).ConfigureAwait(false);
                        Assert.NotNull(revokeResponse);
                    }
                }

                IReadOnlyList<Client.Core.Models.Permissions.PersonPermission> permissionsAfterRevoking = (await PermissionsUtils.SearchPersonPermissionsAsync(
                   KsefClient,
                   grantorAccessToken,
                   PersonQueryType.PermissionsGrantedInCurrentContext,
                   PersonPermissionState.Active).ConfigureAwait(false));

                Assert.False(permissionsAfterRevoking.Any(permission => permission.Description == GrantPermissionsEntityDescription));
            }
        }
    }
}