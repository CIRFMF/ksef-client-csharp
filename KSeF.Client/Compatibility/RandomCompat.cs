#if NETSTANDARD2_0
#nullable enable
namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla właściwości <c>Random.Shared</c> dostępnej od .NET 6.
/// Używa <c>[ThreadStatic]</c> dla bezpiecznych wątkowo instancji per-wątek.
/// </summary>
internal static class RandomCompat
{
    [ThreadStatic]
    private static Random? _shared;

    /// <summary>
    /// Pobiera bezpieczną wątkowo współdzieloną instancję <see cref="Random"/>.
    /// </summary>
    public static Random Shared => _shared ??= new Random();
}
#endif
