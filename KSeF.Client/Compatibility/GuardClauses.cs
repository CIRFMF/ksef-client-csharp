#nullable enable
using System.Runtime.CompilerServices;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Unified guard clause methods for all TFMs.
/// On netstandard2.0: full polyfill implementation.
/// On net8.0+: inline forwarding to built-in <c>ArgumentNullException.ThrowIfNull()</c> etc.
/// </summary>
internal static class Guard
{
#if NETSTANDARD2_0

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="argument"/> is <c>null</c>.
    /// </summary>
    public static void ThrowIfNull(
        object? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="argument"/> is <c>null</c>, empty, or whitespace.
    /// </summary>
    public static void ThrowIfNullOrWhiteSpace(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("The value cannot be an empty string or composed entirely of whitespace.", paramName);
    }

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if <paramref name="argument"/> is <c>null</c> or empty.
    /// </summary>
    public static void ThrowIfNullOrEmpty(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (argument.Length == 0)
            throw new ArgumentException("The value cannot be an empty string.", paramName);
    }

#else

    /// <summary>
    /// Forwards to <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull(
        object? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }

    /// <summary>
    /// Forwards to <see cref="ArgumentException.ThrowIfNullOrWhiteSpace(string?, string?)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
    }

    /// <summary>
    /// Forwards to <see cref="ArgumentException.ThrowIfNullOrEmpty(string?, string?)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(argument, paramName);
    }

#endif
}
