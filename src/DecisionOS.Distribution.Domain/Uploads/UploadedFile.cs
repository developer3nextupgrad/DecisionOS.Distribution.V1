using System.Security.Cryptography;
using System.Text;

namespace DecisionOS.Distribution.Domain.Uploads;

public sealed class UploadedFile
{
    public long Id { get; set; }

    public long UploadBatchId { get; set; }
    public UploadBatch UploadBatch { get; set; } = null!;

    public ReportType ReportType { get; set; }

    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string StoredRelativePath { get; set; } = null!;
    public string Sha256Hex { get; set; } = null!;

    public int HeaderRowNumber { get; set; } = 1;

    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public DateOnly? SnapshotDate { get; set; }

    public DateTimeOffset UploadedAt { get; set; }

    public static string ComputeSha256Hex(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public static string ComputeSha256Hex(string text)
        => ComputeSha256Hex(Encoding.UTF8.GetBytes(text));
}

