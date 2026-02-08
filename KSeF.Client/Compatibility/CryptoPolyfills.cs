#if NETSTANDARD2_0
namespace System.Security.Cryptography;

/// <summary>
/// Polyfill for <c>DSASignatureFormat</c> enum available since .NET 5.
/// Specifies the format of a digital signature.
/// </summary>
internal enum DSASignatureFormat
{
    /// <summary>
    /// The signature format from IEEE P1363 — fixed-size concatenation of r and s values.
    /// </summary>
    IeeeP1363FixedFieldConcatenation = 0,

    /// <summary>
    /// The signature format from RFC 3279 — DER-encoded ASN.1 sequence of r and s values.
    /// </summary>
    Rfc3279DerSequence = 1
}
#endif
