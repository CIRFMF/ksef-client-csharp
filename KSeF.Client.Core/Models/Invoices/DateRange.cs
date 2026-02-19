
using System;

namespace KSeF.Client.Core.Models.Invoices
{
    public class DateRange
    {
        public DateType DateType { get; set; }
        public DateTimeOffset From { get; set; }
        public DateTimeOffset? To { get; set; }
        public bool? RestrictToPermanentStorageHwmDate { get; set; }
    }
    public enum DateType
    {
        Issue,
        Invoicing,
        PermanentStorage
    }
}
