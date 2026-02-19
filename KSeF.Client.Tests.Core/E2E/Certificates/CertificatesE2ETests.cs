using KSeF.Client.Api.Builders.Certificates;
using KSeF.Client.Api.Builders.PersonPermissions;
using KSeF.Client.Api.Builders.X509Certificates;
using KSeF.Client.Core.Exceptions;
using KSeF.Client.Core.Models;
using KSeF.Client.Core.Models.ApiResponses;
using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Core.Models.Certificates;
using KSeF.Client.Core.Models.Permissions;
using KSeF.Client.Core.Models.Permissions.Identifiers;
using KSeF.Client.Core.Models.Permissions.Person;
using KSeF.Client.Tests.Utils;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace KSeF.Client.Tests.Core.E2E.Certificates;

[Collection("CertificateScenarioE2ECollection")]
public class CertificatesE2ETests : TestBase
{
	private const string TestCertificateName = "E2E Test Cert";
	private const int CertificateValidityDays = 1;
	private readonly CertificatesScenarioE2EFixture TestFixture;

	private const string GivenName = "Jan";
	private const string Surname = "Kowalski";
	private const string SerialNumberPrefix = "TEST";
	private const string CommonName = "Jan Kowalski";

	private const string OrganizationName = "Spółka Testowa sp. z o.o.";
	private const string OrganizationCommonName = "Spółka Testowa";

	private const string ExpectedCertificatesLimitExceededExceptionMessage = "25007: Osiągnięto limit dopuszczalnej liczby posiadanych certyfikatów.";
	private const int CertificatesLimitTestPollAsyncMaxAttempts = 10;

	public CertificatesE2ETests()
	{
		TestFixture = new CertificatesScenarioE2EFixture();
	}

	/// <summary>
	/// Logowanie jako właściciel.
	/// Nadanie uprawnień CredentialManage podmiotowi trzeciemu.
	/// Podmiot delegowany przeprowadza podstawowe operacje.
	/// Właściciel odwołuje uprawnienia podmiotowi trzeciemu.
	/// </summary>
	[Fact]
	public async Task GivenGrantedCredentialManagePermissionWhenThirdPartyCreatesAndRevokesCertificateThenCertificateLifecycleCompletesSuccessfully()
	{
		// Przygotuj nip właściciela oraz nip pośrednika
		string ownerNip = MiscellaneousUtils.GetRandomNip();
		string delegateNip = MiscellaneousUtils.GetRandomNip();

		//Zaloguj jako właściciel 
		AuthenticationOperationStatusResponse ownerAuthProcessResponse = await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, ownerNip);
		string ownerAccessToken = ownerAuthProcessResponse.AccessToken.Token;

		#region Nadanie uprawnień CredentialsManage

		GrantPermissionsPersonRequest request = GrantPersonPermissionsRequestBuilder
		.Create()
		.WithSubject(new GrantPermissionsPersonSubjectIdentifier { Type = GrantPermissionsPersonSubjectIdentifierType.Nip, Value = delegateNip })
		.WithPermissions(PersonPermissionType.CredentialsManage)
		.WithDescription("Access for quarterly review")
		.WithSubjectDetails(new PersonPermissionSubjectDetails
		{
			SubjectDetailsType = PersonPermissionSubjectDetailsType.PersonByIdentifier,
			PersonById = new PersonPermissionPersonById
			{
				FirstName = "Anna",
				LastName = "Testowa"
			}
		})
		.Build();

		OperationResponse operationResult = await KsefClient.GrantsPermissionPersonAsync(request, ownerAccessToken);
		Assert.NotNull(operationResult);

		// Poll status operacji nadania uprawnień do momentu zakończenia (200)
		PermissionsOperationStatusResponse operationStatus =
			await AsyncPollingUtils.PollAsync(
				async () => await KsefClient.OperationsStatusAsync(operationResult.ReferenceNumber, ownerAccessToken, CancellationToken).ConfigureAwait(false),
				result => result.Status.Code == OperationStatusCodeResponse.Success,
				delay: TimeSpan.FromMilliseconds(SleepTime),
				maxAttempts: 30,
				cancellationToken: CancellationToken);

		Assert.NotNull(operationStatus);
		Assert.Equal(OperationStatusCodeResponse.Success, operationStatus.Status.Code);

		#endregion nadanie uprawnień CredentialsManage

