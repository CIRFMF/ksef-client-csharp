#if NETSTANDARD2_0
namespace System.Security.Cryptography;

/// <summary>
/// Polyfill dla enumeracji <c>DSASignatureFormat</c> dostępnej od .NET 5.
/// Określa format podpisu cyfrowego.
/// </summary>
internal enum DSASignatureFormat
{
    /// <summary>
    /// Format podpisu wg IEEE P1363 — konkatenacja wartości r i s o stałym rozmiarze.
    /// </summary>
    IeeeP1363FixedFieldConcatenation = 0,

    /// <summary>
    /// Format podpisu wg RFC 3279 — sekwencja ASN.1 wartości r i s zakodowana w DER.
    /// </summary>
    Rfc3279DerSequence = 1
}
#endif
