using QualityControl.Application.Common.Models;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.AuditLogs.Dtos;

public class AuditLogDto
{
    public int Id { get; init; }

    public int? UserId { get; init; }

    public string? UserName { get; init; }

    public AuditAction Action { get; init; }

    public string? EntityName { get; init; }

    public string? EntityId { get; init; }

    public string? Description { get; init; }

    /// <summary>JSON diff of changed properties; present on updates.</summary>
    public string? Changes { get; init; }

    public DateTime Timestamp { get; init; }

    public string? IpAddress { get; init; }

    public bool IsSuccess { get; init; }
}

public class AuditLogQuery : PagedRequest
{
    /// <summary>Matches description, user name or entity id.</summary>
    public string? Search { get; set; }

    public int? UserId { get; set; }

    public AuditAction? Action { get; set; }

    public string? EntityName { get; set; }

    public string? EntityId { get; set; }

    public bool? IsSuccess { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }
}
