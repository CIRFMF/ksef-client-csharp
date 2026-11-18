using System;

namespace KSeF.Client.Core
{
    public static class KsefNumberValidator
    {
        private const int ExpectedLength = 35;
        private const int DataLength = 32;
        private const int ChecksumLength = 2;

        /// <summary>
        /// Weryfikuje numer KSeF poprzez sprawdzenie jego sumy kontrolnej i formatu.
        /// </summary>
        /// <param name="ksefNumber">Numer KSeF do walidacji.</param>
        /// <param name="errorMessage">Zwraca komunikat błędu, jeśli numer jest nieprawidłowy.</param>
        public static bool IsValid(string ksefNumber, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(ksefNumber))
            {
                errorMessage = "Numer KSeF jest pusty.";
                return false;
            }

            if (ksefNumber.Length != ExpectedLength)
            {
                errorMessage = $"Numer KSeF ma nieprawidłową długość: {ksefNumber.Length}. Oczekiwana długość to {ExpectedLength}.";
                return false;
            }

            string data = ksefNumber.Substring(0, DataLength);
            string checksum = ksefNumber.Substring(ExpectedLength - ChecksumLength, ChecksumLength);

            string calculated = Crc8.CalculateChecksum(data);

			if (!string.Equals(calculated, checksum, StringComparison.OrdinalIgnoreCase))
			{
				errorMessage = $"Nieprawidłowa suma kontrolna. Oczekiwano: {checksum.ToUpperInvariant()}, wyliczono: {calculated}.";
				return false;
			}

			return true;
		}
    }
}
