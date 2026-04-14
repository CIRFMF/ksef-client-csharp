using System.Collections.Generic;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Reprezentuje pojedynczy błąd w odpowiedzi Problem Details dla HTTP 400 Bad Request.
    /// </summary>
    public class BadRequestApiError
    {
        /// <summary>
        /// Kod błędu.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Ogólny opis błędu odpowiadający danemu kodowi.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Lista szczegółowych komunikatów opisujących konkretny błąd.
        /// Może zawierać wiele wpisów dla jednego kodu błędu.
        /// </summary>
        public List<string> Details { get; set; }
    }
}
