using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.QualityInspections.Dtos;

namespace QualityControl.Application.Features.QualityInspections;

public interface IQualityInspectionService
{
    Task<PagedResult<QualityInspectionListItemDto>> GetPagedAsync(
        QualityInspectionQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same filter as <see cref="GetPagedAsync"/>, without paging, for report export.
    /// </summary>
    /// <param name="maxRows">
    /// Hard cap on the result. An export is still a single response held in memory,
    /// so it needs a ceiling — just a far higher one than a screen page.
    /// </param>
    Task<IReadOnlyList<QualityInspectionListItemDto>> GetForExportAsync(
        QualityInspectionQuery query,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<QualityInspectionDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Files an inspection act: records the act with its result lines and defects, and
    /// moves the batch to the status implied by the verdict. All of it in one transaction.
    /// </summary>
    Task<QualityInspectionDto> CreateAsync(
        CreateQualityInspectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Corrects a filed act. Only the inspector who signed it — or an administrator —
    /// may do so.
    /// </summary>
    Task<QualityInspectionDto> UpdateAsync(
        int id,
        UpdateQualityInspectionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Administrator-only. Refused once corrective actions depend on the act's defects.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
