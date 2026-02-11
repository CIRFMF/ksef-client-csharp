#nullable enable
#if NETFRAMEWORK
using System.Collections.Generic;

namespace KSeF.Client.Tests.Core.Compatibility;

/// <summary>
/// Polyfille LINQ dla .NET Framework 4.8: <c>DistinctBy</c> (dodany w .NET 6)
/// oraz <c>Dictionary.TryAdd</c> (dodany w .NET Core 2.0).
/// </summary>
internal static class LinqPolyfills
{
    /// <summary>
    /// Zwraca elementy unikalne pod względem klucza wybranego przez <paramref name="keySelector"/>.
    /// Polyfill dla <c>Enumerable.DistinctBy</c> dostępnego od .NET 6.
    /// </summary>
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        HashSet<TKey> seen = new(comparer);
        foreach (TSource element in source)
            if (seen.Add(keySelector(element)))
                yield return element;
    }

    /// <summary>
    /// Próbuje dodać parę klucz-wartość do słownika. Zwraca <c>false</c> jeśli klucz już istnieje.
    /// Polyfill dla <c>Dictionary.TryAdd</c> dostępnego od .NET Core 2.0.
    /// </summary>
    public static bool TryAdd<TKey, TValue>(
        this Dictionary<TKey, TValue> dict,
        TKey key,
        TValue value)
        where TKey : notnull
    {
        if (dict.ContainsKey(key)) return false;
        dict.Add(key, value);
        return true;
    }

    /// <summary>
    /// Dekonstrukcja <see cref="KeyValuePair{TKey,TValue}"/> do krotki (key, value).
    /// Polyfill umożliwiający <c>foreach ((var k, var v) in dict)</c> na .NET Framework 4.8.
    /// </summary>
    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> pair,
        out TKey key,
        out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
#endif