		#region Zaloguj jako podmiot trzeci w kontekście właściciela

		AuthenticationOperationStatusResponse delegateAuthOperationStatusResponse =
			await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, delegateNip, ownerNip);
		string delegateAccessToken = delegateAuthOperationStatusResponse.AccessToken.Token;

		#endregion

		// Przeprowadź podstawowe operacje
		#region Pobierz limity certyfikatów
		// Act
		CertificateLimitResponse certificateLimitsResponse = await KsefClient.GetCertificateLimitsAsync(delegateAccessToken, CancellationToken);
		TestFixture.Limits = certificateLimitsResponse;

		// Assert
		Assert.NotNull(TestFixture.Limits);
		Assert.True(TestFixture.Limits.CanRequest);
		Assert.NotNull(TestFixture.Limits.Enrollment);
		Assert.NotNull(TestFixture.Limits.Certificate);
		#endregion

		#region Pobierz informacje o zarejestrowanych certyfikatach
		// Act
		CertificateEnrollmentsInfoResponse certificateEnrollmentsInfoResponse = await KsefClient.GetCertificateEnrollmentDataAsync(delegateAccessToken, CancellationToken);
		TestFixture.EnrollmentInfo = certificateEnrollmentsInfoResponse;

		// Assert
		Assert.NotNull(TestFixture.EnrollmentInfo);
		Assert.NotEmpty(TestFixture.EnrollmentInfo.SerialNumber);
		Assert.NotEmpty(TestFixture.EnrollmentInfo.CommonName);
		Assert.NotEmpty(TestFixture.EnrollmentInfo.GivenName);
		Assert.Null(TestFixture.EnrollmentInfo.OrganizationName);
		Assert.NotEmpty(TestFixture.EnrollmentInfo.Surname);
		#endregion

		#region Wyślij zgłoszenie nowe
		// Arrange
		(string csr, string key) = CryptographyService.GenerateCsrWithRsa(TestFixture.EnrollmentInfo, RSASignaturePadding.Pkcs1);
		SendCertificateEnrollmentRequest sendCertificateEnrollmentRequest = SendCertificateEnrollmentRequestBuilder
			.Create()
			.WithCertificateName(TestCertificateName)
			.WithCertificateType(CertificateType.Authentication)
			.WithCsr(csr)
			.WithValidFrom(DateTimeOffset.UtcNow.AddDays(CertificateValidityDays))
			.Build();

		// Act
		CertificateEnrollmentResponse certificateEnrollmentResponse = await KsefClient.SendCertificateEnrollmentAsync(
		sendCertificateEnrollmentRequest,
		delegateAccessToken,
		CancellationToken
		);
		TestFixture.EnrollmentReference = certificateEnrollmentResponse.ReferenceNumber;

		// Assert
		Assert.False(string.IsNullOrWhiteSpace(TestFixture.EnrollmentReference));
		Assert.False(string.IsNullOrWhiteSpace(TestFixture.EnrollmentReference));
		#endregion

		#region Sprawdź status rejestracji
		// Act: czekaj aż status będzie 200
		CertificateEnrollmentStatusResponse certificateEnrollmentStatusResponse =
			await AsyncPollingUtils.PollAsync(
				async () => await KsefClient.GetCertificateEnrollmentStatusAsync(
					TestFixture.EnrollmentReference,
					delegateAccessToken,
					CancellationToken).ConfigureAwait(false),
				result => result.Status.Code == CertificateStatusCodeResponse.RequestProcessedSuccessfully,
				delay: TimeSpan.FromMilliseconds(SleepTime),
				maxAttempts: CertificatesLimitTestPollAsyncMaxAttempts,
				cancellationToken: CancellationToken);

		TestFixture.EnrollmentStatus = certificateEnrollmentStatusResponse;

		// Assert
		Assert.NotNull(TestFixture.EnrollmentStatus.Status);
		Assert.Equal(CertificateStatusCodeResponse.RequestProcessedSuccessfully, TestFixture.EnrollmentStatus.Status.Code);
		Assert.False(string.IsNullOrWhiteSpace(TestFixture.EnrollmentStatus.CertificateSerialNumber));
		Assert.NotNull(TestFixture.EnrollmentStatus.RequestDate);
		#endregion

		#region Pobierz zarejestrowany certyfikat
		// Arrange
		TestFixture.SerialNumbers = [TestFixture.EnrollmentStatus.CertificateSerialNumber];
		CertificateListRequest certificateListRequest = new() { CertificateSerialNumbers = TestFixture.SerialNumbers };

		// Act
		CertificateListResponse certificateListResponse = await KsefClient.GetCertificateListAsync(
			certificateListRequest,
			delegateAccessToken,
			CancellationToken);
		TestFixture.RetrievedCertificates = certificateListResponse;

		// Assert
		Assert.NotNull(TestFixture.RetrievedCertificates);
		Assert.Single(TestFixture.RetrievedCertificates.Certificates);
		Assert.False(string.IsNullOrWhiteSpace(TestFixture.RetrievedCertificates.Certificates.First().Certificate));
		Assert.NotNull(TestFixture.RetrievedCertificates.Certificates.First().CertificateType);
		Assert.False(string.IsNullOrWhiteSpace(TestFixture.RetrievedCertificates.Certificates.First().CertificateName));
		Assert.False(string.IsNullOrWhiteSpace(TestFixture.RetrievedCertificates.Certificates.First().CertificateSerialNumber));

		#endregion

		#region Cofnij certyfikat
		// Arrange
		string certificateSerialNumber = TestFixture.RetrievedCertificates.Certificates.ToList().First().CertificateSerialNumber;
		CertificateRevokeRequest certificateRevokeRequest = RevokeCertificateRequestBuilder
			.Create()
			.WithRevocationReason(CertificateRevocationReason.KeyCompromise)
			.Build();

		// Act && Assert
		Exception exception = await Record.ExceptionAsync(async () =>
			await KsefClient.RevokeCertificateAsync(
				certificateRevokeRequest,
				certificateSerialNumber,
				delegateAccessToken,
				CancellationToken).ConfigureAwait(false)
		);
		Assert.Null(exception);
		#endregion

		#region Pobierz listę metadanych zarejestrowanych certyfikatów
		CertificateMetadataListResponse certificateMetadataListResponse = await KsefClient.GetCertificateMetadataListAsync(
			delegateAccessToken,
			requestPayload: null,
			pageSize: CertificatesLimitTestPollAsyncMaxAttempts,
			pageOffset: 0,
			CancellationToken);
		TestFixture.MetadataList = certificateMetadataListResponse;

		// Assert
		Assert.NotNull(TestFixture.MetadataList);
		Assert.Contains(TestFixture.MetadataList.Certificates.ToList(), m => TestFixture.SerialNumbers.Contains(m.CertificateSerialNumber));
		#endregion

		#region Odwołaj uprawnienia

		PagedPermissionsResponse<PersonPermission> permissionsQueryResponse =
			await KsefClient
			.SearchGrantedPersonPermissionsAsync(
				new PersonPermissionsQueryRequest { },
				delegateAccessToken,
				pageOffset: 0,
				pageSize: CertificatesLimitTestPollAsyncMaxAttempts,
				CancellationToken);

		Assert.NotEmpty(permissionsQueryResponse.Permissions);

		OperationResponse operationResponse = await KsefClient.RevokeCommonPermissionAsync(permissionsQueryResponse.Permissions.First().Id, ownerAccessToken, CancellationToken);

		// Poll status operacji cofnięcia uprawnień do 200
		PermissionsOperationStatusResponse revokeOpStatus = await AsyncPollingUtils.PollAsync(
			async () => await KsefClient.OperationsStatusAsync(operationResponse.ReferenceNumber, ownerAccessToken, CancellationToken).ConfigureAwait(false),
			result => result.Status.Code == OperationStatusCodeResponse.Success,
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: 30,
			cancellationToken: CancellationToken);

		Assert.Equal(200, revokeOpStatus.Status.Code);
		Assert.False(string.IsNullOrWhiteSpace(revokeOpStatus.Status.Description));
		Assert.Null(revokeOpStatus.Status.Details);

		// Poll aż lista uprawnień delegata będzie pusta
		permissionsQueryResponse = await AsyncPollingUtils.PollAsync(
			async () => await KsefClient
				.SearchGrantedPersonPermissionsAsync(
					new PersonPermissionsQueryRequest { },
					delegateAccessToken,
					pageOffset: 0,
					pageSize: CertificatesLimitTestPollAsyncMaxAttempts,
					CancellationToken).ConfigureAwait(false),
			result => result.Permissions is { Count: 0 },
			delay: TimeSpan.FromMilliseconds(SleepTime),
			maxAttempts: 15,
			cancellationToken: CancellationToken);

		Assert.Empty(permissionsQueryResponse.Permissions);
		#endregion zdejmij uprawnienia
	}

	/// <summary>
	/// Sprawdzenie tworzenia osobistego certyfikatu samopodpisanego do podpisu XAdES.
	/// </summary>
	/// <param name="encryptionMethodEnum"></param>
	[Theory]
	[InlineData(EncryptionMethodEnum.Rsa)]
	[InlineData(EncryptionMethodEnum.ECDsa)]
	public void SelfSignedCertificateForSignatureBuilderCreateShouldReturnObject(EncryptionMethodEnum encryptionMethodEnum)
	{
		// Arrange
		EncryptionMethodEnum encryptionType = encryptionMethodEnum;
		string serialNumber = MiscellaneousUtils.GetRandomNip();

		// Act
		X509Certificate2 certificate = SelfSignedCertificateForSignatureBuilder
					.Create()
					.WithGivenName(GivenName)
					.WithSurname(Surname)
					.WithSerialNumber($"{SerialNumberPrefix}-{serialNumber}")
					.WithCommonName(CommonName)
					.AndEncryptionType(encryptionType)
					.Build();

		// Assert
		Assert.NotNull(certificate);
		Assert.True(certificate.HasPrivateKey);
		Assert.False(certificate.Archived);
		if (encryptionType == EncryptionMethodEnum.ECDsa)
		{
			Assert.IsAssignableFrom<ECDsa>(certificate.GetECDsaPrivateKey());
		}
		else
		{
			Assert.IsAssignableFrom<RSA>(certificate.GetRSAPrivateKey());
		}
	}

	/// <summary>
	/// Sprawdzenie tworzenia firmowego certyfikatu (company seal) samopodpisanego do podpisu XAdES.
	/// </summary>
	/// <param name="encryptionMethodEnum"></param>
	[Theory]
	[InlineData(EncryptionMethodEnum.Rsa)]
	[InlineData(EncryptionMethodEnum.ECDsa)]
	public void SelfSignedCompanySealForSignatureBuilderCreateShouldReturnObject(EncryptionMethodEnum encryptionMethodEnum)
	{
		if (encryptionMethodEnum == EncryptionMethodEnum.ECDsa)
		{
			return;
		}

		// Arrange
		string organizationIdentifier = MiscellaneousUtils.GetRandomNip();

		// Act
		X509Certificate2 certificate = SelfSignedCertificateForSealBuilder
					.Create()
					.WithOrganizationName(OrganizationName)
					.WithOrganizationIdentifier(organizationIdentifier)
					.WithCommonName(OrganizationCommonName)
					.Build();

		// Assert
		Assert.NotNull(certificate);
		Assert.True(certificate.HasPrivateKey);
	}

	/// <summary>
	/// Sprawdzenie poprawnej komunikacji od API po przekroczeniu limitu wystawionych certyfikatów.
	/// </summary>
	[Fact]
	public async Task CertificatesLimits_WhenExceeded_ShouldThrowException()
	{
		// Arrange
		string nip = "9999999999";

		AuthenticationOperationStatusResponse authenticationOperationStatusResponse =
			await AuthenticationUtils.AuthenticateAsync(AuthorizationClient, nip);

		string accessToken = authenticationOperationStatusResponse.AccessToken.Token;

		// Pobranie limitów
		CertificateLimitResponse certsLimits =
			await KsefClient.GetCertificateLimitsAsync(accessToken, CancellationToken);

		Assert.NotNull(certsLimits);

		int toCreate = certsLimits.Certificate.Remaining;
		List<CertificateEnrollmentStatusResponse> certificates = new();

		// Jeśli limit jest już przekroczony/wyczerpany, to tylko testujemy wyjątek
		bool needsToCreateCertificates = toCreate > 0;

		// Act - Tworzenie certyfikatów do wyczerpania limitu (jeśli są dostępne)
		if (needsToCreateCertificates)
		{
			for (int i = 0; i < toCreate; i++)
			{
				CertificateEnrollmentsInfoResponse enrollmentInfo =
					await KsefClient.GetCertificateEnrollmentDataAsync(accessToken, CancellationToken);

				(string csr, string key) = CryptographyService.GenerateCsrWithRsa(
					enrollmentInfo,
					RSASignaturePadding.Pkcs1);

				SendCertificateEnrollmentRequest sendCertificateEnrollmentRequest =
					SendCertificateEnrollmentRequestBuilder
						.Create()
						.WithCertificateName($"{TestCertificateName} {i + 1}")
						.WithCertificateType(CertificateType.Authentication)
						.WithCsr(csr)
						.WithValidFrom(DateTimeOffset.UtcNow.AddDays(CertificateValidityDays))
						.Build();

				CertificateEnrollmentResponse certificateEnrollmentResponse =
					await KsefClient.SendCertificateEnrollmentAsync(
						sendCertificateEnrollmentRequest,
						accessToken,
						CancellationToken);

				Assert.NotNull(certificateEnrollmentResponse);
				Assert.False(string.IsNullOrWhiteSpace(certificateEnrollmentResponse.ReferenceNumber));

				// Polling aż certyfikat będzie gotowy
				CertificateEnrollmentStatusResponse certificate =
					await AsyncPollingUtils.PollAsync(
						action: async () => await KsefClient.GetCertificateEnrollmentStatusAsync(
							certificateEnrollmentResponse.ReferenceNumber,
							accessToken,
							CancellationToken).ConfigureAwait(false),
						condition: c => c is not null && !string.IsNullOrWhiteSpace(c.CertificateSerialNumber),
						delay: TimeSpan.FromMilliseconds(SleepTime),
						maxAttempts: CertificatesLimitTestPollAsyncMaxAttempts,
						cancellationToken: CancellationToken);

				Assert.NotNull(certificate);
				certificates.Add(certificate);

				await Task.Delay(500, CancellationToken);
			}

			// Weryfikacja że limit został wyczerpany
			certsLimits = await KsefClient.GetCertificateLimitsAsync(accessToken, CancellationToken);
			Assert.NotNull(certsLimits);
			Assert.Equal(0, certsLimits.Certificate.Remaining);
		}

		KsefApiException ksefApiException = await Assert.ThrowsAsync<KsefApiException>(async () =>
		{
			CertificateEnrollmentsInfoResponse enrollmentInfo =
				await KsefClient.GetCertificateEnrollmentDataAsync(accessToken, CancellationToken)
				.ConfigureAwait(false);

			(string csr, string key) = CryptographyService.GenerateCsrWithRsa(
				enrollmentInfo,
				RSASignaturePadding.Pkcs1);

			SendCertificateEnrollmentRequest sendCertificateEnrollmentRequest =
				SendCertificateEnrollmentRequestBuilder
					.Create()
					.WithCertificateName("Test certificate")
					.WithCertificateType(CertificateType.Authentication)
					.WithCsr(csr)
					.WithValidFrom(DateTimeOffset.UtcNow.AddDays(CertificateValidityDays))
					.Build();

			_ = await KsefClient.SendCertificateEnrollmentAsync(
				sendCertificateEnrollmentRequest,
				accessToken,
				CancellationToken)
			.ConfigureAwait(false);
		});

		Assert.NotNull(ksefApiException);
		Assert.Equal(ExpectedCertificatesLimitExceededExceptionMessage, ksefApiException.Message);

        // TODO for this nip we should remove all created certs after test run
        // Cleanup, odwoływanie tylko tych certyfikatów które stworzyliśmy
        if (certificates.Count > 0)
		{
			foreach (CertificateEnrollmentStatusResponse certificate in certificates)
			{

				await KsefClient.RevokeCertificateAsync(
					RevokeCertificateRequestBuilder
						.Create()
						.WithRevocationReason(CertificateRevocationReason.Superseded)
						.Build(),
					certificate.CertificateSerialNumber,
					accessToken,
					CancellationToken);

				await Task.Delay(500, CancellationToken);

			}

			await Task.Delay(TimeSpan.FromMilliseconds(SleepTime), CancellationToken);
		}

		// Weryfikacja limitów po odwołaniu (tylko w przypadku gdy były tworzone)
		if (needsToCreateCertificates)
		{
			certsLimits = await KsefClient.GetCertificateLimitsAsync(accessToken, CancellationToken);
			Assert.NotNull(certsLimits);
			Assert.True(certsLimits.Certificate.Remaining > 0, "Po odwołaniu certyfikatów powinny być dostępne sloty");
		}
	}
}