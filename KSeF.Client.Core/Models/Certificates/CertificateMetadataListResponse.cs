using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.Certificates
{
    public class CertificateMetadataListResponse
    {
        public ICollection<CertificateInfo> Certificates { get; set; }
        public bool HasMore { get; set; }
    }
    public class CertificateInfo
    {
        public string CertificateSerialNumber { get; set; }
        public string Name { get; set; }
        public CertificateType Type { get; set; }
        public string CommonName { get; set; }
        public string Status { get; set; }
        public CertificateSubjectIdentifier SubjectIdentifier { get; set; }
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public DateTimeOffset? LastUseDate { get; set; }
        public DateTimeOffset RequestDate { get; set; }
    }
}
