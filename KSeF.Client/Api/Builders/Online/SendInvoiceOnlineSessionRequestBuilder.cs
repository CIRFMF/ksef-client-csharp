using KSeF.Client.Validation;
using KSeF.Client.Core.Models.Sessions;

namespace KSeF.Client.Api.Builders.Online
{
    /// <summary>
    /// Buduje żądanie wysłania faktury w ramach sesji online w KSeF.
    /// </summary>
    public interface ISendInvoiceOnlineSessionRequestBuilder
    {
        /// <summary>
        /// Ustawia hash i rozmiar oryginalnego dokumentu faktury.
        /// </summary>
        /// <param name="documentHash">Skrót kryptograficzny dokumentu faktury (np. SHA-256).</param>
        /// <param name="documentSize">Rozmiar dokumentu faktury w bajtach. Nie może być ujemny.</param>
        /// <returns>
        /// Interfejs pozwalający ustawić hash zaszyfrowanej faktury.
        /// </returns>
        ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash WithInvoiceHash(string documentHash, long documentSize);

        /// <summary>
        /// Waliduje NIP podmiotu 1.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot1Nip(string nip);

        /// <summary>
        /// Waliduje NIP podmiotu 2.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot2Nip(string nip);

        /// <summary>
        /// Waliduje NIP podmiotu 3.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot3Nip(string nip);

        /// <summary>
        /// Waliduje NIP podmiotu upoważnionego, jeśli występuje.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiotUpowaznionyNip(string nip);

        /// <summary>
        /// #617: Ustawia wewnętrzny identyfikator NIP podmiotu 3 (jeśli identyfikator występuje).
        /// Dodano weryfikację sumy kontrolnej NIP w InternalId (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot3InternalId(string internalId);
    }

    /// <summary>
    /// Etap budowy żądania po ustawieniu hash'a faktury.
    /// </summary>
    public interface ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash
    {
        /// <summary>
        /// Ustawia hash i rozmiar zaszyfrowanego dokumentu faktury.
        /// </summary>
        /// <param name="encryptedDocumentHash">Skrót kryptograficzny zaszyfrowanej faktury.</param>
        /// <param name="encryptedDocumentSize">Rozmiar zaszyfrowanego dokumentu w bajtach. Nie może być ujemny.</param>
        /// <returns>
        /// Interfejs pozwalający ustawić zaszyfrowaną treść faktury.
        /// </returns>
        ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash WithEncryptedDocumentHash(string encryptedDocumentHash, long encryptedDocumentSize);
    }

    /// <summary>
    /// Etap budowy żądania po ustawieniu skrótu (hasha) zaszyfrowanego dokumentu.
    /// </summary>
    public interface ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash
    {
        /// <summary>
        /// Ustawia zaszyfrowaną treść dokumentu faktury.
        /// </summary>
        /// <param name="encryptedDocumentContent">
        /// Zaszyfrowana zawartość faktury w formacie wymaganym przez KSeF.
        /// </param>
        /// <returns>
        /// Interfejs pozwalający ustawić dodatkowe opcje i zbudować żądanie.
        /// </returns>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithEncryptedDocumentContent(string encryptedDocumentContent);
    }

    /// <summary>
    /// Ostatni etap budowy żądania wysłania faktury online.
    /// </summary>
    public interface ISendInvoiceOnlineSessionRequestBuilderBuild
    {
        /// <summary>
        /// Ustawia hash faktury korygowanej, jeśli wysyłany dokument jest korektą.
        /// </summary>
        /// <param name="hashOfCorrectedInvoice">
        /// Hash faktury korygowanej. Nie może być pusty, jeżeli jest ustawiany.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithHashOfCorrectedInvoice(string hashOfCorrectedInvoice);

        /// <summary>
        /// Włącza lub wyłącza tryb offline przy wysyłce faktury.
        /// </summary>
        /// <param name="offlineMode">
        /// Wartość true włącza tryb offline, false pozostawia tryb domyślny.
        /// </param>
        /// <returns>Ten sam interfejs, umożliwiający dalsze ustawienia lub zbudowanie żądania.</returns>
        ISendInvoiceOnlineSessionRequestBuilderBuild WithOfflineMode(bool offlineMode);

        /// <summary>
        /// Tworzy finalne żądanie wysłania faktury w ramach sesji online.
        /// </summary>
        /// <returns>
        /// Obiekt <see cref="SendInvoiceRequest"/> gotowy do wysłania do KSeF.
        /// </returns>
        SendInvoiceRequest Build();
    }

