using System;

namespace KSeF.Client.Core.Models.Certificates
{
    public class CertificateMetadataListRequest
    {
        public string CertificateSerialNumber { get; set; }
        public string Name { get; set; }
        public CertificateType? Type { get; set; }
        public CertificateStatusEnum? Status { get; set; }
        public DateTimeOffset? ExpiresAfter { get; set; }
    }
}
