using KSeF.Client.Core.Models.Sessions;
using System.Globalization;
using System.Text;

namespace KSeF.Client.Extensions;

/// <summary>
/// Extension metod do obsługi filtra <see cref="SessionsFilter"/>.
/// </summary>
public static class SessionsFilterExtensions
{
    private const string IsoInstantFormat = "yyyy-MM-ddTHH:mm:ssZ";

    /// <summary>
    /// Dokleja niepuste właściwości obiektu <see cref="SessionsFilter"/> jako parametry zapytania
    /// do przekazanego <see cref="StringBuilder"/>.
    /// Zakłada, że bazowy adres URL ma już co najmniej jeden parametr (np. "?sessionType=...")
    /// – dlatego kolejne parametry są poprzedzane znakiem '&amp;'.
    /// </summary>
    /// <param name="filter">Instancja filtra</param>
    /// <param name="builder">Obiekt do budowy adresu URL, do którego zostaną dopisane parametry.</param>
    public static void AppendAsQuery(this SessionsFilter filter, StringBuilder builder)
    {
        if (filter == null)
        {
            return;
        }

        Guard.ThrowIfNull(builder);

        void Add(string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
#if NETSTANDARD2_0
                builder.Append(FormattableString.Invariant($"&{name}={Uri.EscapeDataString(value)}"));
#else
                builder.Append(CultureInfo.InvariantCulture,$"&{name}={Uri.EscapeDataString(value)}");
#endif
            }
        }

        Add("referenceNumber", filter.ReferenceNumber);

        if (filter.DateCreatedFrom.HasValue)
        {
            Add("dateCreatedFrom", filter.DateCreatedFrom.Value.UtcDateTime.ToString(IsoInstantFormat, CultureInfo.InvariantCulture));
        }

        if (filter.DateCreatedTo.HasValue)
        {
            Add("dateCreatedTo", filter.DateCreatedTo.Value.UtcDateTime.ToString(IsoInstantFormat, CultureInfo.InvariantCulture));
        }

        if (filter.DateClosedFrom.HasValue)
        {
            Add("dateClosedFrom", filter.DateClosedFrom.Value.UtcDateTime.ToString(IsoInstantFormat, CultureInfo.InvariantCulture));
        }

        if (filter.DateClosedTo.HasValue)
        {
            Add("dateClosedTo", filter.DateClosedTo.Value.UtcDateTime.ToString(IsoInstantFormat, CultureInfo.InvariantCulture));
        }

        if (filter.DateModifiedFrom.HasValue)
        {
            Add("dateModifiedFrom", filter.DateModifiedFrom.Value.UtcDateTime.ToString(IsoInstantFormat, CultureInfo.InvariantCulture));
        }

        if (filter.DateModifiedTo.HasValue)
        {
            Add("dateModifiedTo", filter.DateModifiedTo.Value.UtcDateTime.ToString(IsoInstantFormat, CultureInfo.InvariantCulture));
        }

        if (filter.Statuses != null && filter.Statuses.Count > 0)
        {
            foreach (SessionStatus status in filter.Statuses)
            {
                Add("statuses", status.ToString());
            }
        }
    }
}
