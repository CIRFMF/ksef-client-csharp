#if NETSTANDARD2_0
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfill dla <see cref="AesGcm"/>, który nie jest dostępny na netstandard2.0 / .NET Framework 4.8.
/// Wykorzystuje Windows CNG (BCrypt) przez P/Invoke do uwierzytelnionego szyfrowania/deszyfrowania AES-GCM.
/// </summary>
internal sealed class AesGcmCompat : IDisposable
{
    /// <summary>Maksymalny obsługiwany rozmiar tagu w bajtach (128-bitowy).</summary>
    public const int MaxTagSize = 16;

    /// <summary>Maksymalny obsługiwany rozmiar nonce w bajtach (96-bitowy).</summary>
    public const int MaxNonceSize = 12;

    private readonly byte[] _key;

    /// <summary>
    /// Inicjalizuje nową instancję z podanym kluczem.
    /// </summary>
    /// <param name="key">Klucz AES (128, 192 lub 256 bitów).</param>
    /// <param name="tagSizeInBytes">Oczekiwany rozmiar tagu (ignorowany, zachowany dla kompatybilności API).</param>
    public AesGcmCompat(byte[] key, int tagSizeInBytes = MaxTagSize)
    {
        PlatformGuard.EnsureWindowsCng();
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            throw new ArgumentException("Klucz musi mieć 128, 192 lub 256 bitów.", nameof(key));

        _key = (byte[])key.Clone();
    }

    /// <summary>
    /// Szyfruje dane jawne przy użyciu AES-GCM przez Windows CNG.
    /// </summary>
    public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag)
    {
        if (nonce == null) throw new ArgumentNullException(nameof(nonce));
        if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));
        if (ciphertext == null) throw new ArgumentNullException(nameof(ciphertext));
        if (tag == null) throw new ArgumentNullException(nameof(tag));
        if (ciphertext.Length != plaintext.Length)
            throw new ArgumentException("Szyfrogram musi mieć taką samą długość jak dane jawne.");

        CngEncrypt(_key, nonce, plaintext, ciphertext, tag);
    }

    public void Dispose()
    {
        // Wyzeruj materiał klucza
        Array.Clear(_key, 0, _key.Length);
    }

    #region Windows CNG P/Invoke

    private const int STATUS_SUCCESS = 0;
    private const uint BCRYPT_PAD_NONE = 0;

    [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
    private static extern int BCryptOpenAlgorithmProvider(
        out IntPtr phAlgorithm, string pszAlgId, string pszImplementation, uint dwFlags);

    [DllImport("bcrypt.dll")]
    private static extern int BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, uint dwFlags);

    [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
    private static extern int BCryptSetProperty(
        IntPtr hObject, string pszProperty, byte[] pbInput, int cbInput, uint dwFlags);

    [DllImport("bcrypt.dll")]
    private static extern int BCryptGenerateSymmetricKey(
        IntPtr hAlgorithm, out IntPtr phKey, IntPtr pbKeyObject, int cbKeyObject,
        byte[] pbSecret, int cbSecret, uint dwFlags);

    [DllImport("bcrypt.dll")]
    private static extern int BCryptDestroyKey(IntPtr hKey);

    [DllImport("bcrypt.dll")]
    private static extern int BCryptEncrypt(
        IntPtr hKey,
        byte[] pbInput, int cbInput,
        ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo,
        byte[] pbIV, int cbIV,
        byte[] pbOutput, int cbOutput,
        out int pcbResult,
        uint dwFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
    {
        public int cbSize;
        public int dwInfoVersion;
        public IntPtr pbNonce;
        public int cbNonce;
        public IntPtr pbAuthData;
        public int cbAuthData;
        public IntPtr pbTag;
        public int cbTag;
        public IntPtr pbMacContext;
        public int cbMacContext;
        public int cbAAD;
        public long cbData;
        public int dwFlags;
    }

    private static void CngEncrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag)
    {
        int status = BCryptOpenAlgorithmProvider(out IntPtr hAlg, "AES", null, 0);
        ThrowIfFailed(status, "BCryptOpenAlgorithmProvider");

        try
        {
            // Ustaw tryb łańcuchowania na GCM
            byte[] gcmMode = System.Text.Encoding.Unicode.GetBytes("ChainingModeGCM\0");
            status = BCryptSetProperty(hAlg, "ChainingMode", gcmMode, gcmMode.Length, 0);
            ThrowIfFailed(status, "BCryptSetProperty(ChainingMode)");

            status = BCryptGenerateSymmetricKey(hAlg, out IntPtr hKey, IntPtr.Zero, 0, key, key.Length, 0);
            ThrowIfFailed(status, "BCryptGenerateSymmetricKey");

            try
            {
                GCHandle nonceHandle = GCHandle.Alloc(nonce, GCHandleType.Pinned);
                GCHandle tagHandle = GCHandle.Alloc(tag, GCHandleType.Pinned);

                try
                {
                    BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO authInfo = new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO();
                    authInfo.cbSize = Marshal.SizeOf(typeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO));
                    authInfo.dwInfoVersion = 1; // BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO_VERSION
                    authInfo.pbNonce = nonceHandle.AddrOfPinnedObject();
                    authInfo.cbNonce = nonce.Length;
                    authInfo.pbTag = tagHandle.AddrOfPinnedObject();
                    authInfo.cbTag = tag.Length;

                    status = BCryptEncrypt(
                        hKey,
                        plaintext, plaintext.Length,
                        ref authInfo,
                        null, 0,
                        ciphertext, ciphertext.Length,
                        out int cbResult,
                        BCRYPT_PAD_NONE);

                    ThrowIfFailed(status, "BCryptEncrypt");
                }
                finally
                {
                    nonceHandle.Free();
                    tagHandle.Free();
                }
            }
            finally
            {
                BCryptDestroyKey(hKey);
            }
        }
        finally
        {
            BCryptCloseAlgorithmProvider(hAlg, 0);
        }
    }

    private static void ThrowIfFailed(int status, string operation)
    {
        if (status != STATUS_SUCCESS)
        {
            throw new CryptographicException(
                $"Operacja CNG {operation} zakończyła się niepowodzeniem z kodem NTSTATUS 0x{status:X8}.");
        }
    }

    #endregion
}
#endif
