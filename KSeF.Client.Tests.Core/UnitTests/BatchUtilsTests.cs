using KSeF.Client.Core.Models.Invoices;
using KSeF.Client.Core.Models.Sessions;
using KSeF.Client.Core.Models.Sessions.BatchSession;
using KSeF.Client.Tests.Utils;
using System.IO.Compression;
using System.Text;

namespace KSeF.Client.Tests.Core.UnitTests;

public class BatchUtilsTests
{
    [Fact]
    public void BuildTarGzBytes_ShouldCreateReadableUstarArchiveWithExpectedEntries()
    {
        List<(string FileName, byte[] Content)> files =
        [
            ("faktura_1.xml", Encoding.UTF8.GetBytes("<invoice>1</invoice>")),
            ("nested\\faktura_2.xml", Encoding.UTF8.GetBytes("<invoice>2</invoice>"))
        ];

        byte[] tarGzBytes = BatchUtils.BuildTarGzBytes(files);

        Assert.True(tarGzBytes.Length > 2);
        Assert.Equal((byte)0x1F, tarGzBytes[0]);
        Assert.Equal((byte)0x8B, tarGzBytes[1]);

        Dictionary<string, string> entries = ReadTarGzEntries(tarGzBytes);
        Assert.Equal(2, entries.Count);
        Assert.Equal("<invoice>1</invoice>", entries["faktura_1.xml"]);
        Assert.Equal("<invoice>2</invoice>", entries["nested/faktura_2.xml"]);
    }

    [Fact]
    public async Task UnzipTarGzAsync_ShouldReadArchiveCreatedByBuildTarGzBytes()
    {
        List<(string FileName, byte[] Content)> files =
        [
            ("faktura_1.xml", Encoding.UTF8.GetBytes("<invoice>1</invoice>")),
            ("nested\\faktura_2.xml", Encoding.UTF8.GetBytes("<invoice>2</invoice>"))
        ];

        byte[] tarGzBytes = BatchUtils.BuildTarGzBytes(files);

        Dictionary<string, string> entries = await BatchUtils.UnzipTarGzAsync(tarGzBytes);

        Assert.Equal(2, entries.Count);
        Assert.Equal("<invoice>1</invoice>", entries["faktura_1.xml"]);
        Assert.Equal("<invoice>2</invoice>", entries["nested/faktura_2.xml"]);
    }

    [Fact]
    public void BuildOpenBatchRequest_WithoutCompressionType_KeepsBackwardCompatibility()
    {
        OpenBatchSessionRequest request = BatchUtils.BuildOpenBatchRequest(
            CreateFileMetadata(),
            CreateEncryptionData(),
            CreateEncryptedParts(),
            SystemCode.FA3);

        Assert.NotNull(request.BatchFile);
        Assert.Null(request.BatchFile.CompressionType);
    }

    [Fact]
    public void BuildOpenBatchRequest_WithCompressionTypeTarGz_SetsCompressionType()
    {
        OpenBatchSessionRequest request = BatchUtils.BuildOpenBatchRequest(
            CreateFileMetadata(),
            CreateEncryptionData(),
            CreateEncryptedParts(),
            SystemCode.FA3,
            SystemCodeHelper.GetSchemaVersion(SystemCode.FA3),
            SystemCodeHelper.GetValue(SystemCode.FA3),
            CompressionType.TarGz);

        Assert.NotNull(request.BatchFile);
        Assert.Equal(CompressionType.TarGz, request.BatchFile.CompressionType);
    }

    [Fact]
    public void BuildOpenBatchRequest_WithCompressionTypeZip_SetsCompressionType()
    {
        OpenBatchSessionRequest request = BatchUtils.BuildOpenBatchRequest(
            CreateFileMetadata(),
            CreateEncryptionData(),
            CreateEncryptedParts(),
            SystemCode.FA3,
            SystemCodeHelper.GetSchemaVersion(SystemCode.FA3),
            SystemCodeHelper.GetValue(SystemCode.FA3),
            CompressionType.Zip);

        Assert.NotNull(request.BatchFile);
        Assert.Equal(CompressionType.Zip, request.BatchFile.CompressionType);
    }