    /// <inheritdoc />
    public class SendInvoiceOnlineSessionRequestBuilderImpl
        : ISendInvoiceOnlineSessionRequestBuilder
        , ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash
        , ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash
        , ISendInvoiceOnlineSessionRequestBuilderBuild
    {        
        private readonly bool _nonProdEnvironment;

        private string _documentHash;
        private long _documentSize;
        private string _encryptedDocumentHash;
        private long _encryptedDocumentSize;
        private string _encryptedDocumentContent;
        private string _hashOfCorrectedInvoice;
        private bool _offlineMode;

        
        private string _podmiot1Nip;
        private string _podmiot2Nip;
        private string _podmiot3Nip;
        private string _podmiotUpowaznionyNip;
        private string _podmiot3InternalId;

        /// <summary>
        /// Konstruktor buildera.
        /// 
        /// Domyślnie nonProdEnvironment = false => traktujemy jak PROD (walidacja checksum NIP włączona).
        /// </summary>
        public SendInvoiceOnlineSessionRequestBuilderImpl(bool nonProdEnvironment = false)
        {
            _nonProdEnvironment = nonProdEnvironment;
        }        

        /// <summary>
        /// Tworzy nową instancję buildera żądania wysłania faktury online.
        /// 
        /// Domyślnie: nonProdEnvironment = false => PROD-like => walidacja checksum włączona.
        /// </summary>
        /// <param name="nonProdEnvironment">
        /// Jeśli true => NON-PROD => wyłącza walidację checksum NIP.
        /// Jeśli false => PROD-like => włącza walidację checksum NIP.
        /// </param>
        /// <returns>Interfejs startowy buildera.</returns>
        public static ISendInvoiceOnlineSessionRequestBuilder Create(bool nonProdEnvironment = false)
            => new SendInvoiceOnlineSessionRequestBuilderImpl(nonProdEnvironment);

        private bool ShouldValidateChecksums() => !_nonProdEnvironment;

        private void ValidateNipChecksumIfNeeded(string nip, string fieldName)
        {
            if (!ShouldValidateChecksums())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(nip))
            {
                throw new ArgumentException($"{fieldName} jest wymagany.");
            }

            if (!IdentifierValidators.IsValidNip(nip))
            {
                throw new ArgumentException($"{fieldName} ma nieprawidłowy format lub sumę kontrolną.");
            }
        }

        private void ValidateInternalIdIfNeeded(string internalId, string fieldName)
        {
            if (!ShouldValidateChecksums())
            {
                return;
            }

            // "jeśli identyfikator występuje"
            if (string.IsNullOrWhiteSpace(internalId))
            {
                return;
            }

            // internalId ma swój format w repo (np. 123-456-7890124)
            if (!IdentifierValidators.IsValidInternalId(internalId))
            {
                throw new ArgumentException($"{fieldName} ma nieprawidłowy format lub sumę kontrolną.");
            }
        }

        /// <inheritdoc />
        public ISendInvoiceOnlineSessionRequestBuilderWithInvoiceHash WithInvoiceHash(string documentHash, long documentSize)
        {
            if (string.IsNullOrWhiteSpace(documentHash) || documentSize < 0)
            {
                throw new ArgumentException("Parametry InvoiceHash są nieprawidłowe.");
            }

            _documentHash = documentHash;
            _documentSize = documentSize;
            return this;
        }

        /// <inheritdoc />
        public ISendInvoiceOnlineSessionRequestBuilderWithEncryptedDocumentHash WithEncryptedDocumentHash(string encryptedDocumentHash, long encryptedDocumentSize)
        {
            if (string.IsNullOrWhiteSpace(encryptedDocumentHash) || encryptedDocumentSize < 0)
            {
                throw new ArgumentException("Parametry EncryptedInvoiceHash są nieprawidłowe.");
            }

            _encryptedDocumentHash = encryptedDocumentHash;
            _encryptedDocumentSize = encryptedDocumentSize;
            return this;
        }

        /// <inheritdoc />
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithEncryptedDocumentContent(string encryptedDocumentContent)
        {
            if (string.IsNullOrWhiteSpace(encryptedDocumentContent))
            {
                throw new ArgumentException("EncryptedInvoiceContent nie może być puste ani null.");
            }

            _encryptedDocumentContent = encryptedDocumentContent;
            return this;
        }

        /// <inheritdoc />
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithHashOfCorrectedInvoice(string hashOfCorrectedInvoice)
        {
            if (string.IsNullOrWhiteSpace(hashOfCorrectedInvoice))
            {
                throw new ArgumentException("HashOfCorrectedInvoice nie może być puste ani null.");
            }

            _hashOfCorrectedInvoice = hashOfCorrectedInvoice;
            return this;
        }

