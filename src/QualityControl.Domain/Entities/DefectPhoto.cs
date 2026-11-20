using QualityControl.Domain.Common;

namespace QualityControl.Domain.Entities;

/// <summary>
/// Photographic evidence attached to a defect. Only metadata lives in the database;
/// the bytes are kept on disk under the uploads root and served by file name.
/// </summary>
public class DefectPhoto : BaseEntity
{
    public int DefectId { get; set; }

    public Defect Defect { get; set; } = null!;

    /// <summary>Original name as uploaded, shown to users and used for downloads.</summary>
    public string OriginalFileName { get; set; } = null!;

    /// <summary>
    /// Generated name on disk. Never derived from user input, so an uploaded name
    /// cannot escape the uploads directory or collide with another file.
    /// </summary>
    public string StoredFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public string? Caption { get; set; }

    public DateTime UploadedAt { get; set; }

    public int? UploadedByUserId { get; set; }

    public User? UploadedByUser { get; set; }
}
