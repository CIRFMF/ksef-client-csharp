using System.Collections.Generic;

namespace KSeF.Client.Core.Exceptions
{
    public class ForbiddenProblemDetails
    {
        public string Title { get; set; }
        public int Status { get; set; }
        public string Detail { get; set; }
        public string Instance { get; set; }
        public string ReasonCode { get; set; }
        public Dictionary<string, object> Security { get; set; }
        public string TraceId { get; set; }
    }
}
