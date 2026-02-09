#if NETSTANDARD2_0
#nullable enable
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla <c>X509Certificate2.CopyWithPrivateKey(RSA)</c> i <c>CopyWithPrivateKey(ECDsa)</c>,
/// które nie są częścią kontraktu kompilacji netstandard2.0, ale SĄ dostępne w runtime
/// na .NET Framework 4.7.2+ przez <c>RSACertificateExtensions</c> / <c>ECDsaCertificateExtensions</c>.
/// </summary>
/// <remarks>
/// Cache refleksji jest zabezpieczony barierami pamięciowymi (<see cref="Volatile"/>),
/// aby zapewnić poprawność na architekturach z weak memory model (np. ARM).
/// Na x86/x64 (TSO) bariery są no-op, więc nie wpływają na wydajność.
/// </remarks>
internal static class CertificateCompat
{
    private static MethodInfo? _rsaCopyMethod;
    private static MethodInfo? _ecdsaCopyMethod;
    private static bool _rsaResolved;
    private static bool _ecdsaResolved;

    /// <summary>
    /// Tworzy nowy <see cref="X509Certificate2"/> łącząc certyfikat z kluczem prywatnym RSA.
    /// Wywołuje <c>RSACertificateExtensions.CopyWithPrivateKey</c> w runtime przez refleksję.
    /// </summary>
    public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 cert, RSA rsa)
    {
        if (!Volatile.Read(ref _rsaResolved))
        {
            _rsaCopyMethod = ResolveMethod("RSACertificateExtensions", typeof(RSA));
            Volatile.Write(ref _rsaResolved, true);
        }

        if (_rsaCopyMethod != null)
        {
            return (X509Certificate2)_rsaCopyMethod.Invoke(null, new object[] { cert, rsa })!;
        }

        throw new PlatformNotSupportedException(
            "CopyWithPrivateKey(RSA) nie jest dostępne na tej platformie. " +
            "Wymagany jest .NET Framework 4.7.2+ lub .NET Core 2.0+.");
    }

    /// <summary>
    /// Tworzy nowy <see cref="X509Certificate2"/> łącząc certyfikat z kluczem prywatnym ECDsa.
    /// Wywołuje <c>ECDsaCertificateExtensions.CopyWithPrivateKey</c> w runtime przez refleksję.
    /// </summary>
    public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 cert, ECDsa ecdsa)
    {
        if (!Volatile.Read(ref _ecdsaResolved))
        {
            _ecdsaCopyMethod = ResolveMethod("ECDsaCertificateExtensions", typeof(ECDsa));
            Volatile.Write(ref _ecdsaResolved, true);
        }

        if (_ecdsaCopyMethod != null)
        {
            return (X509Certificate2)_ecdsaCopyMethod.Invoke(null, new object[] { cert, ecdsa })!;
        }

        throw new PlatformNotSupportedException(
            "CopyWithPrivateKey(ECDsa) nie jest dostępne na tej platformie. " +
            "Wymagany jest .NET Framework 4.7.2+ lub .NET Core 2.0+.");
    }

    private static MethodInfo? ResolveMethod(string className, Type keyType)
    {
        string fullTypeName = $"System.Security.Cryptography.X509Certificates.{className}";

        // Szukaj we wszystkich załadowanych assembly (obejmuje System.Core.dll na .NET Framework)
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? type = assembly.GetType(fullTypeName, throwOnError: false);
            if (type != null)
            {
                MethodInfo? method = type.GetMethod(
                    "CopyWithPrivateKey",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(X509Certificate2), keyType },
                    null);

                if (method != null)
                {
                    return method;
                }
            }
        }

        return null;
    }
}
#endif
