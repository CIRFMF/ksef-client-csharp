#if NETSTANDARD2_0
using System.Formats.Asn1;
using System.Security.Cryptography;

namespace KSeF.Client.Compatibility;

/// <summary>
/// Polyfillowe metody rozszerzające dla operacji importu/eksportu kluczy <see cref="RSA"/>
/// dostępnych od .NET 5 / .NET Core 3.0.
/// Używa <see cref="System.Formats.Asn1"/> do kodowania/dekodowania ASN.1 DER.
/// </summary>
internal static class RsaCompat
{
    /// <summary>OID algorytmu szyfrowania RSA (1.2.840.113549.1.1.1).</summary>
    private const string RsaEncryptionOid = "1.2.840.113549.1.1.1";

    /// <summary>
    /// Importuje klucz RSA z ciągu zakodowanego w PEM.
    /// Polyfill dla <c>RSA.ImportFromPem(ReadOnlySpan&lt;char&gt;)</c> dostępnego od .NET 5.
    /// Obsługuje: RSA PRIVATE KEY (PKCS#1), PRIVATE KEY (PKCS#8), PUBLIC KEY (SPKI), RSA PUBLIC KEY (PKCS#1).
    /// </summary>
    /// <param name="rsa">Instancja RSA, do której importowany jest klucz.</param>
    /// <param name="input">Klucz zakodowany w PEM.</param>
    /// <exception cref="ArgumentNullException"><paramref name="input"/> jest <c>null</c>.</exception>
    /// <exception cref="CryptographicException">Blok PEM nie jest rozpoznanym formatem klucza RSA.</exception>
    public static void ImportFromPem(this RSA rsa, string input)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        byte[] der = PemHelper.DecodePem(input, out string label);

