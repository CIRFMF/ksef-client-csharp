using System;
using System.Collections.Generic;

namespace KSeF.Client.Core.Models.RateLimits;

/// <summary>
/// Klasa rozszerzająca funkcjonalność <see cref="EffectiveApiRateLimits"/> poprzez dodanie metod pomocniczych do pobierania limitów dla określonych grup
/// </summary>
public static class EffectiveApiRateLimitsExtensions
{
    /// <summary>
    /// Zwraca wartości limitów dla określonej grupy limitów żądań API KSeF.
    /// </summary>
    /// <param name="limits">Obiekt zawierający limity żądań API KSeF.</param>
    /// <param name="group">Grupa limitów żądań API KSeF.</param>
    /// <returns>Obiekt zawierający wartości limitów dla określonej grupy.</returns>
    /// <exception cref="ArgumentNullException">Wyrzucany, gdy parametr <paramref name="limits"/> jest null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Wyrzucany, gdy parametr <paramref name="group"/> nie jest obsługiwany.</exception>
    public static EffectiveApiRateLimitValues GetRateLimitValues(
        this EffectiveApiRateLimits limits,
        KsefClientRateLimitGroup group)
    {
        if (limits is null)
        {
            throw new ArgumentNullException(nameof(limits));
        }

        return group switch
        {
            KsefClientRateLimitGroup.OnlineSession => limits.OnlineSession,
            KsefClientRateLimitGroup.BatchSession => limits.BatchSession,
            KsefClientRateLimitGroup.InvoiceSend => limits.InvoiceSend,
            KsefClientRateLimitGroup.InvoiceStatus => limits.InvoiceStatus,
            KsefClientRateLimitGroup.SessionList => limits.SessionList,
            KsefClientRateLimitGroup.SessionInvoiceList => limits.SessionInvoiceList,
            KsefClientRateLimitGroup.SessionMisc => limits.SessionMisc,
            KsefClientRateLimitGroup.InvoiceMetadata => limits.InvoiceMetadata,
            KsefClientRateLimitGroup.InvoiceExport => limits.InvoiceExport,
            KsefClientRateLimitGroup.InvoiceExportStatus => limits.InvoiceExportStatus,
            KsefClientRateLimitGroup.InvoiceDownload => limits.InvoiceDownload,
            KsefClientRateLimitGroup.Other => limits.Other,
            KsefClientRateLimitGroup.CollectiveIdentifier => limits.CollectiveIdentifier,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };
    }

    /// <summary>
    /// Zwraca słownik zawierający wszystkie grupy limitów żądań API KSeF wraz z odpowiadającymi im wartościami limitów.
    /// </summary>
    /// <param name="limits">Obiekt zawierający limity żądań API KSeF.</param>
    /// <returns>Słownik zawierający wszystkie grupy limitów i odpowiadające im wartości.</returns>
    /// <exception cref="ArgumentNullException">Wyrzucany, gdy parametr <paramref name="limits"/> jest null.</exception>
    public static IReadOnlyDictionary<KsefClientRateLimitGroup, EffectiveApiRateLimitValues> GetAllRateLimitValues(
        this EffectiveApiRateLimits limits)
    {
        if (limits is null)
        {
            throw new ArgumentNullException(nameof(limits));
        }

        Dictionary<KsefClientRateLimitGroup, EffectiveApiRateLimitValues> result = new();

        foreach (KsefClientRateLimitGroup group in Enum.GetValues(typeof(KsefClientRateLimitGroup)))
        {
            result[group] = limits.GetRateLimitValues(group);
        }

        return result;
    }
}
