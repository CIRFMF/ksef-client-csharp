using System;
using System.Globalization;
using System.Text;

namespace KSeF.Client.Core
{
    public static class KsefNumberValidator
    {
        private const byte Polynomial = 0x07;
        private const byte InitValue = 0x00;

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

            string calculated = ComputeChecksum(Encoding.UTF8.GetBytes(data));

			if (!string.Equals(calculated, checksum, StringComparison.OrdinalIgnoreCase))
			{
				errorMessage = $"Nieprawidłowa suma kontrolna. Oczekiwano: {checksum.ToUpperInvariant()}, wyliczono: {calculated}.";
				return false;
			}

			return true;
		}

        private static string ComputeChecksum(byte[] data)
        {
            byte crc = InitValue;

            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 0x80) != 0
                        ? (byte)((crc << 1) ^ Polynomial)
                        : (byte)(crc << 1);
                }
            }

            return crc.ToString("X2", CultureInfo.InvariantCulture); // zawsze 2-znakowy hex
        }
    }
}