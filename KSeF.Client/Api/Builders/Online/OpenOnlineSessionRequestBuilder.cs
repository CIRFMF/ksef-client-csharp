namespace KSeF.Client.Api.Builders.Online
{
    using KSeF.Client.Core.Models.Sessions.OnlineSession;
    using KSeF.Client.Core.Models.Sessions;

    /// <summary>
    /// Buduje żądanie otwarcia sesji online w KSeF.
    /// </summary>
    public interface IOpenOnlineSessionRequestBuilder
    {
        /// <summary>
        /// Ustawia kod formularza, który będzie używany w sesji online.
        /// </summary>
        /// <param name="systemCode">Kod systemowy formularza zgodny ze specyfikacją KSeF.</param>
        /// <param name="schemaVersion">Wersja schematu formularza.</param>
        /// <param name="value">Wartość identyfikująca formularz.</param>
        /// <returns>
        /// Interfejs pozwalający ustawić dane szyfrowania.
        /// </returns>
        IOpenOnlineSessionRequestBuilderWithFormCode WithFormCode(string systemCode, string schemaVersion, string value);
    }

    /// <summary>
    /// Etap budowy żądania, w którym ustawiono już kod formularza.
    /// </summary>
    public interface IOpenOnlineSessionRequestBuilderWithFormCode
    {
        /// <summary>
        /// Ustawia dane szyfrowania używane dla sesji online.
        /// </summary>
        /// <param name="encryptedSymmetricKey">Zaszyfrowany klucz symetryczny użyty do szyfrowania danych.</param>
        /// <param name="initializationVector">Wektor inicjalizujący użyty przy szyfrowaniu.</param>
        /// <param name="publicKeyId">Identyfikator klucza publicznego użytego do zaszyfrowania klucza symetrycznego.</param>
        /// <returns>
        /// Interfejs pozwalający zbudować finalne żądanie.
        /// </returns>
#nullable enable
        IOpenOnlineSessionRequestBuilderWithEncryption WithEncryption(string encryptedSymmetricKey, string initializationVector, string? publicKeyId = null);
#nullable disable 
    }

    /// <summary>
    /// Ostatni etap budowy żądania otwarcia sesji online.
    /// </summary>
    public interface IOpenOnlineSessionRequestBuilderWithEncryption
    {
        /// <summary>
        /// Tworzy obiekt żądania otwarcia sesji online w KSeF.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="OpenOnlineSessionRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        OpenOnlineSessionRequest Build();
    }

    /// <inheritdoc />
    internal sealed class OpenOnlineSessionRequestBuilderImpl
        : IOpenOnlineSessionRequestBuilder
        , IOpenOnlineSessionRequestBuilderWithFormCode
        , IOpenOnlineSessionRequestBuilderWithEncryption
    {
        private FormCode _formCode;
        private readonly EncryptionInfo _encryption = new();

        private OpenOnlineSessionRequestBuilderImpl() { }

        /// <summary>
        /// Tworzy nową implementację buildera sesji online.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        public static IOpenOnlineSessionRequestBuilder Create() => new OpenOnlineSessionRequestBuilderImpl();

        /// <inheritdoc />
        public IOpenOnlineSessionRequestBuilderWithFormCode WithFormCode(string systemCode, string schemaVersion, string value)
        {
            if (string.IsNullOrWhiteSpace(systemCode) || string.IsNullOrWhiteSpace(schemaVersion) || string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Parametry FormCode nie mogą być puste ani null.");
            }

            _formCode = new FormCode
            {
                SystemCode = systemCode,
                SchemaVersion = schemaVersion,
                Value = value
            };
            return this;
        }

        /// <inheritdoc />
#nullable enable
        public IOpenOnlineSessionRequestBuilderWithEncryption WithEncryption(string encryptedSymmetricKey, string initializationVector, string? publicKeyId = null)
        {
            if (string.IsNullOrWhiteSpace(encryptedSymmetricKey) || string.IsNullOrWhiteSpace(initializationVector))
            {
                throw new ArgumentException("Parametry szyfrowania nie mogą być puste ani null.");
            }

            _encryption.EncryptedSymmetricKey = encryptedSymmetricKey;
            _encryption.InitializationVector = initializationVector;
            _encryption.PublicKeyId = publicKeyId;
            return this;
        }
#nullable disable

        /// <inheritdoc />
        public OpenOnlineSessionRequest Build()
        {
            if (_formCode == null)
            {
                throw new InvalidOperationException("FormCode jest wymagany.");
            }

            if (string.IsNullOrWhiteSpace(_encryption.EncryptedSymmetricKey) || string.IsNullOrWhiteSpace(_encryption.InitializationVector))
            {
                throw new InvalidOperationException("Konfiguracja szyfrowania jest niekompletna.");
            }

            return new OpenOnlineSessionRequest
            {
                FormCode = _formCode,
                Encryption = _encryption
            };
        }
    }

    /// <summary>
    /// Udostępnia metodę pomocniczą do tworzenia buildera żądania otwarcia sesji online.
    /// </summary>
    public static class OpenOnlineSessionRequestBuilder
    {
        /// <summary>
        /// Tworzy nowy builder żądania otwarcia sesji online.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        public static IOpenOnlineSessionRequestBuilder Create() =>
            OpenOnlineSessionRequestBuilderImpl.Create();
    }
}