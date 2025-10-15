using System.Text.Json;
using System.Text.Json.Serialization;

namespace KSeF.Client.Http;

public static class JsonUtil
{
    // Usuwamy generyczne JsonSerializerOptions na rzecz source-generated JsonTypeInfo<T>

    public static string Serialize<T>(T obj, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        try
        {
            return JsonSerializer.Serialize(obj, typeInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[Serialize] Nie udało się zserializować typu {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    public static T Deserialize<T>(string json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        try
        {
            return JsonSerializer.Deserialize(json, typeInfo)
                   ?? throw new InvalidOperationException($"[Deserialize] Zdeserializowana wartość jest pusta (null) dla typu {typeof(T).Name}. JSON: {Shorten(json)}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"[Deserialize] Nie udało się zdeserializować do typu {typeof(T).Name}. JSON (pierwsze 512 znaków): {Shorten(json)}\nWyjątek: {ex.Message}", ex);
        }
    }

    public static async Task SerializeAsync<T>(T obj, Stream output, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        try
        {
            await JsonSerializer.SerializeAsync(output, obj, typeInfo);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[SerializeAsync] Nie udało się zserializować typu {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    public static async Task<T> DeserializeAsync<T>(Stream input, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        try
        {
            var result = await JsonSerializer.DeserializeAsync(input, typeInfo);
            if (result == null)
            {
                throw new InvalidOperationException($"[DeserializeAsync] Zdeserializowana wartość jest pusta (null) dla typu {typeof(T).Name}.");
            }
            return result;
        }
        catch (Exception ex)
        {
            string jsonFragment = null;
            try
            {
                // Próbuj odczytać fragment JSON ze streama (jeśli możliwe)
                if (input.CanSeek)
                {
                    input.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(input, leaveOpen: true);
                    jsonFragment = await reader.ReadToEndAsync();
                }
            }
            catch { /* nie psuj głównego wyjątku */ }

            throw new InvalidOperationException(
                $"[DeserializeAsync] Nie udało się zdeserializować do typu {typeof(T).Name}."
                + (jsonFragment != null ? $" JSON (pierwsze 512 znaków): {Shorten(jsonFragment)}" : "")
                + $"\nWyjątek: {ex.Message}", ex);
        }
    }

    private static string Shorten(string input, int maxLen = 512)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        if (input.Length <= maxLen) return input;
        return input.Substring(0, maxLen) + "...";
    }
}