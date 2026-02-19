namespace KSeF.Client.Core.Infrastructure.Rest
{
    /// <summary>
    /// Endpointy API KSeF.
    /// </summary>
    public static class Routes
    {
        /// <summary>
        /// Endpointy dotyczące danych testowych.
        /// </summary>
        public static class TestData
        {
            private const string Prefix = "testdata";

            /// <summary>
            /// Utworzenie podmiotu testowego.
            /// </summary>
            public const string CreateSubject = Prefix + "/subject";
            /// <summary>
            /// Usunięcie podmiotu testowego.
            /// </summary>
            public const string RemoveSubject = Prefix + "/subject/remove";

            /// <summary>
            /// Utworzenie osoby testowej.
            /// </summary>
            public const string CreatePerson = Prefix + "/person";
            /// <summary>
            /// Usunięcie osoby testowej.
            /// </summary>
            public const string RemovePerson = Prefix + "/person/remove";

            /// <summary>
            /// Nadanie uprawnień testowych.
            /// </summary>
            public const string GrantPerms = Prefix + "/permissions";
            /// <summary>
            /// Cofnięcie uprawnień testowych.
            /// </summary>
            public const string RevokePerms = Prefix + "/permissions/revoke";

            /// <summary>
            /// Włączenie zgody na załączniki w środowisku testowym.
            /// </summary>
            public const string EnableAttach = Prefix + "/attachment";
            /// <summary>
            /// Wyłączenie zgody na załączniki w środowisku testowym.
            /// </summary>
            public const string DisableAttach = Prefix + "/attachment/revoke";

            /// <summary>
            /// Zmiana limitów sesji w bieżącym kontekście (test).
            /// </summary>
            public const string ChangeSessionLimitsInCurrentContext = Prefix + "/limits/context/session";
            /// <summary>
            /// Przywrócenie domyślnych limitów sesji w bieżącym kontekście (test).
            /// </summary>
            public const string RestoreDefaultSessionLimitsInCurrentContext = Prefix + "/limits/context/session";

            /// <summary>
            /// Zmiana limitu certyfikatów bieżącego podmiotu (test).
            /// </summary>
            public const string ChangeCertificatesLimitInCurrentSubject = Prefix + "/limits/subject/certificate";
            /// <summary>
            /// Przywrócenie domyślnego limitu certyfikatów bieżącego podmiotu (test).
            /// </summary>
            public const string RestoreDefaultCertificatesLimitInCurrentSubject = Prefix + "/limits/subject/certificate";

            /// <summary>
            /// Odczyt limitów zapytań (rate limits) w środowisku testowym.
            /// </summary>
            public const string RateLimits = Prefix + "/rate-limits";

            /// <summary>
            /// Przywrócenie domyślnych produkcyjnych limitów API.
            /// </summary>
            public const string ProductionRateLimits = Prefix + "/rate-limits/production";

            /// <summary>
            /// Zablokowanie kontekstu testowego w środowisku DEMO.
            /// Wywołanie: POST /testdata/context/block
            /// </summary>
            public const string BlockContext = Prefix + "/testdata/context/block";

            /// <summary>
            /// Odblokowanie kontekstu testowego w środowisku DEMO.
            /// Wywołanie: POST /testdata/context/unblock
            /// </summary>
            public const string UnblockContext = Prefix + "/testdata/context/unblock";
        }

        /// <summary>
        /// Endpointy dotyczące limitów żądań wysyłanych do API.
        /// </summary>
        public static class Limits
        {
            private const string Prefix = "limits";
            /// <summary>
            /// Limity bieżącego kontekstu.
            /// </summary>
            public const string CurrentContext = Prefix + "/context";
            /// <summary>
            /// Limity bieżącego podmiotu.
            /// </summary>
            public const string CurrentSubject = Prefix + "/subject";

            /// <summary>
            /// Globalne limity zapytań (rate limits).
            /// </summary>
            public const string RateLimits = "rate-limits";
        }

        /// <summary>
        /// Endpointy dotyczące sesji uwierzytelnienia aktywnych w KSeF.
        /// </summary>
        public static class ActiveSessions
        {
            /// <summary>
            /// Lista aktywnych sesji uwierzytelnienia.
            /// </summary>
            public const string Session = "auth/sessions";
            /// <summary>
            /// Informacje o bieżącej sesji uwierzytelnienia.
            /// </summary>
            public const string CurrentSession = Session + "/current";
        }

        /// <summary>
        /// Endpointy dotyczące uwierzytelniania.
        /// </summary>
        public static class Authorization
        {
            private const string Prefix = "auth";
            /// <summary>
            /// Rozpoczęcie wyzwania uwierzytelniającego.
            /// </summary>
            public const string Challenge = Prefix + "/challenge";
            /// <summary>
            /// Przesłanie podpisu XAdES w procesie uwierzytelnienia.
            /// </summary>
            public const string XadesSignature = Prefix + "/xades-signature";
            /// <summary>
            /// Uwierzytelnienie tokenem KSeF.
            /// </summary>
            public const string KsefToken = Prefix + "/ksef-token";
            /// <summary>
            /// Sprawdzenie statusu operacji uwierzytelnienia.
            /// </summary>
            public static string Status(string reference) => Prefix + "/" + reference;
            public static class Token
            {
                private const string TokenPrefix = Prefix + "/token";
                /// <summary>
                /// Pobranie access token (redeem).
                /// </summary>
                public const string Redeem = TokenPrefix + "/redeem";
                /// <summary>
                /// Odświeżenie access token.
                /// </summary>
                public const string Refresh = TokenPrefix + "/refresh";
            }
        }

        /// <summary>
        /// Endpointy dotyczące sesji (interaktywne/wsadowe).
        /// </summary>
        public static class Sessions
        {
            private const string Prefix = "sessions";

            /// <summary>
            /// Korzeń operacji na sesjach.
            /// </summary>
            public const string Root = Prefix;
            /// <summary>
            /// Status sesji wg numeru referencyjnego.
            /// </summary>
            public static string ByReference(string referenceNumber) => Prefix + "/" + referenceNumber;
            /// <summary>
            /// Lista faktur w sesji.
            /// </summary>
            public static string Invoices(string referenceNumber) => Prefix + "/" + referenceNumber + "/invoices";
            /// <summary>
            /// Pojedyncza faktura w sesji.
            /// </summary>
            public static string Invoice(string referenceNumber, string invoiceReferenceNumber) => Prefix + "/" + referenceNumber + "/invoices/" + invoiceReferenceNumber;
            /// <summary>
            /// Lista nieudanych faktur w sesji.
            /// </summary>
            public static string FailedInvoices(string referenceNumber) => Prefix + "/" + referenceNumber + "/invoices/failed";
            /// <summary>
            /// UPO faktury według numeru KSeF.
            /// </summary>
            public static string UpoByKsefNumber(string referenceNumber, string ksefNumber) => Prefix + "/" + referenceNumber + "/invoices/ksef/" + ksefNumber + "/upo";
            /// <summary>
            /// UPO faktury według numeru referencyjnego faktury.
            /// </summary>
            public static string UpoByInvoiceReference(string referenceNumber, string invoiceReferenceNumber) => Prefix + "/" + referenceNumber + "/invoices/" + invoiceReferenceNumber + "/upo";
            /// <summary>
            /// UPO sesji według numeru referencyjnego UPO.
            /// </summary>
            public static string Upo(string referenceNumber, string upoReferenceNumber) => Prefix + "/" + referenceNumber + "/upo/" + upoReferenceNumber;

            /// <summary>
            /// Sesja interaktywna
            /// </summary>
            public static class Online
            {
                private const string OnlinePrefix = Prefix + "/online";

                /// <summary>
                /// Otwarcie sesji interaktywnej.
                /// </summary>
                public const string Open = OnlinePrefix;

                /// <summary>
                /// Wysyłka faktur w sesji interaktywnej.
                /// </summary>
                public static string Invoices(string sessionReferenceNumber) => OnlinePrefix + "/" + sessionReferenceNumber + "/invoices";

                /// <summary>
                /// Zamknięcie sesji interaktywnej.
                /// </summary>
                public static string Close(string sessionReferenceNumber) => OnlinePrefix + "/" + sessionReferenceNumber + "/close";
            }

            /// <summary>
            /// Sesja wsadowa.
            /// </summary>
            public static class Batch
            {
                private const string BatchPrefix = Prefix + "/batch";

                /// <summary>
                /// Otwarcie sesji wsadowej.
                /// </summary>
                public const string Open = BatchPrefix;

                /// <summary>
                /// Zamknięcie sesji wsadowej.
                /// </summary>
                public static string Close(string batchSessionReferenceNumber) => BatchPrefix + "/" + batchSessionReferenceNumber + "/close";
            }
        }

        /// <summary>
        /// Endpointy dotyczące faktur (pobieranie, metadane, eksport).
        /// </summary>
        public static class Invoices
        {
            private const string Prefix = "invoices";

            /// <summary>
            /// Pobranie treści faktury według numeru KSeF.
            /// </summary>
            public static string ByKsefNumber(string ksefNumber) => Prefix + "/ksef/" + ksefNumber;

            /// <summary>
            /// Zapytanie o metadane faktur.
            /// </summary>
            public const string QueryMetadata = Prefix + "/query/metadata";

            /// <summary>
            /// Eksport paczki faktur.
            /// </summary>
            public const string Exports = Prefix + "/exports";

            /// <summary>
            /// Status operacji eksportu według numeru referencyjnego.
            /// </summary>
            public static string ExportByReference(string referenceNumber) => Exports + "/" + referenceNumber;
        }

        /// <summary>
        /// Endpointy dotyczące uprawnień.
        /// </summary>
        public static class Permissions
        {
            private const string Prefix = "permissions";

            /// <summary>
            /// Nadawanie uprawnień.
            /// </summary>
            public static class Grants
            {
                /// <summary>
                /// Nadanie uprawnień do pracy w KSeF osobie fizycznej w bieżącym kontekście podmiotu.
                /// </summary>
                public const string Persons = Prefix + "/persons/grants";
                /// <summary>
                /// Nadanie ról do obsługi faktur innemu podmiotowi (np. biuru rachunkowemu) w bieżącym kontekście.
                /// </summary>
                public const string Entities = Prefix + "/entities/grants";
                /// <summary>
                /// Nadanie uprawnień o charakterze upoważnień (samofakturowanie, RR, przedstawiciel podatkowy, PEF).
                /// </summary>
                public const string Authorizations = Prefix + "/authorizations/grants";
                /// <summary>
                /// Nadanie uprawnień pośrednich podmiotowi w kontekście wskazanego podmiotu docelowego.
                /// </summary>
                public const string Indirect = Prefix + "/indirect/grants";
                /// <summary>
                /// Nadanie uprawnień administratora podmiotu podrzędnego (subunit) w danym kontekście.
                /// </summary>
                public const string Subunits = Prefix + "/subunits/grants";
                /// <summary>
                /// Nadanie uprawnień administracyjnych podmiotowi unijnemu (EU entity) w określonym kontekście.
                /// </summary>
                public const string EuEntities = Prefix + "/eu-entities/administration/grants";
                /// <summary>
                /// Nadanie uprawnień przedstawicielowi podmiotu unijnego.
                /// </summary>
                public const string EuEntitiesRepresentatives = Prefix + "/eu-entities/grants";
            }

            /// <summary>
            /// Cofanie nadanych uprawnień.
            /// </summary>
            public static class Common
            {
                /// <summary>
                /// Adres nadania wspólnego (common grant) wg identyfikatora.
                /// </summary>
                public static string GrantById(string permissionId) => Prefix + "/common/grants/" + permissionId;
            }

            /// <summary>
            /// Uprawnienia o charakterze upoważnień.
            /// </summary>
            public static class Authorizations
            {
                /// <summary>
                /// Adres nadania upoważnienia wg identyfikatora.
                /// </summary>
                public static string GrantById(string permissionId) => Prefix + "/authorizations/grants/" + permissionId;
            }

            /// <summary>
            /// Wyszukiwanie nadanych uprawnień.
            /// </summary>
            public static class Query
            {
                /// <summary>
                /// Wyszukiwanie własnych uprawnień (personal grants) zalogowanej osoby w bieżącym kontekście.
                /// </summary>
                public const string PersonalGrants = Prefix + "/query/personal/grants";

                /// <summary>
                /// Wyszukiwanie uprawnień nadanych osobom fizycznym przez bieżący podmiot.
                /// </summary>
                public const string PersonsGrants = Prefix + "/query/persons/grants";

                /// <summary>
                /// Wyszukiwanie uprawnień administratora podmiotów podrzędnych (subunits) nadanych w bieżącym kontekście.
                /// </summary>
                public const string SubunitsGrants = Prefix + "/query/subunits/grants";

                /// <summary>
                /// Wyszukiwanie ról do obsługi faktur nadanych podmiotom.
                /// </summary>
                public const string EntitiesRoles = Prefix + "/query/entities/roles";

                /// <summary>
                /// Wyszukiwanie ról do obsługi faktur nadanych podmiotom podrzędnym.
                /// </summary>
                public const string SubordinateEntitiesRoles = Prefix + "/query/subordinate-entities/roles";

                /// <summary>
                /// Wyszukiwanie uprawnień typu upoważnienia (samofakturowanie, RR, przedstawiciel podatkowy, PEF) nadanych podmiotom.
                /// </summary>
                public const string AuthorizationsGrants = Prefix + "/query/authorizations/grants";

                /// <summary>
                /// Wyszukiwanie uprawnień nadanych podmiotom unijnym (EU entities) w bieżącym kontekście.
                /// </summary>
                public const string EuEntitiesGrants = Prefix + "/query/eu-entities/grants";
            }

            /// <summary>
            /// Status operacji na uprawnieniach.
            /// </summary>
            public static class Operations
            {
                /// <summary>
                /// Adres statusu operacji na uprawnieniach wg numeru referencyjnego.
                /// </summary>
                public static string ByReference(string operationReferenceNumber) => Prefix + "/operations/" + operationReferenceNumber;
            }

            /// <summary>
            /// Zgoda na faktury z załącznikiem.
            /// </summary>
            public static class Attachments
            {
                /// <summary>
                /// Status zgody na faktury z załącznikiem.
                /// </summary>
                public const string Status = Prefix + "/attachments/status";
            }
        }

        /// <summary>
        /// Endpointy dotyczące certyfikatów.
        /// </summary>
        public static class Certificates
        {
            private const string Prefix = "certificates";
            /// <summary>
            /// Limity certyfikatów.
            /// </summary>
            public const string Limits = Prefix + "/limits";
            /// <summary>
            /// Dane do rejestracji certyfikatów.
            /// </summary>
            public const string EnrollmentData = Prefix + "/enrollments/data";
            /// <summary>
            /// Rejestracja certyfikatów.
            /// </summary>
            public const string Enrollments = Prefix + "/enrollments";
            /// <summary>
            /// Status rejestracji certyfikatów.
            /// </summary>
            public static string EnrollmentStatus(string referenceNumber) => Prefix + "/enrollments/" + referenceNumber;
            /// <summary>
            /// Pobranie certyfikatu.
            /// </summary>
            public const string Retrieve = Prefix + "/retrieve";
            /// <summary>
            /// Cofnięcie certyfikatu.
            /// </summary>
            public static string Revoke(string serialNumber) => Prefix + "/" + serialNumber + "/revoke";
            /// <summary>
            /// Zapytanie o certyfikaty.
            /// </summary>
            public const string Query = Prefix + "/query";
        }

        /// <summary>
        /// Endpointy dotyczące tokenów KSeF.
        /// </summary>
        public static class Tokens
        {
            private const string Prefix = "tokens";

            /// <summary>
            /// Korzeń operacji na tokenach (generowanie i wyszukiwanie).
            /// </summary>
            public const string Root = Prefix;
            /// <summary>
            /// Token wg numeru referencyjnego (status/unieważnienie).
            /// </summary>
            public static string ByReference(string referenceNumber) => Prefix + "/" + referenceNumber;
        }

        /// <summary>
        /// Endpointy dotyczące Peppol.
        /// </summary>
        public static class Peppol
        {
            private const string Prefix = "peppol";

            /// <summary>
            /// Zapytanie o dostawców Peppol.
            /// </summary>
            public const string Query = Prefix + "/query";
        }
    }
}