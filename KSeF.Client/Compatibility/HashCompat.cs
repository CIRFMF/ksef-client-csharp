#if NETSTANDARD2_0
namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla metody statycznej <c>SHA256.HashData(byte[])</c> dostępnej od .NET 5.
/// Na netstandard2.0 używa <c>SHA256.Create().ComputeHash()</c> jako odpowiednika.
/// </summary>
internal static class HashCompat
{
    /// <summary>
    /// Oblicza skrót SHA-256 podanych danych.
    /// </summary>
    /// <param name="source">Dane do zahaszowania.</param>
    /// <returns>Obliczony skrót SHA-256.</returns>
    public static byte[] SHA256HashData(byte[] source)
    {
        using SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(source);
    }

    /// <summary>
    /// Oblicza skrót SHA-256 podanego zakresu bajtów tylko do odczytu.
    /// </summary>
    /// <param name="source">Dane do zahaszowania.</param>
    /// <returns>Obliczony skrót SHA-256.</returns>
    public static byte[] SHA256HashData(ReadOnlySpan<byte> source)
    {
        using SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(source.ToArray());
    }
}
#endif
