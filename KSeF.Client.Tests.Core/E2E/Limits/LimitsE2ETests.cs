using KSeF.Client.Core.Models.Authorization;
using KSeF.Client.Tests.Utils;

namespace KSeF.Client.Tests.Core.E2E.Limits;

public class LimitsE2ETests : TestBase
{
    /// <summary>
    /// Sprawdzenie dostępnych limitów w kontekście bieżącej sesji.
    /// </summary>
    [Fact]
    public async Task CurrentSessionLimits_E2E_Positive()
    {
        const int LimitsChangeValue = 4;

        // 1. Uwierzytelnianie i uzyskanie tokenu dostępu
        AuthenticationOperationStatusResponse authorizationInfo =
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                MiscellaneousUtils.GetRandomNip());
        string accessToken = authorizationInfo.AccessToken.Token;

        // 2. Pobranie limitów dla bieżącego kontekstu sesji
        Client.Core.Models.TestData.SessionLimitsInCurrentContextResponse limitsForContext =
            await LimitsClient.GetLimitsForCurrentContextAsync(
                accessToken,
                CancellationToken);

        // 3. Sprawdzenie czy limity są większe od zera
        Assert.NotNull(limitsForContext);
        Assert.True(limitsForContext.OnlineSession.MaxInvoices > 0);
        Assert.True(limitsForContext.OnlineSession.MaxInvoiceSizeInMB > 0);
        Assert.True(limitsForContext.OnlineSession.MaxInvoiceWithAttachmentSizeInMB > 0);

        Assert.True(limitsForContext.BatchSession.MaxInvoices > 0);
        Assert.True(limitsForContext.BatchSession.MaxInvoiceSizeInMB > 0);
        Assert.True(limitsForContext.BatchSession.MaxInvoiceWithAttachmentSizeInMB > 0);

        // 4. Zmiana limitów dla bieżącego kontekstu sesji
        Client.Core.Models.TestData.ChangeSessionLimitsInCurrentContextRequest newLimits =
            new Client.Core.Models.TestData.ChangeSessionLimitsInCurrentContextRequest
            {
                OnlineSession = new Client.Core.Models.TestData.TestDataSessionLimitsBase
                {
                    MaxInvoices = limitsForContext.OnlineSession.MaxInvoices + LimitsChangeValue,
                    MaxInvoiceSizeInMB = limitsForContext.OnlineSession.MaxInvoiceSizeInMB + LimitsChangeValue,
                    MaxInvoiceWithAttachmentSizeInMB = limitsForContext.OnlineSession.MaxInvoiceWithAttachmentSizeInMB + LimitsChangeValue,
                },

                BatchSession = new Client.Core.Models.TestData.TestDataSessionLimitsBase
                {
                    MaxInvoices = limitsForContext.BatchSession.MaxInvoices + LimitsChangeValue,
                    MaxInvoiceSizeInMB = limitsForContext.BatchSession.MaxInvoiceSizeInMB + LimitsChangeValue,
                    MaxInvoiceWithAttachmentSizeInMB = limitsForContext.BatchSession.MaxInvoiceWithAttachmentSizeInMB + LimitsChangeValue,
                }
            };

        await TestDataClient.ChangeSessionLimitsInCurrentContextAsync(
            newLimits,
            accessToken);

        // 5. Sprawdzenie czy limity zostały zmienione
        limitsForContext =
            await LimitsClient.GetLimitsForCurrentContextAsync(
                accessToken,
                CancellationToken);

        Assert.Equal(limitsForContext.OnlineSession.MaxInvoices, newLimits.OnlineSession.MaxInvoices);
        Assert.Equal(limitsForContext.OnlineSession.MaxInvoiceSizeInMB, newLimits.OnlineSession.MaxInvoiceSizeInMB);
        Assert.Equal(limitsForContext.OnlineSession.MaxInvoiceWithAttachmentSizeInMB, newLimits.OnlineSession.MaxInvoiceWithAttachmentSizeInMB);

        // 6. Przywrócenie oryginalnych limitów dla bieżącego kontekstu sesji
        await TestDataClient.RestoreDefaultSessionLimitsInCurrentContextAsync(
            accessToken);

