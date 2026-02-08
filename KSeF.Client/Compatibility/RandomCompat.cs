#if NETSTANDARD2_0
#nullable enable
namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill for <c>Random.Shared</c> property available since .NET 6.
/// Uses <c>[ThreadStatic]</c> for thread-safe per-thread instances.
/// </summary>
internal static class RandomCompat
{
    [ThreadStatic]
    private static Random? _shared;

    /// <summary>
    /// Gets a thread-safe shared <see cref="Random"/> instance.
    /// </summary>
    public static Random Shared => _shared ??= new Random();
}
#endif
