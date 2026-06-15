using System;

namespace KSeF.Client.DI;

/// <summary>
/// Opcje konfiguracji klienta Latarni.
/// </summary>
public sealed class LighthouseClientOptions
{
    /// <summary>
    /// Bazowy adres URL usługi Latarni.
    /// </summary>
    public string BaseUrl { get; set; }
}
