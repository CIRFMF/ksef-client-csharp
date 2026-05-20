using KSeF.Client.Api.Builders.Batch;
using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Http;

namespace KSeF.Client.Tests.Core.UnitTests;

public class OpenBatchSessionRequestBuilderTests
{
    [Fact]
    public void CompressionTypeContract_ShouldMatchOpenApi()
    {
        Assert.Equal(2, typeof(CompressionType).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Length);
        Assert.Equal("Zip", CompressionType.Zip.ToString());
        Assert.Equal("TarGz", CompressionType.TarGz.ToString());
        Assert.Equal(typeof(CompressionType?), typeof(BatchFileInfo).GetProperty(nameof(BatchFileInfo.CompressionType))?.PropertyType);
    }

    [Fact]
    public void Build_WithCompressionTypeTarGz_SetsBatchFileCompressionType()
    {
        // Arrange
        OpenBatchSessionRequest request = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(
                SystemCodeHelper.GetSystemCode(SystemCode.FA3),
                SystemCodeHelper.GetSchemaVersion(SystemCode.FA3),
                SystemCodeHelper.GetValue(SystemCode.FA3))
            .WithBatchFile(fileSize: 1024, fileHash: "batch-hash", compressionType: CompressionType.TarGz)
            .AddBatchFilePart(ordinalNumber: 1, fileSize: 1024, fileHash: "part-hash")
            .EndBatchFile()
            .WithEncryption("encrypted-key", "iv")
            .Build();

        // Assert
        Assert.NotNull(request.BatchFile);
        Assert.Equal(CompressionType.TarGz, request.BatchFile.CompressionType);
    }

    [Fact]
    public void Build_WithoutCompressionType_KeepsBackwardCompatibility()
    {
        // Arrange
        OpenBatchSessionRequest request = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(
                SystemCodeHelper.GetSystemCode(SystemCode.FA3),
                SystemCodeHelper.GetSchemaVersion(SystemCode.FA3),
                SystemCodeHelper.GetValue(SystemCode.FA3))
            .WithBatchFile(fileSize: 1024, fileHash: "batch-hash")
            .AddBatchFilePart(ordinalNumber: 1, fileSize: 1024, fileHash: "part-hash")
            .EndBatchFile()
            .WithEncryption("encrypted-key", "iv")
            .Build();

        // Assert
        Assert.NotNull(request.BatchFile);
        Assert.Null(request.BatchFile.CompressionType);
    }

    [Fact]
    public void Serialize_WithCompressionTypeTarGz_WritesCompressionTypeInPayload()
    {
        // Arrange
        OpenBatchSessionRequest request = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(
                SystemCodeHelper.GetSystemCode(SystemCode.FA3),
                SystemCodeHelper.GetSchemaVersion(SystemCode.FA3),
                SystemCodeHelper.GetValue(SystemCode.FA3))
            .WithBatchFile(fileSize: 1024, fileHash: "batch-hash", compressionType: CompressionType.TarGz)
            .AddBatchFilePart(ordinalNumber: 1, fileSize: 1024, fileHash: "part-hash")
            .EndBatchFile()
            .WithEncryption("encrypted-key", "iv")
            .Build();

        // Act
        string json = JsonUtil.Serialize(request);

        // Assert
        Assert.Contains("\"TarGz\"", json);
        Assert.Contains("compressionType", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_WithCompressionTypeZip_WritesCompressionTypeInPayload()
    {
        // Arrange
        OpenBatchSessionRequest request = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(
                SystemCodeHelper.GetSystemCode(SystemCode.FA3),
                SystemCodeHelper.GetSchemaVersion(SystemCode.FA3),
                SystemCodeHelper.GetValue(SystemCode.FA3))
            .WithBatchFile(fileSize: 1024, fileHash: "batch-hash", compressionType: CompressionType.Zip)
            .AddBatchFilePart(ordinalNumber: 1, fileSize: 1024, fileHash: "part-hash")
            .EndBatchFile()
            .WithEncryption("encrypted-key", "iv")
            .Build();

        // Act
        string json = JsonUtil.Serialize(request);

        // Assert
        Assert.Contains("\"Zip\"", json);
        Assert.Contains("compressionType", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Serialize_WithoutCompressionType_KeepsBackwardCompatibility()
    {
        // Arrange
        OpenBatchSessionRequest request = OpenBatchSessionRequestBuilder
            .Create()
            .WithFormCode(
                SystemCodeHelper.GetSystemCode(SystemCode.FA3),
                SystemCodeHelper.GetSchemaVersion(SystemCode.FA3),
                SystemCodeHelper.GetValue(SystemCode.FA3))
            .WithBatchFile(fileSize: 1024, fileHash: "batch-hash")
            .AddBatchFilePart(ordinalNumber: 1, fileSize: 1024, fileHash: "part-hash")
            .EndBatchFile()
            .WithEncryption("encrypted-key", "iv")
            .Build();

        // Act
        string json = JsonUtil.Serialize(request);

        // Assert
        Assert.DoesNotContain("compressionType", json);
        Assert.DoesNotContain("CompressionType", json);
    }
}
