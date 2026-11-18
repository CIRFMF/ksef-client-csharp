namespace KSeF.Client.Core.Models.TestData
{
    /// <summary>
    /// Żądanie blokady/odblokowania uwierzytelniania w kontekście
    /// (POST /testdata/context/block oraz /testdata/context/unblock).
    /// </summary>
    public sealed class ContextBlockingRequest
    {
        /// <summary>Identyfikator kontekstu, którego dotyczy operacja.</summary>
        public ContextBlockingIdentifier ContextIdentifier { get; set; }
    }

    /// <summary>Identyfikator kontekstu blokowanego/odblokowywanego w środowisku testowym.</summary>
    public sealed class ContextBlockingIdentifier
    {
        /// <summary>Typ identyfikatora kontekstu.</summary>
        public ContextBlockingIdentifierType Type { get; set; }

        /// <summary>Wartość identyfikatora kontekstu.</summary>
        public string Value { get; set; }
    }

    /// <summary>Typ identyfikatora kontekstu (TestDataAuthenticationContextIdentifierType).</summary>
    public enum ContextBlockingIdentifierType
    {
        Nip,
        InternalId,
        NipVatUe,
        PeppolId
    }
}
