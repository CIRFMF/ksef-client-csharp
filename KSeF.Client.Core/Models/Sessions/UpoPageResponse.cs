using System;

namespace KSeF.Client.Core.Models.Sessions
{
    public class UpoPageResponse
    {
        public string ReferenceNumber { get; set; }
        public Uri DownloadUrl { get; set; }

        public DateTimeOffset DownloadUrlExpirationDate { get; set; }
    }
}
