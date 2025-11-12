using KSeF.Client.Api.Builders.EntityPermissions;
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

[CollectionDefinition("GrantPermissionsInAGeneralIndirectMannerTests.feature")]
[Trait("Category", "Features")]
[Trait("Features", "GrantPermissionsInAGeneralIndirectMannerTests.feature")]
public class GrantPermissionsInAGeneralIndirectMannerTests : KsefIntegrationTestBase
{

    [Fact]
    [Trait("Scenario", "Nadanie pośrednich uprawnień przez dwa podmioty pośrednikowi i dalszemu podmiotowi końcowemu")]
    public async Task GivenGrantPermissionsToIntermediary_WhenIntermediaryGrantsIndirectPermissions_ThenFinalSubjectsHasSinglePermissionInEachContext()
    {
        //Arrange
        string firstSubject = MiscellaneousUtils.GetRandomNip();
        string secondSubject = MiscellaneousUtils.GetRandomNip();

        string intermediaryNip = MiscellaneousUtils.GetRandomNip();
        string lastIdentifierInTheChainPesel = MiscellaneousUtils.GetRandomPesel();
        string lastIdentifierInTheChainNip = MiscellaneousUtils.GetRandomNip();

        //Act
        await GrantPermissionsToIntermediaryByTwoCompanies(firstSubject, secondSubject, intermediaryNip);

        AuthenticationOperationStatusResponse intermediaryAuthOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            intermediaryNip);

        #region nadanie uprawnień w sposób pośredni dla wszystkich kontekstów obsługiwanych przez nip {intermediaryNip} danyemu numerowi PESEL oraz NIP

        OperationResponse grantIndirectPermissionsForPeselOperationResponse = await GrantIndirectPermissions(lastIdentifierInTheChainPesel, IndirectEntitySubjectIdentifierType.Pesel, intermediaryAuthOperationStatusResponse.AccessToken.Token);
        