        // 7. Sprawdzenie czy oryginalne limity zostały przywrócone
        limitsForContext =
            await LimitsClient.GetLimitsForCurrentContextAsync(
                accessToken,
                CancellationToken);

        Assert.Equal(limitsForContext.OnlineSession.MaxInvoices, newLimits.OnlineSession.MaxInvoices - LimitsChangeValue);
        Assert.Equal(limitsForContext.OnlineSession.MaxInvoiceSizeInMB, newLimits.OnlineSession.MaxInvoiceSizeInMB - LimitsChangeValue);
        Assert.Equal(limitsForContext.OnlineSession.MaxInvoiceWithAttachmentSizeInMB, newLimits.OnlineSession.MaxInvoiceWithAttachmentSizeInMB - LimitsChangeValue);
    }

    /// <summary>
    /// Sprawdzenie dostępnych limitów certyfikatów dla bieżącego podmiotu.
    /// </summary>
    [Fact]
    public async Task CertificatesLimits_E2E_Positive()
    {
        const int LimitsChangeValue = 4;

        // 1. Uwierzytelnianie i uzyskanie tokenu dostępu
        AuthenticationOperationStatusResponse authorizationInfo =
            await AuthenticationUtils.AuthenticateAsync(
                AuthorizationClient,
                SignatureService,
                MiscellaneousUtils.GetRandomNip());
        string accessToken = authorizationInfo.AccessToken.Token;

        // 2. Pobranie limitów dla bieżącego podmiotu
        Client.Core.Models.TestData.CertificatesLimitInCurrentSubjectResponse limitsForSubject =
            await LimitsClient.GetLimitsForCurrentSubjectAsync(
                accessToken,
                CancellationToken);

        Assert.NotNull(limitsForSubject);
        Assert.True(limitsForSubject.Certificate.MaxCertificates > 0);
        Assert.True(limitsForSubject.Enrollment.MaxEnrollments > 0);

        // 3. Zmiana limitów
        Client.Core.Models.TestData.ChangeCertificatesLimitInCurrentSubjectRequest newCertificateLimitsForSubject = new Client.Core.Models.TestData.ChangeCertificatesLimitInCurrentSubjectRequest
        {
            SubjectIdentifierType = Client.Core.Models.TestData.TestDataSubjectIdentifierType.Nip,
            Certificate = new Client.Core.Models.TestData.TestDataCertificate
            {
                MaxCertificates = limitsForSubject.Certificate.MaxCertificates + LimitsChangeValue
            },
            Enrollment = new Client.Core.Models.TestData.TestDataEnrollment
            {
                MaxEnrollments = limitsForSubject.Enrollment.MaxEnrollments + LimitsChangeValue
            }
        };

        await TestDataClient.ChangeCertificatesLimitInCurrentSubjectAsync(
            newCertificateLimitsForSubject,
            accessToken);

        // 4. Pobranie aktualnych limitów dla bieżącego podmiotu
        limitsForSubject =
            await LimitsClient.GetLimitsForCurrentSubjectAsync(
                accessToken,
                CancellationToken);


        // 5. Sprawdzenie czy limity dla bieżącego podmiotu zostały zmienione
        Assert.Equal(limitsForSubject.Certificate.MaxCertificates, newCertificateLimitsForSubject.Certificate.MaxCertificates);
        Assert.Equal(limitsForSubject.Enrollment.MaxEnrollments, newCertificateLimitsForSubject.Enrollment.MaxEnrollments);

        // 6. Przywrócenie oryginalnych limitów dla bieżącego podmiotu
        await TestDataClient.RestoreDefaultCertificatesLimitInCurrentSubjectAsync(
            accessToken);

        // 7. Pobranie aktualnych limitów dla bieżącego podmiotu
        limitsForSubject =
            await LimitsClient.GetLimitsForCurrentSubjectAsync(
                accessToken,
                CancellationToken);

        // 8. Sprawdzenie czy oryginalne limity dla bieżącego podmiotu zostały przywrócone
        Assert.Equal(limitsForSubject.Certificate.MaxCertificates, newCertificateLimitsForSubject.Certificate.MaxCertificates - LimitsChangeValue);
        Assert.Equal(limitsForSubject.Enrollment.MaxEnrollments, newCertificateLimitsForSubject.Enrollment.MaxEnrollments - LimitsChangeValue);
    }
}
