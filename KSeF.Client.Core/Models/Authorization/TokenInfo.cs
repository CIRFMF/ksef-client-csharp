using System;

namespace KSeF.Client.Core.Models.Authorization
{
    public class TokenInfo
    {
        public string Token { get; set; }
        public DateTimeOffset ValidUntil { get; set; }
    }

}
