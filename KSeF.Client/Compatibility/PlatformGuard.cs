#if NETSTANDARD2_0
using System.Runtime.InteropServices;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Weryfikuje, czy bieżące środowisko runtime obsługuje operacje kryptograficzne
/// wymagane przez warstwę kompatybilności netstandard2.0.
/// </summary>
/// <remarks>
/// Warstwa kompatybilności netstandard2.0 korzysta z Windows CNG (BCrypt P/Invoke,
/// RSACng, ECDiffieHellmanCng). Te API są dostępne wyłącznie na:
/// <list type="bullet">
///   <item>.NET Framework 4.7.2+ na Windows</item>
/// </list>
/// Dla obsługi wieloplatformowej (Linux, macOS) użyj TFM net8.0/net9.0/net10.0,
/// które mają wbudowane implementacje kryptograficzne.
/// <para>
/// Mono, Xamarin i Unity nie są obsługiwane — brak Windows CNG.
/// </para>
/// </remarks>
internal static class PlatformGuard
{
    // Lazy-init: null = nieoceniony, true = Windows, false = nieobsługiwany.
    // Bezpieczne w scenariuszu wielowątkowym: oba wątki dostaną ten sam wynik
    // (wynik zależy wyłącznie od platformy, która jest niezmienna).
    private static bool? s_isSupported;

    /// <summary>
    /// Sprawdza, czy bieżąca platforma obsługuje operacje kryptograficzne CNG.
    /// Rzuca <see cref="PlatformNotSupportedException"/> z akcjonalnym komunikatem
    /// jeśli platforma nie jest obsługiwana.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// Bieżąca platforma nie obsługuje Windows CNG (Mono, Linux, macOS).
    /// </exception>
    internal static void EnsureWindowsCng()
    {
        if (s_isSupported.HasValue)
        {
            if (!s_isSupported.Value)
                ThrowUnsupported();
            return;
        }

        // Detekcja Mono — Type.GetType("Mono.Runtime") zwraca non-null na Mono
        if (Type.GetType("Mono.Runtime") != null)
        {
            s_isSupported = false;
            ThrowUnsupported();
        }

        // Detekcja systemu operacyjnego — netstandard2.0 crypto wymaga Windows (CNG)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            s_isSupported = false;
            ThrowUnsupported();
        }

        s_isSupported = true;
    }

    private static void ThrowUnsupported()
    {
        throw new PlatformNotSupportedException(
            "Biblioteka KSeF.Client (netstandard2.0) wymaga środowiska " +
            ".NET Framework 4.7.2+ na systemie Windows. " +
            "Operacje kryptograficzne (AES-GCM, ECDH, RSA-PSS, certyfikaty) " +
            "korzystają z Windows CNG, który nie jest dostępny na bieżącej platformie. " +
            "Dla obsługi Linux/macOS użyj pakietu NuGet z TFM net8.0, net9.0 lub net10.0.");
    }
}
#endif
