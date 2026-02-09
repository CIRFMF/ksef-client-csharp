#nullable enable
using System.Runtime.CompilerServices;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Zunifikowane metody klauzul strażnikowych dla wszystkich TFM.
/// Na netstandard2.0: pełna implementacja polyfill.
/// Na net8.0+: inline forwarding do wbudowanych <c>ArgumentNullException.ThrowIfNull()</c> itp.
/// </summary>
internal static class Guard
{
#if NETSTANDARD2_0

    /// <summary>
    /// Rzuca <see cref="ArgumentNullException"/> jeśli <paramref name="argument"/> jest <c>null</c>.
    /// </summary>
    public static void ThrowIfNull(
        object? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
    }

    /// <summary>
    /// Rzuca <see cref="ArgumentException"/> jeśli <paramref name="argument"/> jest <c>null</c>, pusty lub składa się z białych znaków.
    /// </summary>
    public static void ThrowIfNullOrWhiteSpace(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (string.IsNullOrWhiteSpace(argument))
            throw new ArgumentException("Wartość nie może być pustym ciągiem znaków ani składać się wyłącznie z białych znaków.", paramName);
    }

    /// <summary>
    /// Rzuca <see cref="ArgumentException"/> jeśli <paramref name="argument"/> jest <c>null</c> lub pusty.
    /// </summary>
    public static void ThrowIfNullOrEmpty(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
        if (argument.Length == 0)
            throw new ArgumentException("Wartość nie może być pustym ciągiem znaków.", paramName);
    }

#else

    /// <summary>
    /// Przekierowanie do <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull(
        object? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(argument, paramName);
    }

    /// <summary>
    /// Przekierowanie do <see cref="ArgumentException.ThrowIfNullOrWhiteSpace(string?, string?)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace(
        string? argument,
        [CallerArgumentExpression("argument")] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argument, paramName);
    }

    /// <summary>
    /// Przekierowanie do <see cref="ArgumentException.ThrowIfNullOrEmpty(string?, string?)"/>.
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
