using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Metadane tokena KSeF w kontrakcie Rev2 (GET /tokens/rev2 oraz GET /tokens/rev2/{referenceNumber}).
    /// </summary>
    public class AuthenticationKsefTokenRev2
    {
        /// <summary>
        /// Numer referencyjny tokena.
        /// </summary>
        public string ReferenceNumber { get; set; }

        /// <summary>
        /// Identyfikator osoby, która wygenerowała token.
        /// </summary>
        public KsefTokenAuthorIdentifierRev2 AuthorIdentifier { get; set; }

        /// <summary>
        /// Identyfikator kontekstu, w którym został wygenerowany token i do którego daje dostęp.
        /// </summary>
        public KsefTokenContextIdentifierRev2 ContextIdentifier { get; set; }

        /// <summary>
        /// Opis tokena.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Uprawnienia przypisane tokenowi.
        /// </summary>
        public ICollection<KsefTokenPermissionRev2> RequestedPermissions { get; set; }

        /// <summary>
        /// Data i czas utworzenia tokena.
        /// </summary>
        public DateTimeOffset DateCreated { get; set; }

        /// <summary>
        /// Data ostatniego użycia tokena.
        /// </summary>
        public DateTimeOffset? LastUseDate { get; set; }

        /// <summary>
        /// Data rozpoczęcia okresu ważności tokena.
        /// </summary>
        public DateTimeOffset ValidFrom { get; set; }

        /// <summary>
        /// Data wygaśnięcia tokena.
        /// </summary>
        public DateTimeOffset ValidTo { get; set; }

        /// <summary>
        /// Status tokena.
        /// </summary>
        public KsefTokenStatusRev2 Status { get; set; }

        /// <summary>
        /// Informacje o przyczynie niepowodzenia aktywacji tokena. Uzupełniane wyłącznie dla statusu Failed.
        /// </summary>
        public KsefTokenFailureRev2 Failure { get; set; }
    }
}