        switch (label.ToUpperInvariant())
        {
            case "RSA PRIVATE KEY":
                ImportRsaPrivateKeyCore(rsa, der);
                break;

            case "PRIVATE KEY":
                ImportPkcs8PrivateKey(rsa, der);
                break;

            case "PUBLIC KEY":
                ImportSubjectPublicKeyInfo(rsa, der);
                break;

            case "RSA PUBLIC KEY":
                ImportRsaPublicKeyCore(rsa, der);
                break;

            default:
                throw new CryptographicException(
                    $"Nieobsługiwany typ bloku PEM dla RSA: '{label}'.");
        }
    }

    /// <summary>
    /// Importuje klucz RSA z zaszyfrowanego ciągu zakodowanego w PEM.
    /// Polyfill dla <c>RSA.ImportFromEncryptedPem(ReadOnlySpan&lt;char&gt;, ReadOnlySpan&lt;char&gt;)</c> dostępnego od .NET 5.
    /// Obsługuje: ENCRYPTED PRIVATE KEY (PKCS#8 zaszyfrowany).
    /// </summary>
    /// <param name="rsa">Instancja RSA, do której importowany jest klucz.</param>
    /// <param name="input">Zaszyfrowany klucz zakodowany w PEM.</param>
    /// <param name="password">Hasło do odszyfrowania klucza.</param>
    public static void ImportFromEncryptedPem(this RSA rsa, string input, string password)
    {
        if (input is null)
            throw new ArgumentNullException(nameof(input));

        byte[] der = PemHelper.DecodePem(input, out string label);

        if (!string.Equals(label, "ENCRYPTED PRIVATE KEY", StringComparison.OrdinalIgnoreCase))
            throw new CryptographicException(
                $"Oczekiwano bloku PEM 'ENCRYPTED PRIVATE KEY', otrzymano '{label}'.");

        byte[] decryptedPkcs8 = Pkcs8Decryptor.DecryptPkcs8(der, password);
        ImportPkcs8PrivateKey(rsa, decryptedPkcs8);
    }

    /// <summary>
    /// Eksportuje klucz prywatny RSA w formacie PKCS#1 RSAPrivateKey DER.
    /// Polyfill dla <c>RSA.ExportRSAPrivateKey()</c> dostępnego od .NET Core 3.0.
    /// </summary>
    /// <param name="rsa">Instancja RSA, której klucz ma być wyeksportowany.</param>
    /// <returns>Tablica bajtów zawierająca klucz prywatny zakodowany w PKCS#1 DER.</returns>
    public static byte[] ExportRSAPrivateKey(this RSA rsa)
    {
        RSAParameters parameters = rsa.ExportParameters(true);
        return EncodeRsaPrivateKey(parameters);
    }

    /// <summary>
    /// Eksportuje klucz publiczny RSA w formacie SubjectPublicKeyInfo (SPKI) DER.
    /// Polyfill dla <c>RSA.ExportSubjectPublicKeyInfo()</c> dostępnego od .NET Core 3.0.
    /// </summary>
    /// <param name="rsa">Instancja RSA, której klucz ma być wyeksportowany.</param>
    /// <returns>Tablica bajtów zawierająca klucz publiczny zakodowany w SPKI DER.</returns>
    public static byte[] ExportSubjectPublicKeyInfo(this RSA rsa)
    {
        RSAParameters parameters = rsa.ExportParameters(false);
        return EncodeSubjectPublicKeyInfo(parameters);
    }

    /// <summary>
    /// Importuje klucz prywatny PKCS#1 RSAPrivateKey z tablicy bajtów zakodowanej w DER.
    /// Polyfill dla <c>RSA.ImportRSAPrivateKey(ReadOnlySpan&lt;byte&gt;, out int)</c> dostępnego od .NET Core 3.0.
    /// </summary>
    /// <param name="rsa">Instancja RSA, do której importowany jest klucz.</param>
    /// <param name="source">Dane klucza prywatnego RSA PKCS#1 zakodowane w DER.</param>
    /// <param name="bytesRead">Liczba bajtów odczytanych z <paramref name="source"/>.</param>
    public static void ImportRSAPrivateKey(this RSA rsa, ReadOnlySpan<byte> source, out int bytesRead)
    {
        byte[] sourceArray = source.ToArray();
        ImportRsaPrivateKeyCore(rsa, sourceArray);
        bytesRead = sourceArray.Length;
    }

    #region PKCS#1 RSAPrivateKey ASN.1

    /// <summary>
    /// Dekoduje strukturę PKCS#1 RSAPrivateKey DER i importuje ją do instancji RSA.
    /// <code>
    /// RSAPrivateKey ::= SEQUENCE {
    ///     version           INTEGER,
    ///     modulus           INTEGER,
    ///     publicExponent    INTEGER,
    ///     privateExponent   INTEGER,
    ///     prime1            INTEGER,
    ///     prime2            INTEGER,
    ///     exponent1         INTEGER,
    ///     exponent2         INTEGER,
    ///     coefficient       INTEGER
    /// }
    /// </code>
    /// </summary>
    private static void ImportRsaPrivateKeyCore(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        sequence.ReadInteger(); // wersja (0)

        RSAParameters parameters = new RSAParameters
        {
            Modulus = ReadUnsignedInteger(sequence),
            Exponent = ReadUnsignedInteger(sequence),
            D = ReadUnsignedInteger(sequence),
            P = ReadUnsignedInteger(sequence),
            Q = ReadUnsignedInteger(sequence),
            DP = ReadUnsignedInteger(sequence),
            DQ = ReadUnsignedInteger(sequence),
            InverseQ = ReadUnsignedInteger(sequence)
        };

        // Dopełnij składniki klucza prywatnego do prawidłowej długości
        int halfModLen = (parameters.Modulus.Length + 1) / 2;
        parameters.D = PadOrTrimLeft(parameters.D, parameters.Modulus.Length);
        parameters.P = PadOrTrimLeft(parameters.P, halfModLen);
        parameters.Q = PadOrTrimLeft(parameters.Q, halfModLen);
        parameters.DP = PadOrTrimLeft(parameters.DP, halfModLen);
        parameters.DQ = PadOrTrimLeft(parameters.DQ, halfModLen);
        parameters.InverseQ = PadOrTrimLeft(parameters.InverseQ, halfModLen);

        rsa.ImportParameters(parameters);
    }

    /// <summary>
    /// Koduje parametry klucza prywatnego RSA jako PKCS#1 RSAPrivateKey DER.
    /// </summary>
    private static byte[] EncodeRsaPrivateKey(RSAParameters parameters)
    {
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

        writer.PushSequence();
        writer.WriteInteger(0); // wersja
        WriteUnsignedInteger(writer, parameters.Modulus);
        WriteUnsignedInteger(writer, parameters.Exponent);
        WriteUnsignedInteger(writer, parameters.D);
        WriteUnsignedInteger(writer, parameters.P);
        WriteUnsignedInteger(writer, parameters.Q);
        WriteUnsignedInteger(writer, parameters.DP);
        WriteUnsignedInteger(writer, parameters.DQ);
        WriteUnsignedInteger(writer, parameters.InverseQ);
        writer.PopSequence();

        return writer.Encode();
    }

    #endregion

    #region PKCS#1 Klucz publiczny RSA

    /// <summary>
    /// Dekoduje PKCS#1 RSAPublicKey i importuje go.
    /// <code>
    /// RSAPublicKey ::= SEQUENCE {
    ///     modulus         INTEGER,
    ///     publicExponent  INTEGER
    /// }
    /// </code>
    /// </summary>
    private static void ImportRsaPublicKeyCore(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        RSAParameters parameters = new RSAParameters
        {
            Modulus = ReadUnsignedInteger(sequence),
            Exponent = ReadUnsignedInteger(sequence)
        };

        rsa.ImportParameters(parameters);
    }

    #endregion

    #region SubjectPublicKeyInfo (SPKI)

    /// <summary>
    /// Dekoduje strukturę SubjectPublicKeyInfo (SPKI) zawierającą klucz publiczny RSA.
    /// <code>
    /// SubjectPublicKeyInfo ::= SEQUENCE {
    ///     algorithm       AlgorithmIdentifier,
    ///     subjectPublicKey BIT STRING
    /// }
    /// AlgorithmIdentifier ::= SEQUENCE {
    ///     algorithm  OID,
    ///     parameters ANY OPTIONAL  -- NULL dla RSA
    /// }
    /// </code>
    /// </summary>
    private static void ImportSubjectPublicKeyInfo(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader spkiSequence = reader.ReadSequence();

        // Identyfikator algorytmu
        AsnReader algId = spkiSequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != RsaEncryptionOid)
            throw new CryptographicException($"Oczekiwano OID RSA ({RsaEncryptionOid}), otrzymano '{oid}'.");

        // Odczytaj i odrzuć parametry (NULL dla RSA)
        if (algId.HasData)
            algId.ReadNull();

        // SubjectPublicKey BIT STRING → zawiera PKCS#1 RSAPublicKey
        byte[] publicKeyBits = spkiSequence.ReadBitString(out _);
        ImportRsaPublicKeyCore(rsa, publicKeyBits);
    }

    /// <summary>
    /// Koduje parametry klucza publicznego RSA jako SubjectPublicKeyInfo (SPKI) DER.
    /// </summary>
    private static byte[] EncodeSubjectPublicKeyInfo(RSAParameters parameters)
    {
        // Najpierw zakoduj wewnętrzny RSAPublicKey
        AsnWriter innerWriter = new AsnWriter(AsnEncodingRules.DER);
        innerWriter.PushSequence();
        WriteUnsignedInteger(innerWriter, parameters.Modulus);
        WriteUnsignedInteger(innerWriter, parameters.Exponent);
        innerWriter.PopSequence();
        byte[] rsaPublicKey = innerWriter.Encode();

        // Teraz zakoduj otoczkę SPKI
        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();

        // Identyfikator algorytmu
        writer.PushSequence();
        writer.WriteObjectIdentifier(RsaEncryptionOid);
        writer.WriteNull();
        writer.PopSequence();

        // SubjectPublicKey jako BIT STRING
        writer.WriteBitString(rsaPublicKey);

        writer.PopSequence();
        return writer.Encode();
    }

    #endregion

    #region PKCS#8 PrivateKeyInfo

    /// <summary>
    /// Dekoduje strukturę PKCS#8 PrivateKeyInfo i importuje klucz RSA.
    /// <code>
    /// PrivateKeyInfo ::= SEQUENCE {
    ///     version                   INTEGER,
    ///     privateKeyAlgorithm       AlgorithmIdentifier,
    ///     privateKey                OCTET STRING  -- zawiera PKCS#1 RSAPrivateKey
    /// }
    /// </code>
    /// </summary>
    private static void ImportPkcs8PrivateKey(RSA rsa, byte[] der)
    {
        AsnReader reader = new AsnReader(der, AsnEncodingRules.DER);
        AsnReader sequence = reader.ReadSequence();

        sequence.ReadInteger(); // wersja (0)

        // Identyfikator algorytmu
        AsnReader algId = sequence.ReadSequence();
        string oid = algId.ReadObjectIdentifier();
        if (oid != RsaEncryptionOid)
            throw new CryptographicException($"PKCS#8 zawiera algorytm '{oid}', oczekiwano RSA ({RsaEncryptionOid}).");

        // PrivateKey OCTET STRING → zawiera PKCS#1 RSAPrivateKey
        byte[] privateKeyOctets = sequence.ReadOctetString();
        ImportRsaPrivateKeyCore(rsa, privateKeyOctets);
    }

    #endregion

    #region Pomocniki ASN.1 Integer

    /// <summary>
    /// Odczytuje ASN.1 INTEGER i zwraca bajty wartości bezwzględnej (bez znaku, bez wiodącego zera).
    /// </summary>
    private static byte[] ReadUnsignedInteger(AsnReader reader)
    {
        ReadOnlyMemory<byte> value = reader.ReadIntegerBytes();
        byte[] bytes = value.ToArray();

        // Usuń wiodący bajt zerowy używany jako znak dodatni w ASN.1
        if (bytes.Length > 1 && bytes[0] == 0)
        {
            byte[] trimmed = new byte[bytes.Length - 1];
            Buffer.BlockCopy(bytes, 1, trimmed, 0, trimmed.Length);
            return trimmed;
        }

        return bytes;
    }

    /// <summary>
    /// Zapisuje wartość liczby całkowitej bez znaku jako ASN.1 INTEGER (dodaje wiodące zero jeśli najwyższy bit jest ustawiony).
    /// </summary>
    private static void WriteUnsignedInteger(AsnWriter writer, byte[] value)
    {
        if (value is null || value.Length == 0)
        {
            writer.WriteInteger(0);
            return;
        }

        writer.WriteIntegerUnsigned(new ReadOnlySpan<byte>(value));
    }

    /// <summary>
    /// Dopełnia lub przycina tablicę bajtów do dokładnej docelowej długości.
    /// Jeśli krótsza — dopełnia wiodącymi zerami. Jeśli dłuższa i wiodące bajty są zerami — przycina.
    /// </summary>
    private static byte[] PadOrTrimLeft(byte[] data, int targetLength)
    {
        if (data.Length == targetLength)
            return data;

        if (data.Length < targetLength)
        {
            byte[] padded = new byte[targetLength];
            Buffer.BlockCopy(data, 0, padded, targetLength - data.Length, data.Length);
            return padded;
        }

        // Przytnij wiodące zera
        int offset = data.Length - targetLength;
        for (int i = 0; i < offset; i++)
        {
            if (data[i] != 0)
                throw new CryptographicException("Wartość klucza RSA jest zbyt duża dla oczekiwanego rozmiaru.");
        }

        byte[] trimmed = new byte[targetLength];
        Buffer.BlockCopy(data, offset, trimmed, 0, targetLength);
        return trimmed;
    }

    #endregion

    /// <summary>
    /// Tworzy instancję RSA z klucza publicznego zakodowanego w PEM, obsługującą szyfrowanie OAEP-SHA256.
    /// Na .NET Framework 4.8 <see cref="RSA.Create()"/> zwraca <see cref="RSACryptoServiceProvider"/>,
    /// który nie obsługuje <see cref="RSAEncryptionPadding.OaepSHA256"/>.
    /// Ta metoda używa <see cref="RSACng"/>.
    /// </summary>
    /// <param name="pem">Klucz publiczny RSA zakodowany w PEM.</param>
    /// <returns>Instancja RSA obsługująca OAEP-SHA256.</returns>
    public static RSA CreateFromPemWithOaepSupport(string pem)
    {
        // Parsuj PEM, aby uzyskać RSAParameters za pomocą tymczasowej instancji RSA
        using (RSA temp = RSA.Create())
        {
            temp.ImportFromPem(pem);
            RSAParameters parameters = temp.ExportParameters(false);

            // RSACng obsługuje OaepSHA256 na .NET Framework 4.6.1+
            RSACng cng = new RSACng();
            try
            {
                cng.ImportParameters(parameters);
                return cng;
            }
            catch
            {
                cng.Dispose();
                throw;
            }
        }
    }
}
#endif
