using KSeF.Client.Core.Interfaces.Clients;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.IndirectEntity;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Features;

public partial class CredentialsRevokeTests
{
    /// <summary>
    /// Pomocnicza klasa do testów unieważniania i nadawania uprawnień (Credentials).
    /// Zawiera metody opakowujące wywołania API oraz ułatwiające sprawdzanie statusu operacji.
    /// </summary>
    private class CredentialsRevokeHelpers
    {
        /// <summary>
        /// Wyszukuje uprawnienia nadane osobom fizycznym w bieżącym kontekście, filtrowane po stanie uprawnienia.
        /// </summary>
        /// <param name="client">Klient KSeF używany do wywołań API.</param>
        /// <param name="token">Token dostępu używany do autoryzacji.</param>
        /// <param name="state">Stan uprawnienia do filtrowania (np. aktywne/nieaktywne).</param>
        /// <returns>Listę uprawnień osoby w formie tylko do odczytu.</returns>
        public static async Task<IReadOnlyList<PersonPermission>> SearchPersonPermissionsAsync(
        IKSeFClient client, string token, PersonPermissionState state
            )
        => await PermissionsUtils.SearchPersonPermissionsAsync(
               client,
               token,
               PersonQueryType.PermissionsGrantedInCurrentContext,
               state);

        /// <summary>
        /// Nadaje uprawnienie CredentialsManage delegatowi zidentyfikowanemu przez NIP.
        /// </summary>
        /// <param name="client">Klient KSeF używany do wywołań API.</param>
        /// <param name="ownerToken">Token właściciela uprawnień (nadawcy).</param>
        /// <param name="delegateNip">NIP delegata, któremu zostanie nadane uprawnienie.</param>
        /// <returns>Prawda, jeśli operacja zakończyła się powodzeniem.</returns>
        public static async Task<bool> GrantCredentialsManageToDelegateAsync(
            IKSeFClient client, string ownerToken, string delegateNip)
        {
            GrantPermissionsPersonSubjectIdentifier subjectIdentifier = new GrantPermissionsPersonSubjectIdentifier { Type = GrantPermissionsPersonSubjectIdentifierType.Nip, Value = delegateNip };
            PersonPermissionType[] permissions = new[] { PersonPermissionType.CredentialsManage };

            OperationResponse operationResponse = await PermissionsUtils.GrantPersonPermissionsAsync(client, ownerToken, subjectIdentifier, permissions);

            return await ConfirmOperationSuccessAsync(client, operationResponse, ownerToken);
        }

        /// <summary>
        /// Odbiera (unieważnia) wskazane uprawnienie osoby po jego identyfikatorze.
        /// </summary>
        /// <param name="client">Klient KSeF używany do wywołań API.</param>
        /// <param name="token">Token dostępu używany do autoryzacji.</param>
        /// <param name="permissionId">Identyfikator uprawnienia do unieważnienia.</param>
        /// <returns>Prawda, jeśli operacja zakończyła się powodzeniem.</returns>
        public static async Task<bool> RevokePersonPermissionAsync(
            IKSeFClient client, string token, string permissionId)
        {
            OperationResponse operationResponse = await PermissionsUtils.RevokePersonPermissionAsync(client, token, permissionId);

            return await ConfirmOperationSuccessAsync(client, operationResponse, token);
        }

        /// <summary>
        /// Nadaje uprawnienie InvoiceWrite osobie z PESEL-em w trybie pośrednim
        /// (subject: PESEL, target: NIP właściciela), korzystając z tokena delegata.
        /// </summary>
        /// <param name="client">Klient KSeF używany do wywołań API.</param>
        /// <param name="delegateToken">Token delegata posiadającego prawo nadawania uprawnień.</param>
        /// <param name="nipOwner">NIP właściciela (target), w którego kontekście nadawane jest uprawnienie.</param>
        /// <param name="pesel">PESEL osoby, której nadawane jest uprawnienie.</param>
        /// <returns>Prawda, jeśli operacja zakończyła się powodzeniem.</returns>
        public static async Task<bool> GrantInvoiceWriteToPeselAsManagerAsync(
            IKSeFClient client, string delegateToken, string nipOwner, string pesel)
        {
            IndirectEntitySubjectIdentifier subjectIdentifier = new IndirectEntitySubjectIdentifier
            {
                Type = IndirectEntitySubjectIdentifierType.Pesel,
                Value = pesel
            };

            IndirectEntityTargetIdentifier targetIdentifier = new IndirectEntityTargetIdentifier
            {
                Type = IndirectEntityTargetIdentifierType.Nip,
                Value = nipOwner
            };

            IndirectEntityStandardPermissionType[] permissions = new[] { IndirectEntityStandardPermissionType.InvoiceWrite };

            OperationResponse operationResponse = await PermissionsUtils.GrantIndirectPermissionsAsync(client, delegateToken, subjectIdentifier, targetIdentifier, permissions);

            return await ConfirmOperationSuccessAsync(client, operationResponse, delegateToken);
        }

        /// <summary>
        /// Pomocnicza metoda potwierdzająca powodzenie operacji nadawania/odbierania uprawnień.
        /// Czeka krótką chwilę, a następnie sprawdza status operacji po numerze referencyjnym.
        /// </summary>
        /// <param name="client">Klient KSeF używany do wywołań API.</param>
        /// <param name="operationResponse">Odpowiedź inicjująca operację (z numerem referencyjnym).</param>
        /// <param name="token">Token dostępu używany do autoryzacji odczytu statusu operacji.</param>
        /// <returns>Prawda, jeżeli status operacji zwróci kod 200.</returns>
        private static async Task<bool> ConfirmOperationSuccessAsync(
            IKSeFClient client, OperationResponse operationResponse, string token)
        {
            if (string.IsNullOrWhiteSpace(operationResponse?.ReferenceNumber))
                return false;

            // Krótkie odczekanie, aby backend zdążył przetworzyć operację
            await Task.Delay(1000);

            PermissionsOperationStatusResponse status = await PermissionsUtils.GetPermissionsOperationStatusAsync(client, operationResponse.ReferenceNumber!, token);
            return status?.Status?.Code == OperationStatusCodeResponse.Success;
        }
    }
}