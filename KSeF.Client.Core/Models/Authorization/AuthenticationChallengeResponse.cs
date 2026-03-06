using System;

namespace KSeF.Client.Core.Models.Authorization
{
    public class AuthenticationChallengeResponse
    {
        /// <summary>
        /// Unikatowy ciąg znaków
        /// </summary>
        public string Challenge { get; set; }

        /// <summary>
        /// Czas wygenerowania wyzwania
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Czas wygenerowania wyzwania autoryzacyjnego jako liczba milisekund od 1 stycznia 1970 roku (Unix timestamp)
        /// </summary>
        public long TimestampMs { get; set; }

        /// <summary>
        /// Adres IP klienta
        /// </summary>
        public string ClientIp { get; set; }
    }
}