using System;

namespace KSeF.Client.Core.Models.Sessions
{
    public class SessionStatusResponse
    {
        public StatusInfo Status { get; set; }

        public UpoResponse Upo { get; set; }

        public int? InvoiceCount { get; set; }

        public int? SuccessfulInvoiceCount { get; set; }

        public int? FailedInvoiceCount { get; set; }

        public DateTimeOffset? ValidUntil { get; set; }
    }
}
