using System;
using System.Globalization;
using System.Text;

namespace KSeF.Client.Core
{
    /// <summary>
    /// Oblicza sumę CRC-8 z wielomianem 0x07 i wartością początkową 0x00.
    /// </summary>
    public static class Crc8
    {
        /// <summary>
        /// Oblicza sumę kontrolną bajtów UTF-8 przekazanego tekstu.
        /// </summary>
        /// <param name="input">Tekst objęty sumą kontrolną.</param>
        /// <returns>Dwuznakowy zapis szesnastkowy sumy kontrolnej, wielkimi literami.</returns>
        /// <exception cref="ArgumentNullException">Tekst jest równy <see langword="null"/>.</exception>
        public static string CalculateChecksum(string input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            byte crc = 0x00;
            foreach (byte b in Encoding.UTF8.GetBytes(input))
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc & 0x80) != 0
                        ? (byte)((crc << 1) ^ 0x07)
                        : (byte)(crc << 1);
                }
            }

            return crc.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
