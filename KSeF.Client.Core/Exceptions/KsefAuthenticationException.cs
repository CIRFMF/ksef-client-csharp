using System;
using System.Collections.Generic;
using System.Linq;

namespace KSeF.Client.Core.Exceptions
{
    /// <summary>
    /// Wyjątek zgłaszany, gdy operacja uwierzytelnienia zakończyła się niepowodzeniem
    /// (status inny niż 100 - w toku i 200 - sukces), np. 400, 415, 425, 450, 460, 470, 480.
    /// </summary>
    public sealed class KsefAuthenticationException : Exception
    {
        /// <summary>
        /// Kod statusu operacji uwierzytelnienia zwrócony przez KSeF.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Szczegóły statusu zwrócone przez KSeF (może być puste).
        /// </summary>
        public IReadOnlyList<string> Details { get; }

        /// <summary>
        /// Inicjalizuje nową instancję klasy <see cref="KsefAuthenticationException"/>.
        /// </summary>
        /// <param name="statusCode">Kod statusu uwierzytelnienia.</param>
        /// <param name="message">Opis wyjątku.</param>
        /// <param name="details">Szczegóły statusu.</param>
        public KsefAuthenticationException(int statusCode, string message, IEnumerable<string> details = null)
            : base(message)
        {
            StatusCode = statusCode;
            Details = details?.ToList() ?? new List<string>();
        }
    }
}
