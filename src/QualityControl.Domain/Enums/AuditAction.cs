namespace QualityControl.Domain.Enums;

/// <summary>
/// Kind of user action recorded in the audit log.
/// </summary>
public enum AuditAction
{
    Login = 1,
    LoginFailed = 2,
    Logout = 3,
    Create = 4,
    Update = 5,
    Delete = 6,
    Archive = 7,
    Restore = 8,
    Inspection = 9,
    DefectRegistered = 10,
    CorrectiveActionStatusChanged = 11,
    PasswordResetRequested = 12,
    PasswordResetCompleted = 13,
    Export = 14
}
