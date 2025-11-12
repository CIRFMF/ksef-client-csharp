using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Tests.Utils;
using System.IO.Compression;
using System.Security.Cryptography;

namespace KSeF.Client.Tests.Features;

/// <summary>
/// Testy integracyjne dla funkcjonalności wysyłki wsadowej faktur do systemu KSeF.
/// Weryfikują proces otwierania sesji, wysyłania zaszyfrowanych paczek i zamykania sesji wsadowej.
/// </summary>
[CollectionDefinition("Batch.feature")]
[Trait("Category", "Features")]
[Trait("Features", "batch.feature")]
public class BatchTests : KsefIntegrationTestBase
{
    private const int DefaultInvoiceCount = 5;
    private const int MultiPartCount = 5;
    private const int ExceedingInvoiceCount = 10001;
    private const int ExceedingPartCount = 51;
    // Jednostki komunikowane i egzekwowane przez API w MB/GB (dziesiętne: 1 MB = 1_000_000 B, 1 GB = 1_000_000_000 B).
    private const long PaddingSafetyMarginInBytes = 100_000; // +100 KB zapasu (dziesiętne)
    // Efektywny limit na część przed szyfrowaniem: 100 MB = 100_000_000 bajtów
    private const long MaxPartSizeInBytes = 100_000_000L; // 100 MB (dziesiętnie)
    // Efektywny limit całej paczki: 5 GB = 5_000_000_000 bajtów
    private const long MaxTotalPackageSizeInBytes = 5_000_000_000L; // 5 GB (dziesiętnie)
    private const long ExceededTotalPackageSizeInBytes = MaxTotalPackageSizeInBytes + 1; // przekroczenie o 1 bajt
    private const int EncryptionKeySize = 256; // bytes dla RSA
    private const int InitializationVectorSize = 16; // bytes

    private string authenticatedNip;
    private string accessToken;

    public BatchTests()
    {
        authenticatedNip = MiscellaneousUtils.GetRandomNip();
        accessToken = AuthenticationUtils.AuthenticateAsync(AuthorizationClient, SignatureService, authenticatedNip)
            .GetAwaiter()
            .GetResult()
            .AccessToken.Token;
    }

    /// <summary>
    /// Weryfikuje poprawne wysłanie dokumentów w jednoczęściowej paczce (scenariusz pozytywny).
    /// Oczekuje pomyślnego przetworzenia wszystkich faktur i możliwości pobrania UPO.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Wysłanie dokumentów w jednoczęściowej paczce (happy path)")]
    public async Task Batch_SendSinglePart_ShouldSucceed(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);

