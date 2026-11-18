using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Żądanie wygenerowania tokena KSeF o określonym okresie ważności (POST /tokens/rev2).
    /// </summary>
    public class KsefTokenRequestRev2
    {
        /// <summary>
        /// Uprawnienia przypisane tokenowi.
        /// </summary>
        public ICollection<KsefTokenPermissionType> Permissions { get; set; }

        /// <summary>
        /// Opis tokena (5-256 znaków).
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Data rozpoczęcia okresu ważności tokena.
        /// Może przypadać najpóźniej 60 dni po zainicjowaniu generowania tokena.
        /// Jeśli nie zostanie podana, przyjmuje wartość odpowiadającą momentowi inicjacji.
        /// </summary>
        public DateTimeOffset? ValidFrom { get; set; }

        /// <summary>
        /// Data wygaśnięcia tokena.
        /// Może przypadać najpóźniej 365 dni po <see cref="ValidFrom"/>.
        /// Jeśli nie zostanie podana, okres ważności wynosi 90 dni od <see cref="ValidFrom"/>.
        /// </summary>
        public DateTimeOffset? ValidTo { get; set; }
    }
}
