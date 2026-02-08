#if NETSTANDARD2_0
#nullable enable
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill for <c>X509Certificate2.CopyWithPrivateKey(RSA)</c> and <c>CopyWithPrivateKey(ECDsa)</c>
/// which are not part of the netstandard2.0 compile-time contract but ARE available at runtime
/// on .NET Framework 4.7.2+ via <c>RSACertificateExtensions</c> / <c>ECDsaCertificateExtensions</c>.
/// </summary>
internal static class CertificateCompat
{
    private static MethodInfo? _rsaCopyMethod;
    private static MethodInfo? _ecdsaCopyMethod;
    private static bool _rsaResolved;
    private static bool _ecdsaResolved;

    /// <summary>
    /// Creates a new X509Certificate2 by combining the certificate with an RSA private key.
    /// Calls <c>RSACertificateExtensions.CopyWithPrivateKey</c> at runtime via reflection.
    /// </summary>
    public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 cert, RSA rsa)
    {
        if (!_rsaResolved)
        {
            _rsaCopyMethod = ResolveMethod("RSACertificateExtensions", typeof(RSA));
            _rsaResolved = true;
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
    /// Creates a new X509Certificate2 by combining the certificate with an ECDsa private key.
    /// Calls <c>ECDsaCertificateExtensions.CopyWithPrivateKey</c> at runtime via reflection.
    /// </summary>
    public static X509Certificate2 CopyWithPrivateKey(this X509Certificate2 cert, ECDsa ecdsa)
    {
        if (!_ecdsaResolved)
        {
            _ecdsaCopyMethod = ResolveMethod("ECDsaCertificateExtensions", typeof(ECDsa));
            _ecdsaResolved = true;
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

        // Search in all loaded assemblies (covers System.Core.dll on .NET Framework)
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
