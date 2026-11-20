using QualityControl.Application.Common.Models;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.CorrectiveActions.Dtos;

public class CorrectiveActionHistoryDto
{
    public int Id { get; init; }

    public CorrectiveActionStatus? FromStatus { get; init; }

    public CorrectiveActionStatus ToStatus { get; init; }

    public int ChangedByUserId { get; init; }

    public string ChangedByUserName { get; init; } = null!;

    public DateTime ChangedAt { get; init; }

    public string? Comment { get; init; }
}

public class CorrectiveActionListItemDto
{
    public int Id { get; init; }

    public int DefectId { get; init; }

    public string DefectDescription { get; init; } = null!;

    public DefectSeverity DefectSeverity { get; init; }

    public string DefectTypeName { get; init; } = null!;

    public int ProductionBatchId { get; init; }

    public string BatchNumber { get; init; } = null!;

    public string ProductName { get; init; } = null!;

    public int DepartmentId { get; init; }

    public string DepartmentName { get; init; } = null!;

    public string Description { get; init; } = null!;

    public CorrectiveActionStatus Status { get; init; }

    public int? AssignedToId { get; init; }

    public string? AssignedToName { get; init; }

    public DateTime? DueDate { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? ClosedAt { get; init; }

    public bool IsOverdue { get; init; }
}

public class CorrectiveActionDto : CorrectiveActionListItemDto
{
    public DateTime? AssignedAt { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? FixedAt { get; init; }

    public DateTime? VerifiedAt { get; init; }

    public string? VerifiedByName { get; init; }

    public string? ResolutionNotes { get; init; }

    public IReadOnlyList<CorrectiveActionHistoryDto> History { get; init; } = [];

    /// <summary>Transitions the domain will currently accept, so the UI shows only real options.</summary>
    public IReadOnlyList<CorrectiveActionStatus> AllowedNextStatuses { get; set; } = [];
}

public sealed record CreateCorrectiveActionRequest(
    int DefectId,
    string Description,
    int? AssignedToId,
    DateTime? DueDate);

public sealed record UpdateCorrectiveActionRequest(
    string Description,
    DateTime? DueDate,
    string? ResolutionNotes);

/// <param name="AssignedToId">Employee to make responsible.</param>
/// <param name="Comment">Optional note stored on the history entry.</param>
public sealed record AssignCorrectiveActionRequest(int AssignedToId, string? Comment);

/// <param name="Status">Requested new status.</param>
/// <param name="Comment">Optional note stored on the history entry.</param>
public sealed record ChangeCorrectiveActionStatusRequest(CorrectiveActionStatus Status, string? Comment);

public class CorrectiveActionQuery : PagedRequest
{
    /// <summary>Matches the action description, the defect description or the batch number.</summary>
    public string? Search { get; set; }

    public int? DefectId { get; set; }

    public int? ProductionBatchId { get; set; }

    public int? DepartmentId { get; set; }

    public int? AssignedToId { get; set; }

    public CorrectiveActionStatus? Status { get; set; }

    public DefectSeverity? Severity { get; set; }

    /// <summary>True returns only unfinished actions past their due date.</summary>
    public bool? IsOverdue { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}