        /// <inheritdoc />
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithOfflineMode(bool offlineMode)
        {
            _offlineMode = offlineMode;
            return this;
        }

        /// <summary>
        /// Waliduje NIP podmiotu 1.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot1Nip(string nip)
        {
            ValidateNipChecksumIfNeeded(nip, nameof(_podmiot1Nip));
            _podmiot1Nip = nip;
            return this;
        }

        /// <summary>
        /// Waliduje NIP podmiotu 2.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot2Nip(string nip)
        {
            ValidateNipChecksumIfNeeded(nip, nameof(_podmiot2Nip));
            _podmiot2Nip = nip;
            return this;
        }

        /// <summary>
        /// Waliduje NIP podmiotu 3.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot3Nip(string nip)
        {
            ValidateNipChecksumIfNeeded(nip, nameof(_podmiot3Nip));
            _podmiot3Nip = nip;
            return this;
        }

        /// <summary>
        /// Waliduje NIP podmiotu upoważnionego, jeśli występuje.
        /// Dodano weryfikację sumy kontrolnej NIP (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiotUpowaznionyNip(string nip)
        {
            // "jeśli występuje"
            if (!string.IsNullOrWhiteSpace(nip))
            {
                ValidateNipChecksumIfNeeded(nip, nameof(_podmiotUpowaznionyNip));
            }

            _podmiotUpowaznionyNip = nip;
            return this;
        }

        /// <summary>
        /// #617: Ustawia wewnętrzny identyfikator NIP podmiotu 3 (jeśli identyfikator występuje).
        /// Dodano weryfikację sumy kontrolnej NIP w InternalId (tylko PROD-like).
        /// Walidacja wyłącznie w builderze.
        /// </summary>
        public ISendInvoiceOnlineSessionRequestBuilderBuild WithPodmiot3InternalId(string internalId)
        {
            ValidateInternalIdIfNeeded(internalId, nameof(_podmiot3InternalId));
            _podmiot3InternalId = internalId;
            return this;
        }

        /// <inheritdoc />
        public SendInvoiceRequest Build()
        {
            if (string.IsNullOrWhiteSpace(_documentHash))
            {
                throw new InvalidOperationException("InvoiceHash jest wymagany.");
            }

            if (string.IsNullOrWhiteSpace(_encryptedDocumentHash))
            {
                throw new InvalidOperationException("EncryptedInvoiceHash jest wymagany.");
            }

            if (string.IsNullOrWhiteSpace(_encryptedDocumentContent))
            {
                throw new InvalidOperationException("EncryptedInvoiceContent jest wymagany.");
            }

            return new SendInvoiceRequest
            {
                InvoiceHash = _documentHash,
                InvoiceSize = _documentSize,
                EncryptedInvoiceHash = _encryptedDocumentHash,
                EncryptedInvoiceSize = _encryptedDocumentSize,
                EncryptedInvoiceContent = _encryptedDocumentContent,
                HashOfCorrectedInvoice = _hashOfCorrectedInvoice,
                OfflineMode = _offlineMode
            };
        }
    }

    /// <summary>
    /// Udostępnia metodę pomocniczą do tworzenia buildera żądania wysłania faktury online.
    /// </summary>
    public static class SendInvoiceOnlineSessionRequestBuilder
    {
        /// <summary>
        /// Tworzy nowy builder żądania wysłania faktury w ramach sesji online.
        /// 
        /// Domyślnie: nonProdEnvironment = false => PROD-like => walidacja checksum NIP włączona.
        /// Aby wyłączyć walidację na non-prod, użyj Create(true).
        /// </summary>
        /// <param name="nonProdEnvironment">
        /// Jeśli true => NON-PROD => wyłącza walidację checksum NIP.
        /// Jeśli false => PROD-like => włącza walidację checksum NIP.
        /// </param>
        /// <returns>Interfejs startowy buildera.</returns>
        public static ISendInvoiceOnlineSessionRequestBuilder Create(bool nonProdEnvironment)
            => SendInvoiceOnlineSessionRequestBuilderImpl.Create(nonProdEnvironment);

        /// <summary>
        /// Tworzy nowy builder żądania wysłania faktury w ramach sesji online.
        /// 
        /// Domyślnie: nonProdEnvironment = false => PROD-like => walidacja checksum NIP włączona.
        /// </summary>
        /// <returns>Interfejs startowy buildera.</returns>
        public static ISendInvoiceOnlineSessionRequestBuilder Create() =>
            SendInvoiceOnlineSessionRequestBuilderImpl.Create(false);
    }
}