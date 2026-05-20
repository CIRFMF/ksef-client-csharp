using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Http;

namespace KSeF.Client.Tests.Core.UnitTests;

public class InvoiceExportRequestTests
{
    [Fact]
    public void CompressionTypeContract_ShouldMatchOpenApi()
    {
        Assert.Equal(2, typeof(CompressionType).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Length);
        Assert.Equal("Zip", CompressionType.Zip.ToString());
        Assert.Equal("TarGz", CompressionType.TarGz.ToString());
        Assert.Equal(typeof(CompressionType?), typeof(InvoiceExportRequest).GetProperty(nameof(InvoiceExportRequest.CompressionType))?.PropertyType);
    }

    [Fact]
    public void Serialize_WithCompressionTypeTarGz_WritesCompressionTypeInPayload()
    {
        InvoiceExportRequest request = new()
        {
            CompressionType = CompressionType.TarGz,
            Filters = new InvoiceQueryFilters()
        };

        string json = JsonUtil.Serialize(request);

        Assert.Contains("\"TarGz\"", json);
        Assert.Contains("compressionType", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_WithCompressionTypeZip_WritesCompressionTypeInPayload()
    {
        InvoiceExportRequest request = new()
        {
            CompressionType = CompressionType.Zip,
            Filters = new InvoiceQueryFilters()
        };

        string json = JsonUtil.Serialize(request);

        Assert.Contains("\"Zip\"", json);
        Assert.Contains("compressionType", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_WithoutCompressionType_KeepsBackwardCompatibility()
    {
        InvoiceExportRequest request = new()
        {
            Filters = new InvoiceQueryFilters()
        };

        string json = JsonUtil.Serialize(request);

        Assert.DoesNotContain("compressionType", json);
        Assert.DoesNotContain("CompressionType", json);
    }
}
