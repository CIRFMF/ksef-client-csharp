using System;

namespace KSeF.Client.Core.Models
{
    /// <summary>
    /// Identyfikator zwracany przez API KSeF w postaci obiektu z polami <c>type</c> i <c>value</c>,
    /// gdzie <c>type</c> jest rozszerzalną wartością wyliczeniową (<see cref="ExtensibleEnumValue{TCode}"/>).
    /// </summary>
    /// <typeparam name="TTypeCode">Zamknięty typ wyliczeniowy kodów typu identyfikatora.</typeparam>
    public class ExtensibleIdentifier<TTypeCode>
        where TTypeCode : struct, Enum
    {
        /// <summary>
        /// Informacje o typie identyfikatora.
        /// </summary>
        public ExtensibleEnumValue<TTypeCode> Type { get; set; }

        /// <summary>
        /// Wartość identyfikatora.
        /// </summary>
        public string Value { get; set; }
    }
}
