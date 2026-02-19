#if NET9_0_OR_GREATER
using System.Buffers.Text;
#endif

namespace KSeF.Client.Extensions;

    public static class Base64UrlExtensions
{
    public static byte[] DecodeBase64OrBase64Url(this string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
        {
            throw new FormatException("invoiceHash jest pusty.");
        }

        base64String = base64String.Trim();

        // jeśli wygląda na Base64URL -> zamień na Base64
        if (base64String.Contains('-') || base64String.Contains('_'))
        {
            base64String = base64String.Replace('-', '+').Replace('_', '/');
        }

        // dopełnij padding do %4
        int mod = base64String.Length % 4;
        if (mod == 2)
        {
            base64String += "==";
        }
        else if (mod == 3)
        {
            base64String += "=";
        }
        else if (mod != 0)
        {
            throw new FormatException("Nieprawidłowa długość Base64/Base64Url.");
        }

        return Convert.FromBase64String(base64String);
    }

    public static string EncodeBase64UrlToString(this byte[] blob)
    {
#if NET9_0_OR_GREATER
                return Base64Url.EncodeToString(blob);
#else
        // RFC 4648 §5: Base64url = Base64 z zamianą znaków i bez paddingu.
        return Convert.ToBase64String(blob).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#endif
    }
}

