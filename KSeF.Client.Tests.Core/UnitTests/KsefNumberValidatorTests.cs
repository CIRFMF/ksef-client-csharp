using KSeF.Client.Core;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace KSeF.Client.Tests.Core.UnitTests;

/// <summary>
/// Zestaw testów jednostkowych walidatora numeru KSeF.
/// Testy opisane komentarzami AAA (Arrange, Act, Assert).
/// </summary>
public class KsefNumberValidatorTests
{
    /// <summary>
    /// Gdy numer KSeF jest pusty lub zawiera wyłącznie białe znaki,
    /// walidacja powinna zwracać false oraz odpowiedni komunikat o pustej wartości.
    /// </summary>
    [Fact]
    public void IsValidEmptyOrWhitespaceReturnsFalseWithMessage()
    {
        // Arrange (Przygotowanie)
        string empty = string.Empty;
        string whitespace = "   ";

        // Act (Działanie)
        bool resultEmpty = KsefNumberValidator.IsValid(empty, out string emptyMsg);
        bool resultWs = KsefNumberValidator.IsValid(whitespace, out string wsMsg);

        // Assert (Weryfikacja)
        Assert.False(resultEmpty);
        Assert.Equal("Numer KSeF jest pusty.", emptyMsg);

        Assert.False(resultWs);
        Assert.Equal("Numer KSeF jest pusty.", wsMsg);
    }

    /// <summary>
    /// Gdy numer KSeF ma nieprawidłową długość (np. 34 zamiast 35),
    /// walidacja powinna zwracać false oraz komunikat z oczekiwaną długością.
    /// </summary>
    [Fact]
    public void IsValidInvalidLengthReturnsFalseWithMessage()
    {
        // Arrange (Przygotowanie)
        // 34 znaki: 32 dane + 2 suma kontrolna (brakuje jednego znaku do 35)
        string data32 = GetRandomConventionalData32(); // długość 32
        string checksum = ComputeChecksum(data32);
        string tooShort = data32 + checksum; // 34

        // Act (Działanie)
        bool result = KsefNumberValidator.IsValid(tooShort, out string msg);

        // Assert (Weryfikacja)
        Assert.False(result);
        Assert.Equal("Numer KSeF ma nieprawidłową długość: 34. Oczekiwana długość to 35.", msg);
    }

    /// <summary>
    /// Przy poprawnych danych (32 znaki) i zgodnej sumy kontrolnej
    /// walidacja powinna zwrócić true bez komunikatu błędu.
    /// </summary>
    [Fact]
    public void IsValidValidDataAndChecksumReturnsTrue()
    {
        // Arrange (Przygotowanie)
        string data32 = GetRandomConventionalData32(); // długość 32
        string ksef = BuildKsefNumber(data32, 'X');    // 32 + 1 znak wypełniający + 2 suma kontrolna = 35

        // Act (Działanie)
        bool result = KsefNumberValidator.IsValid(ksef, out string msg);

        // Assert (Weryfikacja)
        Assert.True(result);
        Assert.True(string.IsNullOrEmpty(msg));
    }

    /// <summary>
    /// Gdy suma kontrolna nie zgadza się z danymi,
    /// walidacja powinna zwrócić false; obecnie komunikat pozostaje pusty.
    /// </summary>
    [Fact]
    public void IsValidMismatchedChecksumReturnsFalseAndEmptyMessage()
    {
        // Arrange (Przygotowanie)
        string data32 = GetRandomConventionalData32();
        string ksef = BuildKsefNumber(data32, 'X');

        // Zmień ostatnią cyfrę sumy kontrolnej, aby wymusić niezgodność
        string invalid = ksef[..^1] + (ksef[^1] == '0' ? '1' : '0');

        // Act (Działanie)
        bool result = KsefNumberValidator.IsValid(invalid, out string msg);

        // Assert (Weryfikacja)
        Assert.False(result);
        // Obecna implementacja nie ustawia komunikatu błędu dla niezgodnej sumy kontrolnej
        Assert.True(string.IsNullOrEmpty(msg));
    }

    /// <summary>
    /// Modyfikacja 33. znaku (znaku wypełniającego) nie wpływa na wynik walidacji
    /// w bieżącym zachowaniu walidatora.
    /// </summary>
    [Fact]
    public void IsValidModifying33rdCharacterDoesNotAffectValidationCurrentBehavior()
    {
        // Arrange (Przygotowanie)
        string data32 = GetRandomConventionalData32();
        string baseKsef = BuildKsefNumber(data32, 'X');
        string alteredKsef = BuildKsefNumber(data32, 'Y'); // różni się tylko 33. znak

        // Act (Działanie)
        bool resultBase = KsefNumberValidator.IsValid(baseKsef, out string msg1);
        bool resultAltered = KsefNumberValidator.IsValid(alteredKsef, out string msg2);

        // Assert (Weryfikacja)
        Assert.True(resultBase);
        Assert.True(resultAltered);
        Assert.True(string.IsNullOrEmpty(msg1));
        Assert.True(string.IsNullOrEmpty(msg2));
    }

    private static string BuildKsefNumber(string data32, char filler)
    {
        string checksum = ComputeChecksum(data32);
        // UWAGA: Oczekiwana długość to 35, podczas gdy Dane(32) + SumaKontrolna(2) = 34.
        // Dodajemy znak wypełniający, aby uzyskać 35. Bieżący walidator ignoruje ten znak.
        return data32 + filler + checksum;
    }

    // Generuje losowe dane (32 znaki) w konwencji: yyyyMMdd-EE-XXXXXXXXXX-XXXXXXXXXX-XX,
    // a następnie ucina do 32 znaków, aby spełnić założenia walidatora.
    private static string GetRandomConventionalData32()
    {
        string date = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture); // np. 20250916
        string part1 = RandomHex(10);                       // 10 znaków HEX
        string part2 = RandomHex(10);                       // 10 znaków HEX
#if NETFRAMEWORK
        byte[] rndBytes = new byte[4];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) { rng.GetBytes(rndBytes); }
        int rndVal = (int)(BitConverter.ToUInt32(rndBytes, 0) % 100);
        string suffix = rndVal.ToString("D2", CultureInfo.InvariantCulture);
#else
        string suffix = RandomNumberGenerator.GetInt32(0, 100).ToString("D2", CultureInfo.InvariantCulture); // 2 cyfry 00-99
#endif

        string full = $"{date}-EE-{part1}-{part2}-{suffix}"; // długość 36
        return full[..32]; // obcięcie do 32 znaków
    }

    private static string RandomHex(int length)
    {
        int byteLen = (length + 1) / 2;
#if NETFRAMEWORK
        byte[] bytes = new byte[byteLen];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create()) { rng.GetBytes(bytes); }
#else
        Span<byte> bytes = stackalloc byte[byteLen];
        RandomNumberGenerator.Fill(bytes);
#endif

        StringBuilder sb = new(byteLen * 2);
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }

        string hex = sb.ToString();
        return hex[..length]; // zwróć dokładnie 'length' znaków HEX (A-F wielkie)
    }

    // Replikuje logikę sumy kontrolnej używaną w KsefNumberValidator
    private static string ComputeChecksum(string data32)
    {
        byte crc = 0x00;
        foreach (byte b in Encoding.UTF8.GetBytes(data32))
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