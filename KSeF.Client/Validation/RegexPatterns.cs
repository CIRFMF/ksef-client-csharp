using System.Text.RegularExpressions;

namespace KSeF.Client.Validation
{
    /// <summary>
    /// Wzorce wyrażeń regularnych do walidacji formatów identyfikatorów KSeF.
    /// Na .NET 7+ używa <c>[GeneratedRegex]</c> source generator.
    /// Na netstandard2.0 używa <c>new Regex()</c> z <see cref="RegexOptions.Compiled"/>.
    /// </summary>
    public static partial class RegexPatterns
    {
        public const string NipPatternCore = "[1-9]((\\d[1-9])|([1-9]\\d))\\d{7}";
        public const string NipPattern = $"^{NipPatternCore}$";

        public const string VatUePatternCore = "(ATU\\d{8}|BE[01]{1}\\d{9}|BG\\d{9,10}|CY\\d{8}[A-Z]|CZ\\d{8,10}|DE\\d{9}|DK\\d{8}|EE\\d{9}|EL\\d{9}|ES([A-Z]\\d{8}|\\d{8}[A-Z]|[A-Z]\\d{7}[A-Z])|FI\\d{8}|FR[A-Z0-9]{2}\\d{9}|HR\\d{11}|HU\\d{8}|IE(\\d{7}[A-Z]{2}|\\d[A-Z0-9+*]\\d{5}[A-Z])|IT\\d{11}|LT(\\d{9}|\\d{12})|LU\\d{8}|LV\\d{11}|MT\\d{8}|NL[A-Z0-9+*]{12}|PT\\d{9}|RO\\d{2,10}|SE\\d{12}|SI\\d{8}|SK\\d{10}|XI((\\d{9}|\\d{12})|(GD|HA)\\d{3}))";
        public const string VatUePattern = $"^{VatUePatternCore}$";

        public const string NipVatUePattern = $"^{NipPatternCore}-{VatUePatternCore}$";

        public const string InternalIdPattern = "^[1-9]((\\d[1-9])|([1-9]\\d))\\d{7}-\\d{5}$";
        public const string PeppolIdPattern = @"^P[A-Z]{2}[0-9]{6}$";

        public const string ReferenceNumberPattern = "^(20[2-9][0-9]|2[1-9][0-9]{2}|[3-9][0-9]{3})(0[1-9]|1[0-2])(0[1-9]|[1-2][0-9]|3[0-1])-([0-9A-Z]{2})-([0-9A-F]{10})-([0-9A-F]{10})-([0-9A-F]{2})$";
        public const string KsefNumberPattern = "^([1-9](\\d[1-9]|[1-9]\\d)\\d{7})-(20[2-9][0-9]|2[1-9]\\d{2}|[3-9]\\d{3})(0[1-9]|1[0-2])(0[1-9]|[12]\\d|3[01])-([0-9A-F]{6})-?([0-9A-F]{6})-([0-9A-F]{2})$";

        public const string CertificateNamePattern = @"^[a-zA-Z0-9_\-\ ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]+$";

        public const string PeselPattern = @"^\d{2}(?:0[1-9]|1[0-2]|2[1-9]|3[0-2]|4[1-9]|5[0-2]|6[1-9]|7[0-2]|8[1-9]|9[0-2])\d{7}$";
        public const string CertificateFingerPrintSha256Pattern = @"^[0-9A-F]{64}$";

        private const string KsefNumberV36Pattern = "^([1-9]((\\d[1-9])|([1-9]\\d))\\d{7}|M\\d{9}|[A-Z]{3}\\d{7})-(20[2-9][0-9]|2[1-9][0-9]{2}|[3-9][0-9]{3})(0[1-9]|1[0-2])(0[1-9]|[1-2][0-9]|3[0-1])-([0-9A-F]{6})-([0-9A-F]{6})-([0-9A-F]{2})$";
        private const string KsefNumberV35Pattern = "^([1-9]((\\d[1-9])|([1-9]\\d))\\d{7}|M\\d{9}|[A-Z]{3}\\d{7})-(20[2-9][0-9]|2[1-9][0-9]{2}|[3-9][0-9]{3})(0[1-9]|1[0-2])(0[1-9]|[1-2][0-9]|3[0-1])-([0-9A-F]{6})([0-9A-F]{6})-([0-9A-F]{2})$";
        private const string Base64Pattern = @"^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$";
        private const string Ip4AddressPattern = "^((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4}$";
        private const string Ip4RangePattern = "^((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4}-((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4}$";
        private const string Ip4MaskPattern = "^((25[0-5]|(2[0-4]|1\\d|[1-9]|)\\d)\\.?\\b){4}\\/(0|[1-9]|1[0-9]|2[0-9]|3[0-2])$";
        private const string Sha256Base64Pattern = "^[A-Za-z0-9+/]{43}=$";

