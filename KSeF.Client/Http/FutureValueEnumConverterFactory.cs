using System.Text.Json;
using System.Text.Json.Serialization;

namespace KSeF.Client.Http;

/// <summary>
/// Konwerter enumów zawierających wartość <c>FutureValue</c> (kontrakty rewizji operacji).
/// Nieznana wartość zwrócona przez API jest mapowana na <c>FutureValue</c> zamiast powodować wyjątek deserializacji,
/// dzięki czemu nowa wartość biznesowa nie zrywa starszych rewizji (zgodnie z zasadami wersjonowania API KSeF).
/// </summary>
public sealed class FutureValueEnumConverterFactory : JsonConverterFactory
{
    private const string FutureValueName = "FutureValue";

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum && Enum.GetNames(typeToConvert).Contains(FutureValueName);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        Type converterType = typeof(FutureValueEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType);
    }

    private sealed class FutureValueEnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private static readonly T FutureValue = (T)Enum.Parse(typeof(T), FutureValueName);

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString();
                return Enum.TryParse(value, ignoreCase: true, out T parsed) && Enum.IsDefined(typeof(T), parsed)
                    ? parsed
                    : FutureValue;
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt64(out long number))
            {
                object underlying = Enum.ToObject(typeof(T), number);
                return Enum.IsDefined(typeof(T), underlying) ? (T)underlying : FutureValue;
            }

            throw new JsonException($"Nieoczekiwany token JSON {reader.TokenType} dla enuma {typeof(T).Name}.");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
