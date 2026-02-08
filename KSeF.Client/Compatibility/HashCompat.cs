#if NETSTANDARD2_0
namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill for <c>SHA256.HashData(byte[])</c> static method available since .NET 5.
/// On netstandard2.0, uses <c>SHA256.Create().ComputeHash()</c> as equivalent.
/// </summary>
internal static class HashCompat
{
    /// <summary>
    /// Computes the SHA-256 hash of the specified data.
    /// </summary>
    /// <param name="source">The data to hash.</param>
    /// <returns>The computed SHA-256 hash.</returns>
    public static byte[] SHA256HashData(byte[] source)
    {
        using SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(source);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the specified read-only span of bytes.
    /// </summary>
    /// <param name="source">The data to hash.</param>
    /// <returns>The computed SHA-256 hash.</returns>
    public static byte[] SHA256HashData(ReadOnlySpan<byte> source)
    {
        using SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(source.ToArray());
    }
}
#endif
