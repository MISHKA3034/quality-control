using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.DefectTypes.Dtos;

namespace QualityControl.Application.Features.DefectTypes;

public interface IDefectTypeService
{
    Task<PagedResult<DefectTypeDto>> GetPagedAsync(
        DefectTypeQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Active types for the defect-registration dropdown.</summary>
    Task<IReadOnlyList<DefectTypeDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    Task<DefectTypeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<DefectTypeDto> CreateAsync(CreateDefectTypeRequest request, CancellationToken cancellationToken = default);

    Task<DefectTypeDto> UpdateAsync(
        int id,
        UpdateDefectTypeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Refused once defects reference the type; deactivate it instead.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
