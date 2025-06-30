namespace KSeFClient.Api.Services;

public static class XadeSDummy
{
    public static async Task<string> SignWithPZ(string xml, string directoryPath, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromMinutes(5); // domyślnie 5 min
        var pollInterval = TimeSpan.FromSeconds(1); // poll co sekundę

        // 1. Generuj unikalny guid
        var guid = Guid.NewGuid().ToString("N");
        var baseFileName = $"ksefDummySign_{guid}.xml";
        var fileToSignPath = Path.Combine(directoryPath, baseFileName);
        var fileSignedPath = Path.Combine(directoryPath, $"ksefDummySign_{guid} (1).xml");

        try
        {
            // 2. Zapisz oryginalny plik do podpisu
            await File.WriteAllTextAsync(fileToSignPath, xml);

            Console.WriteLine($"File written: {fileToSignPath}. Waiting for signed version...");

            // 3. Czekaj aż pojawi się plik podpisany (ksefDummySign_{guid}(1).xml)
            var start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
            {
                if (File.Exists(fileSignedPath))
                {
                    // #GreenFlag: plik podpisany jest, odczytujemy i zwracamy zawartość
                    return await File.ReadAllTextAsync(fileSignedPath);
                }
                await Task.Delay(pollInterval);
            }

            // #RedFlag: Timeout – plik się nie pojawił
            throw new TimeoutException($"Timeout: plik podpisany ({fileSignedPath}) nie pojawił się w ciągu {timeout.Value.TotalSeconds} sekund.");
        }
        catch (Exception ex)
        {
            // #PlanRollback: błąd – zwróć info
            return $"#RedFlag: Błąd – {ex.Message}";
        }
    }
}