        static RegexPatterns()
        {
            ReferenceNumber = ReferenceNumberRegex();
            KsefNumber = KsefNumberRegex();
            KsefNumberV35 = KsefNumberV35Regex();
            KsefNumberV36 = KsefNumberV36Regex();
            InternalId = InternalIdRegex();
            Nip = NipRegex();
            Pesel = PeselRegex();
            NipVatUe = NipVatUeRegex();
            Base64String = Base64Regex();
            Ip4Address = Ip4AddressRegex();
            Ip4Range = Ip4RangeRegex();
            Ip4Mask = Ip4MaskRegex();
            Sha256Base64 = Sha256Base64Regex();
            CertificateName = CertificateNameRegex();
            Fingerprint = CertificateFingerPrintSha256Regex();
            PeppolId = PeppolIdRegex();
        }

        public static Regex ReferenceNumber { get; }
        public static Regex KsefNumber { get; }
        public static Regex KsefNumberV35 { get; }
        public static Regex KsefNumberV36 { get; }
        public static Regex InternalId { get; }
        public static Regex Nip { get; }
        public static Regex Pesel { get; }
        public static Regex NipVatUe { get; }
        public static Regex Base64String { get; }
        public static Regex Ip4Address { get; }
        public static Regex Ip4Range { get; }
        public static Regex Ip4Mask { get; }
        public static Regex Sha256Base64 { get; }
        public static Regex CertificateName { get; }
        public static Regex Fingerprint { get; }
        public static Regex PeppolId { get; }

#if NETSTANDARD2_0
        // netstandard2.0: generator źródłowy [GeneratedRegex] niedostępny — użyto skompilowanych instancji Regex.
        private static Regex ReferenceNumberRegex() => new Regex(ReferenceNumberPattern, RegexOptions.Compiled);
        private static Regex KsefNumberRegex() => new Regex(KsefNumberPattern, RegexOptions.Compiled);
        private static Regex KsefNumberV36Regex() => new Regex(KsefNumberV36Pattern, RegexOptions.Compiled);
        private static Regex KsefNumberV35Regex() => new Regex(KsefNumberV35Pattern, RegexOptions.Compiled);
        private static Regex InternalIdRegex() => new Regex(InternalIdPattern, RegexOptions.Compiled);
        private static Regex NipRegex() => new Regex(NipPattern, RegexOptions.Compiled);
        private static Regex PeselRegex() => new Regex(PeselPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex NipVatUeRegex() => new Regex(NipVatUePattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static Regex Base64Regex() => new Regex(Base64Pattern, RegexOptions.Compiled);
        private static Regex Ip4AddressRegex() => new Regex(Ip4AddressPattern, RegexOptions.Compiled);
        private static Regex Ip4RangeRegex() => new Regex(Ip4RangePattern, RegexOptions.Compiled);
        private static Regex Ip4MaskRegex() => new Regex(Ip4MaskPattern, RegexOptions.Compiled);
        private static Regex Sha256Base64Regex() => new Regex(Sha256Base64Pattern, RegexOptions.Compiled);
        private static Regex CertificateNameRegex() => new Regex(CertificateNamePattern, RegexOptions.Compiled);
        private static Regex CertificateFingerPrintSha256Regex() => new Regex(CertificateFingerPrintSha256Pattern, RegexOptions.Compiled);
        private static Regex PeppolIdRegex() => new Regex(PeppolIdPattern, RegexOptions.Compiled);
#else
        [GeneratedRegex(ReferenceNumberPattern, RegexOptions.Compiled)]
        private static partial Regex ReferenceNumberRegex();

        [GeneratedRegex(KsefNumberPattern, RegexOptions.Compiled)]
        private static partial Regex KsefNumberRegex();

        [GeneratedRegex(KsefNumberV36Pattern, RegexOptions.Compiled)]
        private static partial Regex KsefNumberV36Regex();

        [GeneratedRegex(KsefNumberV35Pattern, RegexOptions.Compiled)]
        private static partial Regex KsefNumberV35Regex();

        [GeneratedRegex(InternalIdPattern, RegexOptions.Compiled)]
        private static partial Regex InternalIdRegex();

        [GeneratedRegex(NipPattern, RegexOptions.Compiled)]
        private static partial Regex NipRegex();

        [GeneratedRegex(PeselPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex PeselRegex();

        [GeneratedRegex(NipVatUePattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        private static partial Regex NipVatUeRegex();

        [GeneratedRegex(Base64Pattern)]
        private static partial Regex Base64Regex();

        [GeneratedRegex(Ip4AddressPattern, RegexOptions.Compiled)]
        private static partial Regex Ip4AddressRegex();

        [GeneratedRegex(Ip4RangePattern, RegexOptions.Compiled)]
        private static partial Regex Ip4RangeRegex();

        [GeneratedRegex(Ip4MaskPattern, RegexOptions.Compiled)]
        private static partial Regex Ip4MaskRegex();

        [GeneratedRegex(Sha256Base64Pattern, RegexOptions.Compiled)]
        private static partial Regex Sha256Base64Regex();

        [GeneratedRegex(CertificateNamePattern, RegexOptions.Compiled)]
        private static partial Regex CertificateNameRegex();

        [GeneratedRegex(CertificateFingerPrintSha256Pattern, RegexOptions.Compiled)]
        private static partial Regex CertificateFingerPrintSha256Regex();

        [GeneratedRegex(PeppolIdPattern, RegexOptions.Compiled)]
        private static partial Regex PeppolIdRegex();
#endif

    }
}
