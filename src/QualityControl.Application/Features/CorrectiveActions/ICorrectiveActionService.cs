using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.CorrectiveActions.Dtos;

namespace QualityControl.Application.Features.CorrectiveActions;

public interface ICorrectiveActionService
{
    Task<PagedResult<CorrectiveActionListItemDto>> GetPagedAsync(
        CorrectiveActionQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Same filter as <see cref="GetPagedAsync"/>, without paging, for report export.</summary>
    /// <param name="maxRows">Hard cap on the result.</param>
    Task<IReadOnlyList<CorrectiveActionListItemDto>> GetForExportAsync(
        CorrectiveActionQuery query,
        int maxRows,
        CancellationToken cancellationToken = default);

    Task<CorrectiveActionDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Opens a corrective action against a defect, optionally assigning it at once.</summary>
    Task<CorrectiveActionDto> CreateAsync(
        CreateCorrectiveActionRequest request,
        CancellationToken cancellationToken = default);

    Task<CorrectiveActionDto> UpdateAsync(
        int id,
        UpdateCorrectiveActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sets the responsible employee and notifies them.</summary>
    Task<CorrectiveActionDto> AssignAsync(
        int id,
        AssignCorrectiveActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves the action through its workflow. The transition is validated by the
    /// domain and recorded in the action's history.
    /// </summary>
    Task<CorrectiveActionDto> ChangeStatusAsync(
        int id,
        ChangeCorrectiveActionStatusRequest request,
        CancellationToken cancellationToken = default);
}
