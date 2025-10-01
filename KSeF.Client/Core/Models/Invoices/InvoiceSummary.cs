using KSeF.Client.Core.Models.Invoices.Common;
using KSeF.Client.Core.Models.Sessions;

namespace KSeF.Client.Core.Models.Invoices;

public class InvoiceSummary
{
    /// <summary>
    /// Numer KSeF faktury.
    /// </summary>
    public string KsefNumber { get; set; }

    /// <summary>
    /// Numer faktury nadany przez wystawcę.
    /// </summary>
    public string InvoiceNumber { get; set; }

    /// <summary>
    /// Data wystawienia faktury (tylko data).
    /// </summary>
    public DateOnly IssueDate { get; set; }

    /// <summary>
    /// Data przyjęcia faktury w systemie KSeF (data i czas z offsetem).
    /// </summary>
    public DateTimeOffset InvoicingDate { get; set; }

    /// <summary>
    /// Data nadania numeru KSeF (data i czas z offsetem).
    /// </summary>
    public DateTimeOffset AcquisitionDate { get; set; }

    /// <summary>
    /// Data trwałego zapisu faktury w repozytorium systemu KSeF (data i czas z offsetem).
    /// </summary>
    public DateTimeOffset PermanentStorageDate { get; set; }

    /// <summary>
    /// Dane identyfikujące sprzedawcę.
    /// </summary>
    public PartyInfo Seller { get; set; }

    /// <summary>
    /// Dane identyfikujące nabywcę.
    /// </summary>
    public Buyer Buyer { get; set; }

    /// <summary>
    /// Łączna kwota netto.
    /// </summary>
    public decimal NetAmount { get; set; }

    /// <summary>
    /// Łączna kwota brutto.
    /// </summary>
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Łączna kwota VAT.
    /// </summary>
    public decimal VatAmount { get; set; }

    /// <summary>
    /// Kod waluty.
    /// </summary>
    public string Currency { get; set; }

    /// <summary>
    /// Tryb fakturowania (online/offline).
    /// </summary>
    public InvoicingMode InvoicingMode { get; set; }

    /// <summary>
    /// Rodzaj faktury.
    /// </summary>
    public InvoiceType InvoiceType { get; set; }

    /// <summary>
    /// Struktura dokumentu faktury.
    /// </summary>
    public FormCode FormCode { get; set; }

    /// <summary>
    /// Czy faktura została wystawiona w trybie samofakturowania.
    /// </summary>
    public bool IsSelfInvoicing { get; set; }

    /// <summary>
    /// Czy faktura posiada załącznik.
    /// </summary>
    public bool HasAttachment { get; set; }

    /// <summary>
    /// Skrót SHA256 faktury.
    /// </summary>
    public string InvoiceHash { get; set; }

    /// <summary>
    /// Skrót SHA256 korygowanej faktury lub null.
    /// </summary>
    public string HashOfCorrectedInvoice { get; set; }

    /// <summary>
    /// Lista podmiotów trzecich (array lub null).
    /// </summary>
    public List<ThirdSubjects> ThirdSubjects { get; set; }

    /// <summary>
    /// Podmiot upoważniony (object lub null).
    /// </summary>
    public AuthorizedSubject AuthorizedSubject { get; set; }
}