    [Fact]
    public void BuildTarGzBytes_WhenPathExceedsUstarLimit_ThrowsArgumentException()
    {
        string segment = new('a', 90);
        // 90 + 1 + 90 + 1 + 90 + 4 = 276B i nie da się rozdzielić do prefix<=155/name<=100.
        string invalidPath = $"{segment}/{segment}/{segment}.xml";

        List<(string FileName, byte[] Content)> files =
        [
            (invalidPath, Encoding.UTF8.GetBytes("<invoice>1</invoice>"))
        ];

        ArgumentException ex = Assert.Throws<ArgumentException>(() => BatchUtils.BuildTarGzBytes(files));
        Assert.Contains("USTAR", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static FileMetadata CreateFileMetadata() => new()
    {
        FileSize = 1024,
        HashSHA = "batch-hash"
    };

    private static EncryptionData CreateEncryptionData() => new()
    {
        CipherKey = [1, 2, 3],
        CipherIv = [4, 5, 6],
        EncryptionInfo = new EncryptionInfo
        {
            EncryptedSymmetricKey = "encrypted-key",
            InitializationVector = "iv",
            PublicKeyId = "pkid"
        }
    };

    private static List<BatchPartSendingInfo> CreateEncryptedParts() =>
    [
        new BatchPartSendingInfo
        {
            Data = [1, 2, 3],
            OrdinalNumber = 1,
            Metadata = new FileMetadata
            {
                FileSize = 512,
                HashSHA = "part-hash"
            }
        }
    ];

    private static Dictionary<string, string> ReadTarGzEntries(byte[] tarGzBytes)
    {
        using MemoryStream input = new(tarGzBytes);
        using GZipStream gzip = new(input, CompressionMode.Decompress);
        using MemoryStream tarStream = new();
        gzip.CopyTo(tarStream);

        byte[] tarBytes = tarStream.ToArray();
        Dictionary<string, string> entries = new(StringComparer.OrdinalIgnoreCase);

        const int blockSize = 512;
        int offset = 0;

        while (offset + blockSize <= tarBytes.Length)
        {
            if (IsZeroBlock(tarBytes, offset, blockSize))
            {
                break;
            }

            string name = ReadNullTerminatedString(tarBytes, offset, 100);
            string prefix = ReadNullTerminatedString(tarBytes, offset + 345, 155);
            if (!string.IsNullOrWhiteSpace(prefix))
            {
                name = $"{prefix}/{name}";
            }

            long size = ParseTarOctal(tarBytes, offset + 124, 12);
            int contentOffset = offset + blockSize;
            if (contentOffset + size > tarBytes.Length)
            {
                throw new InvalidOperationException("Nieprawidłowy rozmiar wpisu TAR.");
            }

            int contentLength = checked((int)size);
            byte[] contentBytes = new byte[contentLength];
            Array.Copy(tarBytes, contentOffset, contentBytes, 0, contentLength);
            entries[name] = Encoding.UTF8.GetString(contentBytes);

            int paddedSize = (int)(((size + blockSize - 1) / blockSize) * blockSize);
            offset = contentOffset + paddedSize;
        }

        return entries;
    }

    private static bool IsZeroBlock(byte[] bytes, int offset, int length)
    {
        for (int i = 0; i < length; i++)
        {
            if (bytes[offset + i] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static string ReadNullTerminatedString(byte[] bytes, int offset, int length)
    {
        int end = offset;
        int limit = offset + length;
        while (end < limit && bytes[end] != 0)
        {
            end++;
        }

        return Encoding.UTF8.GetString(bytes, offset, end - offset);
    }

    private static long ParseTarOctal(byte[] bytes, int offset, int length)
    {
        int end = offset + length;
        while (end > offset && (bytes[end - 1] == 0 || bytes[end - 1] == (byte)' '))
        {
            end--;
        }

        if (end <= offset)
        {
            return 0;
        }

        string octal = Encoding.ASCII.GetString(bytes, offset, end - offset).Trim();
        return Convert.ToInt64(octal, 8);
    }

}
