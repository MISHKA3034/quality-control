using QualityControl.Application.Common.Models;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.DefectTypes.Dtos;

public class DefectTypeDto
{
    public int Id { get; init; }

    public string Code { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public DefectSeverity DefaultSeverity { get; init; }

    public bool IsActive { get; init; }

    /// <summary>How many defects have been registered under this type.</summary>
    public int DefectCount { get; init; }
}

public sealed record CreateDefectTypeRequest(
    string Code,
    string Name,
    string? Description,
    DefectSeverity DefaultSeverity);

public sealed record UpdateDefectTypeRequest(
    string Code,
    string Name,
    string? Description,
    DefectSeverity DefaultSeverity,
    bool IsActive);

public class DefectTypeQuery : PagedRequest
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public DefectSeverity? DefaultSeverity { get; set; }
}