        PermissionsOperationStatusResponse grantIndirectPermissionsForPeselOperationStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.OperationsStatusAsync(grantIndirectPermissionsForPeselOperationResponse.ReferenceNumber, intermediaryAuthOperationStatusResponse.AccessToken.Token),
            status => status is not null &&
                     status.Status is not null &&
                     status.Status.Code == 200,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken.None);
        Assert.True(grantIndirectPermissionsForPeselOperationStatus.Status.Code == 200);

        OperationResponse grantIndirectPermissionsForNipOperationResponse = await GrantIndirectPermissions(lastIdentifierInTheChainNip, IndirectEntitySubjectIdentifierType.Nip, intermediaryAuthOperationStatusResponse.AccessToken.Token);
        
        PermissionsOperationStatusResponse grantIndirectPermissionsForNipOperationStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.OperationsStatusAsync(grantIndirectPermissionsForNipOperationResponse.ReferenceNumber, intermediaryAuthOperationStatusResponse.AccessToken.Token),
            status => status is not null &&
                     status.Status is not null &&
                     status.Status.Code == 200,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken.None);
        Assert.True(grantIndirectPermissionsForNipOperationStatus.Status.Code == OperationStatusCodeResponse.Success);
        #endregion

        //Assert
        #region logowanie jako NIP podmiot końcowy w kontekście podmiotu pierwszego
        AuthenticationOperationStatusResponse lastManNipAuthOperationStatusResponseInFirstContext = await AuthenticationUtils.AuthenticateAsync(
          AuthorizationClient,
          SignatureService,
          lastIdentifierInTheChainNip, firstSubject);

        PersonToken personTokenInFirstContext = TokenService.MapFromJwt(lastManNipAuthOperationStatusResponseInFirstContext.AccessToken.Token);
        Assert.True(personTokenInFirstContext.Permissions.Length == 1);
        #endregion
        # region logowanie jako NIP podmiot końcowy w kontekście podmiotu drugiego
        AuthenticationOperationStatusResponse lastManNipAuthOperationStatusResponseInSecondContext = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            lastIdentifierInTheChainNip, secondSubject);

        PersonToken personTokenInSecondContext = TokenService.MapFromJwt(lastManNipAuthOperationStatusResponseInSecondContext.AccessToken.Token);
        Assert.True(personTokenInSecondContext.Permissions.Length == 1);
        #endregion

        #region logowanie jako PESEL podmiot końcowy w kontekście podmiotu pierwszego
        AuthenticationOperationStatusResponse lastManPeselAuthOperationStatusResponseInFirstContext = await AuthenticationUtils.AuthenticateAsync(
          AuthorizationClient,
          SignatureService,
          lastIdentifierInTheChainNip, firstSubject);

        PersonToken peselTokenInFirstContext = TokenService.MapFromJwt(lastManPeselAuthOperationStatusResponseInFirstContext.AccessToken.Token);
        Assert.True(personTokenInFirstContext.Permissions.Length == 1);
        #endregion

        # region logowanie jako PESEL podmiot końcowy w kontekście podmiotu drugiego
        AuthenticationOperationStatusResponse lastManAuthOperationStatusResponseInSecondContext = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            lastIdentifierInTheChainNip, secondSubject);

        PersonToken peselTokenInSecondContext = TokenService.MapFromJwt(lastManAuthOperationStatusResponseInSecondContext.AccessToken.Token);
        Assert.True(personTokenInSecondContext.Permissions.Length == 1);
        #endregion
    }

    private async Task GrantPermissionsToIntermediaryByTwoCompanies(string subjectNip, string secondSubjectNip, string intermediaryNIP)
    {
        AuthenticationOperationStatusResponse authOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            subjectNip);

        AuthenticationOperationStatusResponse secondAuthOperationStatusResponse = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            secondSubjectNip);

        GrantPermissionsEntityRequest request = GrantEntityPermissionsRequestBuilder
            .Create()
            .WithSubject(new GrantPermissionsEntitySubjectIdentifier { Type = GrantPermissionsEntitySubjectIdentifierType.Nip, Value = intermediaryNIP })
            .WithPermissions(
                EntityPermission.New(EntityStandardPermissionType.InvoiceRead, true),
                EntityPermission.New(EntityStandardPermissionType.InvoiceWrite, true)
            )
            .WithDescription("description")
            .Build();

        OperationResponse firstActionStatusResponse = await KsefClient.GrantsPermissionEntityAsync(request, authOperationStatusResponse.AccessToken.Token);
        Assert.NotNull(firstActionStatusResponse);

        PermissionsOperationStatusResponse firstGrantPermissionsOperationStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.OperationsStatusAsync(firstActionStatusResponse.ReferenceNumber, authOperationStatusResponse.AccessToken.Token),
            status => status is not null &&
                     status.Status is not null &&
                     status.Status.Code == OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken.None);

        OperationResponse secondActionStatusResponse = await KsefClient.GrantsPermissionEntityAsync(request, secondAuthOperationStatusResponse.AccessToken.Token);
        Assert.NotNull(firstActionStatusResponse);

        PermissionsOperationStatusResponse grantOperationStatus = await AsyncPollingUtils.PollAsync(
            async () => await KsefClient.OperationsStatusAsync(secondActionStatusResponse.ReferenceNumber, secondAuthOperationStatusResponse.AccessToken.Token),
            status => status is not null &&
                     status.Status is not null &&
                     status.Status.Code == OperationStatusCodeResponse.Success,
            delay: TimeSpan.FromSeconds(1),
            maxAttempts: 60,
            cancellationToken: CancellationToken.None);
    }

    private async Task<OperationResponse> GrantIndirectPermissions(string identifier, IndirectEntitySubjectIdentifierType type , string authToken)
    {
        IndirectEntitySubjectIdentifier subject = new IndirectEntitySubjectIdentifier
        {
            Type = type,
            Value = identifier
        };

        IndirectEntityTargetIdentifier target = new IndirectEntityTargetIdentifier { Type = IndirectEntityTargetIdentifierType.AllPartners };

        return await PermissionsUtils.GrantIndirectPermissionsAsync(KsefClient, authToken,
            subject, target, new[] { IndirectEntityStandardPermissionType.InvoiceRead });
    }
}