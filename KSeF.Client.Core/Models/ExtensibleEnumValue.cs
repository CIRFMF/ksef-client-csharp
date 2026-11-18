using System;

namespace KSeF.Client.Core.Models
{
    /// <summary>
    /// Rozszerzalna wartość wyliczeniowa zwracana przez API KSeF w postaci obiektu
    /// z polami <c>code</c>, <c>rawCode</c> i <c>displayName</c> (kontrakty rewizji operacji, docelowo od API v3).
    /// </summary>
    /// <typeparam name="TCode">
    /// Zamknięty typ wyliczeniowy kodów rozpoznawanych przez daną rewizję operacji.
    /// Powinien zawierać wartość zastępczą <c>FutureValue</c>.
    /// </typeparam>
    /// <remarks>
    /// Zgodnie z zasadami wersjonowania i zgodności API KSeF:
    /// <list type="bullet">
    /// <item><description><see cref="Code"/> służy do logiki przewidzianej przez daną rewizję operacji; dla wartości nieznanej przyjmuje wartość zastępczą <c>FutureValue</c>.</description></item>
    /// <item><description><see cref="RawCode"/> to rzeczywisty, stabilny kod wartości; należy używać go wyłącznie do diagnostyki, zachowania informacji lub bezpiecznej prezentacji.</description></item>
    /// <item><description><see cref="DisplayName"/> to tekst prezentacyjny; może się zmieniać i nie może być podstawą logiki biznesowej ani trwałym identyfikatorem.</description></item>
    /// </list>
    /// </remarks>
    public class ExtensibleEnumValue<TCode>
        where TCode : struct, Enum
    {
        /// <summary>
        /// Kod rozpoznawany przez bieżącą rewizję operacji. Dla wartości nieznanej przyjmuje wartość zastępczą <c>FutureValue</c>.
        /// </summary>
        public TCode Code { get; set; }

        /// <summary>
        /// Rzeczywisty, stabilny kod wartości zwrócony przez API. Dla wartości znanej jest równy <see cref="Code"/>.
        /// </summary>
        public string RawCode { get; set; }

        /// <summary>
        /// Nazwa przeznaczona do prezentacji użytkownikowi. Nie jest stabilnym identyfikatorem.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
