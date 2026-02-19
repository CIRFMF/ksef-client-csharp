#if NETSTANDARD2_0
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla operacji kodowania/dekodowania PEM dostępnych od .NET 5.
/// Zapewnia odpowiednik <c>PemEncoding.Write</c> oraz <c>X509Certificate2.CreateFromPem</c>.
/// </summary>
internal static class PemHelper
{
    private static readonly Regex PemBlockRegex = new(
        @"-----BEGIN\s+(?<label>[^-]+)-----\s*(?<data>[A-Za-z0-9+/=\s]+?)\s*-----END\s+\k<label>-----",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Dekoduje blok zakodowany w PEM, wyodrębniając etykietę i surowe dane binarne.
    /// </summary>
    /// <param name="pem">Ciąg zakodowany w PEM zawierający pojedynczy blok PEM.</param>
    /// <param name="label">Po zakończeniu metody zawiera etykietę z nagłówka PEM (np. "CERTIFICATE", "RSA PRIVATE KEY").</param>
    /// <returns>Zdekodowane dane binarne z bloku PEM.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pem"/> jest <c>null</c>.</exception>
    /// <exception cref="CryptographicException">Ciąg PEM nie zawiera poprawnego bloku PEM.</exception>
    public static byte[] DecodePem(string pem, out string label)
    {
        if (pem is null)
            throw new ArgumentNullException(nameof(pem));

        Match match = PemBlockRegex.Match(pem);
        if (!match.Success)
            throw new CryptographicException("Nie znaleziono poprawnego bloku PEM.");

        label = match.Groups["label"].Value.Trim();
        string base64 = match.Groups["data"].Value;

        // Usuń wszystkie białe znaki z zawartości Base64
        string normalized = new string(base64.Where(c => !char.IsWhiteSpace(c)).ToArray());

        try
        {
            return Convert.FromBase64String(normalized);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Blok PEM zawiera nieprawidłowe dane Base64.", ex);
        }
    }

    /// <summary>
    /// Koduje dane binarne jako ciąg PEM z podaną etykietą.
    /// Odpowiednik <c>new string(PemEncoding.Write(label, data))</c> na .NET 5+.
    /// </summary>
    /// <param name="label">Etykieta PEM (np. "PUBLIC KEY", "CERTIFICATE").</param>
    /// <param name="data">Dane binarne do zakodowania.</param>
    /// <returns>Ciąg zakodowany w PEM z 64-znakowymi liniami Base64.</returns>
    public static string EncodePem(string label, byte[] data)
    {
        if (label is null)
            throw new ArgumentNullException(nameof(label));
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        string base64 = Convert.ToBase64String(data);

        StringBuilder sb = new StringBuilder();
        sb.Append("-----BEGIN ").Append(label).Append("-----").Append('\n');

        // Podziel base64 na 64-znakowe linie (standard PEM)
        for (int i = 0; i < base64.Length; i += 64)
        {
            int length = Math.Min(64, base64.Length - i);
            sb.Append(base64, i, length).Append('\n');
        }

        sb.Append("-----END ").Append(label).Append("-----");
        return sb.ToString();
    }

    /// <summary>
    /// Koduje dane binarne jako PEM i zwraca tablicę <c>char[]</c>.
    /// Odpowiada sygnaturze <c>PemEncoding.Write(string, ReadOnlySpan&lt;byte&gt;)</c> na .NET 5+.
    /// </summary>
    /// <param name="label">Etykieta PEM.</param>
    /// <param name="data">Dane binarne do zakodowania.</param>
    /// <returns>Tablica <c>char[]</c> zawierająca dane zakodowane w PEM.</returns>
    public static char[] WritePem(string label, ReadOnlySpan<byte> data)
    {
        return EncodePem(label, data.ToArray()).ToCharArray();
    }

    /// <summary>
    /// Tworzy <see cref="X509Certificate2"/> z ciągu certyfikatu zakodowanego w PEM.
    /// Polyfill dla <c>X509Certificate2.CreateFromPem(ReadOnlySpan&lt;char&gt;)</c> dostępnego od .NET 5.
    /// </summary>
    /// <param name="certPem">Certyfikat zakodowany w PEM.</param>
    /// <returns>Nowa instancja <see cref="X509Certificate2"/>.</returns>
    /// <exception cref="CryptographicException">PEM nie zawiera poprawnego bloku CERTIFICATE.</exception>
    public static X509Certificate2 CreateCertificateFromPem(string certPem)
    {
        byte[] certBytes = DecodePem(certPem, out string label);

        if (!string.Equals(label, "CERTIFICATE", StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException($"Oczekiwano bloku PEM 'CERTIFICATE', otrzymano '{label}'.");

        return new X509Certificate2(certBytes);
    }
}

// UWAGA: Klasa polyfill PemEncoding została celowo usunięta z tego pliku.
// Microsoft.Bcl.Cryptography 10.0.2 (build netstandard2.0) już zawiera
// 'internal static class PemEncoding' w przestrzeni nazw System.Security.Cryptography.
// Dodanie kolejnej 'internal PemEncoding' w tej samej przestrzeni spowodowałoby CS0122
// (niedostępny), ponieważ kompilator rozwiązuje najpierw typ internal z NuGeta.
//
// Rozwiązanie (FAZA 3): Kod używający PemEncoding.Write() stosuje:
//   #if NETSTANDARD2_0
//       return new string(PemHelper.WritePem("PUBLIC KEY", pubKeyBytes));
//   #else
//       return new string(PemEncoding.Write("PUBLIC KEY", pubKeyBytes));
//   #endif
#endif
