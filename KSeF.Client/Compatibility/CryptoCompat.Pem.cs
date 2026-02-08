#if NETSTANDARD2_0
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill for PEM encoding/decoding operations available since .NET 5.
/// Provides <c>PemEncoding.Write</c> equivalent and <c>X509Certificate2.CreateFromPem</c> equivalent.
/// </summary>
internal static class PemHelper
{
    private static readonly Regex PemBlockRegex = new(
        @"-----BEGIN\s+(?<label>[^-]+)-----\s*(?<data>[A-Za-z0-9+/=\s]+?)\s*-----END\s+\k<label>-----",
        RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Decodes a PEM-encoded block, extracting the label and the raw binary data.
    /// </summary>
    /// <param name="pem">The PEM-encoded string containing a single PEM block.</param>
    /// <param name="label">When this method returns, contains the label from the PEM header (e.g., "CERTIFICATE", "RSA PRIVATE KEY").</param>
    /// <returns>The decoded binary data from the PEM block.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="pem"/> is <c>null</c>.</exception>
    /// <exception cref="CryptographicException">The PEM string does not contain a valid PEM block.</exception>
    public static byte[] DecodePem(string pem, out string label)
    {
        if (pem is null)
            throw new ArgumentNullException(nameof(pem));

        Match match = PemBlockRegex.Match(pem);
        if (!match.Success)
            throw new CryptographicException("Nie znaleziono poprawnego bloku PEM.");

        label = match.Groups["label"].Value.Trim();
        string base64 = match.Groups["data"].Value;

        // Remove all whitespace from Base64 content
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
    /// Encodes binary data as a PEM string with the specified label.
    /// Equivalent to <c>new string(PemEncoding.Write(label, data))</c> on .NET 5+.
    /// </summary>
    /// <param name="label">The PEM label (e.g., "PUBLIC KEY", "CERTIFICATE").</param>
    /// <param name="data">The binary data to encode.</param>
    /// <returns>A PEM-encoded string with 64-character Base64 lines.</returns>
    public static string EncodePem(string label, byte[] data)
    {
        if (label is null)
            throw new ArgumentNullException(nameof(label));
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        string base64 = Convert.ToBase64String(data);

        StringBuilder sb = new StringBuilder();
        sb.Append("-----BEGIN ").Append(label).Append("-----").Append('\n');

        // Split base64 into 64-character lines (PEM standard)
        for (int i = 0; i < base64.Length; i += 64)
        {
            int length = Math.Min(64, base64.Length - i);
            sb.Append(base64, i, length).Append('\n');
        }

        sb.Append("-----END ").Append(label).Append("-----");
        return sb.ToString();
    }

    /// <summary>
    /// Encodes binary data as PEM and returns a <c>char[]</c> array.
    /// Matches the signature of <c>PemEncoding.Write(string, ReadOnlySpan&lt;byte&gt;)</c> on .NET 5+.
    /// </summary>
    /// <param name="label">The PEM label.</param>
    /// <param name="data">The binary data to encode.</param>
    /// <returns>A <c>char[]</c> containing the PEM-encoded data.</returns>
    public static char[] WritePem(string label, ReadOnlySpan<byte> data)
    {
        return EncodePem(label, data.ToArray()).ToCharArray();
    }

    /// <summary>
    /// Creates an <see cref="X509Certificate2"/> from a PEM-encoded certificate string.
    /// Polyfill for <c>X509Certificate2.CreateFromPem(ReadOnlySpan&lt;char&gt;)</c> available since .NET 5.
    /// </summary>
    /// <param name="certPem">The PEM-encoded certificate.</param>
    /// <returns>A new <see cref="X509Certificate2"/> instance.</returns>
    /// <exception cref="CryptographicException">The PEM does not contain a valid CERTIFICATE block.</exception>
    public static X509Certificate2 CreateCertificateFromPem(string certPem)
    {
        byte[] certBytes = DecodePem(certPem, out string label);

        if (!string.Equals(label, "CERTIFICATE", StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException($"Oczekiwano bloku PEM 'CERTIFICATE', otrzymano '{label}'.");

        return new X509Certificate2(certBytes);
    }
}

// NOTE: PemEncoding polyfill class was intentionally removed from this file.
// Microsoft.Bcl.Cryptography 10.0.2 (netstandard2.0 build) already contains an
// 'internal static class PemEncoding' in System.Security.Cryptography namespace.
// Adding another 'internal PemEncoding' in the same namespace would cause CS0122
// (inaccessible) because the compiler resolves to the NuGet's internal type first.
//
// Resolution (FAZA 3): Code using PemEncoding.Write() will use:
//   #if NETSTANDARD2_0
//       return new string(PemHelper.WritePem("PUBLIC KEY", pubKeyBytes));
//   #else
//       return new string(PemEncoding.Write("PUBLIC KEY", pubKeyBytes));
//   #endif
#endif
