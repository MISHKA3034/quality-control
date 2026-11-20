using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Defects.Dtos;

namespace QualityControl.Application.Features.Defects;

/// <summary>A photograph being uploaded. The caller owns the stream's disposal.</summary>
/// <param name="Content">Image bytes.</param>
/// <param name="FileName">Client-supplied name; used for display and to derive the extension.</param>
/// <param name="ContentType">Client-supplied media type; checked against an allowlist.</param>
/// <param name="Length">Size in bytes, checked against the configured limit.</param>
/// <param name="Caption">Optional caption.</param>
public sealed record PhotoUpload(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string? Caption);

/// <summary>A photograph being served back to a client.</summary>
public sealed record PhotoDownload(Stream Content, string ContentType, string FileName);

public interface IDefectService
{
    Task<PagedResult<DefectListItemDto>> GetPagedAsync(
        DefectQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Same filter as <see cref="GetPagedAsync"/>, without paging, for report export.</summary>
    /// <param name="maxRows">Hard cap on the result.</param>
    Task<IReadOnlyList<DefectListItemDto>> GetForExportAsync(
        DefectQuery query,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<DefectDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<DefectDto> CreateAsync(CreateDefectRequest request, CancellationToken cancellationToken = default);

    Task<DefectDto> UpdateAsync(int id, UpdateDefectRequest request, CancellationToken cancellationToken = default);

    /// <summary>Refused once corrective actions have been raised against the defect.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<DefectPhotoDto> AddPhotoAsync(
        int defectId,
        PhotoUpload upload,
        CancellationToken cancellationToken = default);

    Task<PhotoDownload> GetPhotoContentAsync(
        int defectId,
        int photoId,
        CancellationToken cancellationToken = default);

    Task DeletePhotoAsync(int defectId, int photoId, CancellationToken cancellationToken = default);
}
