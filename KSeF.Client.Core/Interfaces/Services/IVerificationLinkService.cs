using KSeF.Client.Core.Models.QRCode;
using System;
using System.Security.Cryptography.X509Certificates;


namespace KSeF.Client.Core.Interfaces.Services
{
    public interface IVerificationLinkService
    {
        /// <summary>
        /// Buduje link do weryfikacji faktury w systemie KSeF.
        /// </summary>
        string BuildInvoiceVerificationUrl(string nip, DateTime issueDate, string invoiceHash);

        /// <summary>
        /// Buduje link do weryfikacji certyfikatu Wystawcy (offline).
        /// </summary>
        string BuildCertificateVerificationUrl(
            string sellerNip,
            QRCodeContextIdentifierType contextIdentifierType,
            string contextIdentifierValue,
            string certificateSerial,
            string invoiceHash,
            X509Certificate2 signingCertificate,
            string privateKey = ""
        );
    }
}
