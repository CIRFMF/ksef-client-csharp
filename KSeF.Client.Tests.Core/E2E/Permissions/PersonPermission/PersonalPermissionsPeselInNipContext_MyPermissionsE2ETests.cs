using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;
using static KSeF.Client.Core.Models.Permissions.PersonalPermission;

namespace KSeF.Client.Tests.Core.E2E.Permissions.PersonPermissions;

public class PersonalPermissionsPeselInNipContext_MyPermissionsE2ETests : TestBase
{
    /// <summary>
    /// Pobranie listy moich uprawnień do pracy w KSeF jako osoba uprawniona PESEL w kontekście NIP.
    /// Scenariusz: właściciel nadaje uprawnienia (InvoiceRead, InvoiceWrite) osobie w swoim kontekście NIP,
    /// następnie ta osoba uwierzytelnia się w tym samym kontekście i wywołuje
    /// <c>SearchGrantedPersonalPermissionsAsync</c> filtrowane po NIP. Test sprawdza, że zwrócono dokładnie dwa nadane uprawnienia
    /// w oczekiwanym kontekście (z użyciem mechanizmu pollingu na wypadek opóźnionej spójności).
    /// </summary>
    [Fact]
    public async Task PersonalPermissions_ByPesel_InNipContext_ShouldReturnPermissionsInContext()
    {
        // Arrange
        string contextNip = MiscellaneousUtils.GetRandomNip();
        string pesel = MiscellaneousUtils.GetRandomPesel();

        // Właściciel uwierzytelnia się we własnym kontekście
        AuthenticationOperationStatusResponse ownerAuth =
            await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, contextNip);

        // Nadaj uprawnienia dla osoby (PESEL) w kontekście NIP właściciela
        GrantPermissionsPersonSubjectIdentifier subject = new GrantPermissionsPersonSubjectIdentifier
        {
            Type = GrantPermissionsPersonSubjectIdentifierType.Pesel,
            Value = pesel
        };

        string description = $"Nadanie uprawnień przeglądania i wystawiania faktur dla PESEL {pesel} w kontekście NIP {contextNip}";

        OperationResponse grantResponse = await PermissionsUtils.GrantPersonPermissionsAsync(
            KsefClient,
            ownerAuth.AccessToken.Token,
            subject,
            [
                PersonPermissionType.InvoiceRead,
                PersonPermissionType.InvoiceWrite
            ],
            description);

        Assert.NotNull(grantResponse);

        // Uwierzytelnij się jako osoba (PESEL) w kontekście NIP właściciela
        using System.Security.Cryptography.X509Certificates.X509Certificate2 personalCertificate =
            SelfSignedCertificateForSignatureBuilder
                .Create()
                .WithGivenName("A")
                .WithSurname("R")
                .WithSerialNumber("PNOPL-" + pesel)
                .WithCommonName("A R")
                .Build();

        AuthenticationOperationStatusResponse personAuth = await AuthenticationUtils.AuthenticateAsync(
            AuthorizationClient,
            SignatureService,
            contextNip,
            AuthenticationTokenContextIdentifierType.Nip,
            personalCertificate);

        // Act: pobierz moje uprawnienia dla osoby w bieżącym kontekście NIP, filtrując po kontekście na poziomie zapytania
        PersonalPermissionsQueryRequest query = new PersonalPermissionsQueryRequest
        {
            ContextIdentifier = new PersonalPermissionsContextIdentifier
            {
                Type = PersonalPermissionsContextIdentifierType.Nip,
                Value = contextNip
            }
        };

        PagedPermissionsResponse<PersonalPermission> personalPermissions =
            await AsyncPollingUtils.PollAsync(
                action: () => KsefClient.SearchGrantedPersonalPermissionsAsync(
                    query,
                    personAuth.AccessToken.Token),
                condition: r => r is not null && r.Permissions is not null && r.Permissions.Count >= 2,
                delay: TimeSpan.FromMilliseconds(SleepTime),
                maxAttempts: 30,
                cancellationToken: CancellationToken);

        // Assert
        Assert.NotNull(personalPermissions);
        Assert.NotEmpty(personalPermissions.Permissions);
        Assert.Equal(2, personalPermissions.Permissions.Count);
        List<PersonalPermission> inContextPermissions = personalPermissions.Permissions.Where(p =>
         p.Description == description &&
         p.PermissionState == PersonalPermissionState.Active)
            .ToList();

        Assert.Contains(inContextPermissions, p => p.PermissionScope == PersonalPermissionScopeType.InvoiceRead);
        Assert.Contains(inContextPermissions, p => p.PermissionScope == PersonalPermissionScopeType.InvoiceWrite);

    }
}
