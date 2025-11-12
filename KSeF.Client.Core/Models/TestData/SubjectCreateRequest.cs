using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.TestData
{
    /// <summary>Żądanie utworzenia podmiotu testowego.</summary>
    public sealed class SubjectCreateRequest
    {
        /// <summary>Identyfikator podmiotu (NIP) wykorzystywany w testach.</summary>
        public string SubjectNip { get; set; }

        /// <summary>Typ podmiotu (np. EnforcementAuthority/JST/VATGroup).</summary>
        public SubjectType SubjectType { get; set; }

        /// <summary>Jednostki podrzędne (np. oddziały/JST).</summary>
        public ICollection<SubjectSubunit> Subunits { get; set; }

        /// <summary>Opis</summary>
        public string Description { get; set; }

        /// <summary>Data utworzenia</summary>
        public DateTimeOffset? CreatedDate { get; set; }
    }

    public sealed class SubjectSubunit
    {
        public string SubjectNip { get; set; }
        public string Description{ get; set; }
    }

    public enum SubjectType
    {
        EnforcementAuthority,
        VatGroup,
        JST
    }
}
