namespace KSeF.Client.Core.Models.Sessions
{
    public class EncryptionInfo
    {
        public string EncryptedSymmetricKey { get; set; }
        public string InitializationVector { get; set; }
        /// <summary>
        /// Identyfikator klucza publicznego użytego do zaszyfrowania klucza symetrycznego
        /// (skrót SHA-256 z DER SubjectPublicKeyInfo, zakodowany w Base64).
        /// </summary>
        #nullable enable
        public string? PublicKeyId { get; set; }
    }

}