        // Generacja klucza AES-256 i IV zgodnie z wymaganiami KSeF
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // Szyfrowanie ZIP i podział na części (tutaj: 1 część)
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: 1);

        // Act
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            encryptionData,
            encryptedParts,
            systemCode);
        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, encryptedParts);
        await BatchUtils.CloseBatchAsync(KsefClient, openSessionResponse.ReferenceNumber, accessToken);

        // Assert
        // Oczekiwanie aż system KSeF przetworzy wszystkie faktury
        SessionStatusResponse sessionStatus = await BatchUtils.WaitForBatchStatusAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            accessToken);

        Assert.True(sessionStatus.Status.Code != BatchSessionCodeResponse.Processing);
        Assert.Equal(DefaultInvoiceCount, sessionStatus.SuccessfulInvoiceCount);

        SessionInvoicesResponse sessionInvoices = await BatchUtils.GetSessionInvoicesAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            accessToken);
        Assert.NotNull(sessionInvoices);
        Assert.NotEmpty(sessionInvoices.Invoices);

        // Weryfikacja możliwości pobrania UPO (Urzędowego Poświadczenia Odbioru) dla pierwszej faktury
        SessionInvoice firstInvoice = sessionInvoices.Invoices.First();
        string upoDocument = await BatchUtils.GetSessionInvoiceUpoByKsefNumberAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            firstInvoice.KsefNumber,
            accessToken);

        Assert.NotNull(upoDocument);
    }

    /// <summary>
    /// Weryfikuje odrzucenie faktur z niepoprawnym NIP (scenariusz negatywny).
    /// Oczekuje statusu błędu i zliczenia wszystkich faktur jako niepoprawnych.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Wysłanie dokumentów w jednoczęściowej paczce z niepoprawnym NIP w fakturach (negatywny)")]
    public async Task Batch_SendWithIncorrectNip_ShouldFail(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        // Generowanie faktury z NIP-em innym niż użyty do uwierzytelnienia
        string unauthorizedNip = MiscellaneousUtils.GetRandomNip();

        List<(string FileName, byte[] Content)> invoicesWithInvalidNip = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            unauthorizedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoicesWithInvalidNip, CryptographyService);
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: 1);

        // Act
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            encryptionData,
            encryptedParts,
            systemCode);
        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, encryptedParts);
        await BatchUtils.CloseBatchAsync(KsefClient, openSessionResponse.ReferenceNumber, accessToken);

        // Assert
        SessionStatusResponse sessionStatus = await BatchUtils.WaitForBatchStatusAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            accessToken);

        // Kod 445 Błąd weryfikacji, brak poprawnych faktur
        Assert.True(sessionStatus.Status.Code == BatchSessionCodeResponse.NoValidInvoices);
        Assert.Equal(DefaultInvoiceCount, sessionStatus.FailedInvoiceCount);
    }

    /// <summary>
    /// Weryfikuje odrzucenie paczki przekraczającej limit 10000 faktur.
    /// Oczekuje zwrócenia błędu o przekroczonym limicie.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml", ExceedingInvoiceCount)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", ExceedingInvoiceCount)]
    [Trait("Scenario", "Przekroczona liczba faktur > 10000 (MaxInvoiceCountLimit)")]
    public async Task Batch_SendWithExceededInvoiceLimit_ShouldFail(
        SystemCode systemCode,
        string invoiceTemplatePath,
        int invoiceCount)
    {
        // Arrange
        // Generowanie liczby faktur zdefiniowanej w ExceedingInvoiceCount (warunek graniczny), co przekracza limit API KSeF wynoszący MaxInvoiceCountLimit
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            invoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: 1);

        // Act
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            encryptionData,
            encryptedParts,
            systemCode);
        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, encryptedParts);
        await BatchUtils.CloseBatchAsync(KsefClient, openSessionResponse.ReferenceNumber, accessToken);

        // Assert
        SessionStatusResponse sessionStatus = await BatchUtils.WaitForBatchStatusAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            accessToken);

        // Kod 420 Przekroczony limit faktur w sesji
        Assert.Equal(BatchSessionCodeResponse.InvoiceLimitExceeded, sessionStatus.Status.Code);
    }

    /// <summary>
    /// Weryfikuje odrzucenie paczki przekraczającej maksymalny rozmiar 5 GB.
    /// Oczekuje wyjątku podczas próby otwarcia sesji z fileSize > 5_000_000_000 bajtów (MaxTotalPackageSizeInBytes).
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Rozmiar całej paczki (fileSize) > 5GB (MaxTotalPackageSizeInBytes)")]
    public async Task Batch_SendWithExceededTotalPackageSize_ShouldFail(
        SystemCode systemCode,
        string invoiceTemplatePath)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);

        // Modyfikacja metadaty aby symulować paczkę o rozmiarze przekraczającym 5 GB (dziesiętnie)
        FileMetadata manipulatedMetadata = new FileMetadata
        {
            FileSize = ExceededTotalPackageSizeInBytes,
            HashSHA = zipMetadata.HashSHA
        };

        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: 50);

        // Act & Assert
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            manipulatedMetadata,
            encryptionData,
            encryptedParts,
            systemCode);

        // API KSeF powinno odrzucić żądanie ze względu na przekroczony limit fileSize
        await Assert.ThrowsAnyAsync<KsefApiException>(async () =>
            await BatchUtils.OpenBatchAsync(KsefClient, openSessionRequest, accessToken));
    }

    /// <summary>
    /// Weryfikuje odrzucenie paczki przekraczającej limit rozmiaru 100 MB (przed szyfrowaniem).
    /// Jeśli API nie zwróci błędu przy zamykaniu (202 Accepted), weryfikujemy status sesji.
    /// Akceptujemy dwa możliwe wyniki: odrzucenie całej paczki przed przetwarzaniem (0/0) albo odrzucenie wszystkich faktur (0/5).
    /// Dla stabilności zwiększamy czas oczekiwania na finalny status (do 3 minut).
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Rozmiar part (przed szyfrowaniem) > 100MB")]
    public async Task Batch_SendWithExceededPartSize_ShouldFail(
        SystemCode systemCode,
        string invoiceTemplatePath)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);

        // Dodanie sztucznego wypełnienia, aby paczka przekroczyła efektywny limit (100 MB)
        byte[] paddedZipBytes = AddPaddingToZipArchive(zipBytes, MaxPartSizeInBytes);
        // Metadane muszą odpowiadać rozmiarowi PRZED szyfrowaniem, więc licz je po dodaniu paddingu
        FileMetadata paddedZipMetadata = CryptographyService.GetMetaData(paddedZipBytes);

        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            paddedZipBytes,
            encryptionData,
            CryptographyService,
            partCount: 1);

        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            paddedZipMetadata,
            encryptionData,
            encryptedParts,
            systemCode);

        // Act: open i upload mogą przejść, limit może ujawnić się dopiero po przetworzeniu
        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);

        await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, encryptedParts);

        bool closedWithError = false;
        try
        {
            await BatchUtils.CloseBatchAsync(KsefClient, openSessionResponse.ReferenceNumber, accessToken);
        }
        catch (KsefApiException)
        {
            closedWithError = true;
        }

        // Assert: jeżeli nie było wyjątku przy zamykaniu, to sesja powinna zakończyć się niepowodzeniem.
        // Akceptowany rezultat: (0 sukcesów, 5 błędów) lub (0 sukcesów, 0 błędów) w przypadku odrzucenia całej paczki.
        if (!closedWithError)
        {
            // Zwiększone oczekiwanie na finalny status
            SessionStatusResponse sessionStatus = await BatchUtils.WaitForBatchStatusAsync(
                KsefClient,
                openSessionResponse.ReferenceNumber,
                accessToken,
                sleepTime: 500,
                maxAttempts: 360);

            Assert.NotNull(sessionStatus);
            Assert.True(sessionStatus.Status.Code != BatchSessionCodeResponse.Processing);

            int success = sessionStatus.SuccessfulInvoiceCount ?? 0;
            int failed = sessionStatus.FailedInvoiceCount ?? 0;

            Assert.Equal(0, success);
            Assert.True(failed == 0 || failed == DefaultInvoiceCount);
        }
    }

    /// <summary>
    /// Weryfikuje wykrycie próby zamknięcia sesji bez wysłania wszystkich zadeklarowanych części.
    /// Oczekuje wyjątku podczas wysyłania niepełnego zestawu części.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Zamknięcie sesji bez wysłania wszystkich części")]
    public async Task Batch_CloseWithoutAllParts_ShouldFail(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // Deklaracja 5 (MultiPartCount) części w żądaniu
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: MultiPartCount);

        // Act
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            encryptionData,
            encryptedParts,
            systemCode);
        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);

        // Assert
        // Próba wysłania tylko pierwszej części, mimo że zadeklarowano 5
        // API powinno wykryć niezgodność i odrzucić żądanie
        List<BatchPartSendingInfo> incompletePartsList = new List<BatchPartSendingInfo> { encryptedParts[0] };

        await Assert.ThrowsAnyAsync<AggregateException>(async () =>
            await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, incompletePartsList));
    }

    /// <summary>
    /// Weryfikuje odrzucenie paczki z liczbą części przekraczającą maksymalny limit 50.
    /// Oczekuje wyjątku podczas próby otwarcia sesji.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml", ExceedingPartCount)]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml", ExceedingPartCount)]
    [Trait("Scenario", "Próba wysłania z przekroczoną liczbą części (>50)")]
    public async Task Batch_SendWithExceededPartCount_ShouldFail(
        SystemCode systemCode,
        string invoiceTemplatePath,
        int partCount)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();

        // Próba podziału paczki na 51 części, co przekracza limit API wynoszący 50
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: partCount);

        // Act & Assert
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            encryptionData,
            encryptedParts,
            systemCode);

        // API KSeF odrzuca żądanie z przekroczoną liczbą części
        await Assert.ThrowsAnyAsync<KsefApiException>(async () =>
            await BatchUtils.OpenBatchAsync(KsefClient, openSessionRequest, accessToken));
    }

    /// <summary>
    /// Weryfikuje wykrycie nieprawidłowo zaszyfrowanego klucza symetrycznego.
    /// Oczekuje błędu deszyfrowania po przetworzeniu sesji przez system KSeF.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Wysłanie paczki z nieprawidłowo zaszyfrowanym kluczem")]
    public async Task Batch_SendWithInvalidEncryptedKey_ShouldFail(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: 1);

        // Podmiana prawidłowego klucza zaszyfrowanego RSA na losowe dane
        // Klucz musi być zaszyfrowany RSA-OAEP kluczem publicznym MF, więc losowe dane nie będą poprawne
        byte[] corruptedEncryptedKey = new byte[EncryptionKeySize];
        RandomNumberGenerator.Fill(corruptedEncryptedKey);

        EncryptionData corruptedEncryptionData = new EncryptionData
        {
            CipherKey = encryptionData.CipherKey,
            CipherIv = encryptionData.CipherIv,
            EncryptionInfo = new EncryptionInfo
            {
                EncryptedSymmetricKey = Convert.ToBase64String(corruptedEncryptedKey),
                InitializationVector = encryptionData.EncryptionInfo.InitializationVector
            }
        };

        // Act
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            corruptedEncryptionData,
            encryptedParts,
            systemCode);

        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, encryptedParts);
        await BatchUtils.CloseBatchAsync(KsefClient, openSessionResponse.ReferenceNumber, accessToken);

        // Assert
        SessionStatusResponse sessionStatus = await BatchUtils.WaitForBatchStatusAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            accessToken);

        // Kod 415 Błąd odszyfrowania dostarczonego klucza
        Assert.Equal(BatchSessionCodeResponse.KeyDecryptionError, sessionStatus.Status.Code);
    }

    /// <summary>
    /// Weryfikuje wykrycie uszkodzonych zaszyfrowanych danych.
    /// Oczekuje błędu deszyfrowania po przetworzeniu sesji przez system KSeF.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Wysłanie paczki z nieprawidłowo zaszyfrowanymi danymi")]
    public async Task Batch_SendWithCorruptedEncryptedData_ShouldFail(SystemCode systemCode, string invoiceTemplatePath)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: 1);

        // Celowe uszkodzenie zaszyfrowanych danych poprzez inwersję bitów w środkowej pozycji
        // To symuluje uszkodzenie podczas transmisji lub manipulację danymi
        byte[] corruptedData = encryptedParts[0].Data.ToArray();
        int corruptionPosition = corruptedData.Length / 2;
        corruptedData[corruptionPosition] ^= 0xFF;

        BatchPartSendingInfo corruptedPart = new BatchPartSendingInfo
        {
            OrdinalNumber = encryptedParts[0].OrdinalNumber,
            Data = corruptedData,
            Metadata = encryptedParts[0].Metadata
        };

        // Act
        List<BatchPartSendingInfo> corruptedPartsList = new List<BatchPartSendingInfo> { corruptedPart };
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            encryptionData,
            corruptedPartsList,
            systemCode);
        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, corruptedPartsList);
        await BatchUtils.CloseBatchAsync(KsefClient, openSessionResponse.ReferenceNumber, accessToken);

        // Assert
        SessionStatusResponse sessionStatus = await BatchUtils.WaitForBatchStatusAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            accessToken);

        // Kod 405 Błąd weryfikacji poprawności dostarczonych elementów paczki
        Assert.Equal(BatchSessionCodeResponse.ValidationError, sessionStatus.Status.Code);
    }

    /// <summary>
    /// Weryfikuje wykrycie nieprawidłowego wektora inicjującego (IV).
    /// Oczekuje błędu deszyfrowania po przetworzeniu sesji przez system KSeF.
    /// </summary>
    [Theory]
    [InlineData(SystemCode.FA2, "invoice-template-fa-2.xml")]
    [InlineData(SystemCode.FA3, "invoice-template-fa-3.xml")]
    [Trait("Scenario", "Wysłanie paczki z nieprawidłowym wektorem inicjującym")]
    public async Task Batch_SendWithInvalidInitializationVector_ShouldFail(
        SystemCode systemCode,
        string invoiceTemplatePath)
    {
        // Arrange
        List<(string FileName, byte[] Content)> invoices = BatchUtils.GenerateInvoicesInMemory(
            DefaultInvoiceCount,
            authenticatedNip,
            invoiceTemplatePath);

        (byte[] zipBytes, FileMetadata zipMetadata) = BatchUtils.BuildZip(invoices, CryptographyService);
        EncryptionData encryptionData = CryptographyService.GetEncryptionData();
        List<BatchPartSendingInfo> encryptedParts = BatchUtils.EncryptAndSplit(
            zipBytes,
            encryptionData,
            CryptographyService,
            partCount: 1);

        // Generowanie losowego IV zamiast użycia tego, który wykorzystano przy szyfrowaniu
        // W AES-CBC poprawny IV jest kluczowy dla odszyfrowania pierwszego bloku
        byte[] corruptedInitializationVector = new byte[InitializationVectorSize];
        RandomNumberGenerator.Fill(corruptedInitializationVector);

        EncryptionData corruptedEncryptionData = new EncryptionData
        {
            CipherKey = encryptionData.CipherKey,
            CipherIv = encryptionData.CipherIv,
            EncryptionInfo = new EncryptionInfo
            {
                EncryptedSymmetricKey = encryptionData.EncryptionInfo.EncryptedSymmetricKey,
                InitializationVector = Convert.ToBase64String(corruptedInitializationVector)
            }
        };

        // Act
        OpenBatchSessionRequest openSessionRequest = BatchUtils.BuildOpenBatchRequest(
            zipMetadata,
            corruptedEncryptionData,
            encryptedParts,
            systemCode);

        OpenBatchSessionResponse openSessionResponse = await BatchUtils.OpenBatchAsync(
            KsefClient,
            openSessionRequest,
            accessToken);
        await BatchUtils.SendBatchPartsAsync(KsefClient, openSessionResponse, encryptedParts);
        await BatchUtils.CloseBatchAsync(KsefClient, openSessionResponse.ReferenceNumber, accessToken);

        // Assert
        SessionStatusResponse sessionStatus = await BatchUtils.WaitForBatchStatusAsync(
            KsefClient,
            openSessionResponse.ReferenceNumber,
            accessToken);

        // Kod 430 Błąd dekompresji pierwotnego archiwum
        Assert.Equal(BatchSessionCodeResponse.ArchiveDecompressionError, sessionStatus.Status.Code);
    }

    /// <summary>
    /// Dodaje wypełnienie (padding) do archiwum ZIP, aby osiągnąć minimalny wymagany rozmiar.
    /// Używane do testowania limitów rozmiaru paczki.
    /// Wypełnienie składa się z losowych danych w pliku bez kompresji, aby zachować kontrolę nad rozmiarem.
    /// </summary>
    /// <param name="zipBytes">Oryginalne bajty archiwum ZIP.</param>
    /// <param name="minimumSizeInBytes">Minimalny wymagany rozmiar w bajtach (przed szyfrowaniem).</param>
    /// <returns>Archiwum ZIP z dodanym wypełnieniem.</returns>
    private static byte[] AddPaddingToZipArchive(byte[] zipBytes, long minSizeBytes)
    {
        using MemoryStream memoryStream = new MemoryStream();
        memoryStream.Write(zipBytes, 0, zipBytes.Length);

        using ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Update, leaveOpen: true);

        if (memoryStream.Length < minSizeBytes)
        {
            long paddingSize = (minSizeBytes - memoryStream.Length) + PaddingSafetyMarginInBytes; // +100 KB zapasu
            ZipArchiveEntry paddingEntry = archive.CreateEntry("padding.bin", CompressionLevel.NoCompression);
            using Stream entryStream = paddingEntry.Open();
            RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create();
            byte[] buffer = new byte[1_000_000];
            long bytesWritten = 0;

            while (bytesWritten < paddingSize)
            {
                randomGenerator.GetBytes(buffer);
                int bytesToWrite = (int)Math.Min(buffer.Length, paddingSize - bytesWritten);
                entryStream.Write(buffer, 0, bytesToWrite);
                bytesWritten += bytesToWrite;
            }
        }

        archive.Dispose();
        return memoryStream.ToArray();
    }
}
