namespace QualityControl.Application.Common.Interfaces;

/// <summary>
/// Outbound notifications — password resets and corrective-action assignments.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends the password-reset link. <paramref name="resetToken"/> is the raw
    /// single-use token; it exists only in this message and in the user's mailbox.
    /// </summary>
    Task SendPasswordResetAsync(
        string email,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default);

    /// <summary>Tells an employee that a corrective action is now theirs.</summary>
    Task SendCorrectiveActionAssignedAsync(
        string email,
        string employeeName,
        int correctiveActionId,
        string defectDescription,
        DateTime? dueDate,
        CancellationToken cancellationToken = default);

    /// <summary>Alerts OTK that a critical defect has been registered.</summary>
    Task SendCriticalDefectRegisteredAsync(
        IEnumerable<string> emails,
        string batchNumber,
        string productName,
        string defectDescription,
        CancellationToken cancellationToken = default);
}
