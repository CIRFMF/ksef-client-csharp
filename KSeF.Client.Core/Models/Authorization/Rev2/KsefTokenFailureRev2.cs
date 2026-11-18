using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Authorization.Rev2
{
    /// <summary>
    /// Informacje o przyczynie niepowodzenia aktywacji tokena. Uzupełniane wyłącznie dla statusu <see cref="KsefTokenStatusRev2Code.Failed"/>.
    /// </summary>
    public class KsefTokenFailureRev2
    {
        /// <summary>
        /// Kod niepowodzenia, np. token.permission-grant-failed, token.unknown-error.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Opis niepowodzenia przeznaczony do prezentacji użytkownikowi.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Dodatkowe szczegóły błędu.
        /// </summary>
        public ICollection<string> Details { get; set; }
    }
}
