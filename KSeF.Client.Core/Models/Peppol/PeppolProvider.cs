using System;

namespace KSeF.Client.Core.Models.Peppol
{
    public class PeppolProvider
    {
        /// <summary>
        /// Identyfikator dostawcy usług Peppol (np. kod w rejestrze Peppol).
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nazwa dostawcy usług Peppol.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data rejestracji dostawcy usług Peppol w systemie.
        /// </summary>
        public DateTimeOffset DateCreated { get; set; }
    }
}
