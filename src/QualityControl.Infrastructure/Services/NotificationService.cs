using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Options;

namespace QualityControl.Infrastructure.Services;

/// <summary>
/// Sends notifications by e-mail, or logs them when mail is disabled.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly EmailOptions _email;
    private readonly AuthOptions _auth;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IOptions<EmailOptions> email,
        IOptions<AuthOptions> auth,
        ILogger<NotificationService> logger)
    {
        _email = email.Value;
        _auth = auth.Value;
        _logger = logger;
    }

    public Task SendPasswordResetAsync(
        string email,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        // Built here rather than in the Application layer, which has no business
        // knowing the SPA's route shape.
        var link = $"{_auth.ClientBaseUrl.TrimEnd('/')}/reset-password"
                   + $"?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(resetToken)}";

        var body = new StringBuilder()
            .AppendLine($"Здравствуйте, {userName}.")
            .AppendLine()
            .AppendLine("Для вашей учётной записи в системе контроля качества запрошен сброс пароля.")
            .AppendLine("Чтобы задать новый пароль, перейдите по ссылке:")
            .AppendLine()
            .AppendLine(link)
            .AppendLine()
            .AppendLine($"Ссылка действительна {_auth.PasswordResetTokenLifetimeMinutes} мин.")
            .AppendLine("Если вы не запрашивали сброс пароля, просто проигнорируйте это письмо —")
            .AppendLine("ваш текущий пароль останется без изменений.")
            .ToString();

        return SendAsync(email, "Сброс пароля — АСКК «Амкодор-Пинск»", body, cancellationToken);
    }

    public Task SendCorrectiveActionAssignedAsync(
        string email,
        string employeeName,
        int correctiveActionId,
        string defectDescription,
        DateTime? dueDate,
        CancellationToken cancellationToken = default)
    {
        var body = new StringBuilder()
            .AppendLine($"Здравствуйте, {employeeName}.")
            .AppendLine()
            .AppendLine($"Вам назначено корректирующее действие №{correctiveActionId}.")
            .AppendLine($"Дефект: {defectDescription}")
            .AppendLine(dueDate is null
                ? "Срок исполнения: не установлен."
                : $"Срок исполнения: {dueDate:dd.MM.yyyy}.")
            .AppendLine()
            .AppendLine($"Открыть: {_auth.ClientBaseUrl.TrimEnd('/')}/corrective-actions/{correctiveActionId}")
            .ToString();

        return SendAsync(
            email,
            $"Назначено корректирующее действие №{correctiveActionId}",
            body,
            cancellationToken);
    }

    public async Task SendCriticalDefectRegisteredAsync(
        IEnumerable<string> emails,
        string batchNumber,
        string productName,
        string defectDescription,
        CancellationToken cancellationToken = default)
    {
        var recipients = emails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();

        if (recipients.Count == 0)
        {
            _logger.LogWarning(
                "Критический дефект по партии {BatchNumber} зарегистрирован, но получатели уведомления не найдены.",
                batchNumber);
            return;
        }

        var body = new StringBuilder()
            .AppendLine("Зарегистрирован КРИТИЧЕСКИЙ дефект.")
            .AppendLine()
            .AppendLine($"Партия: {batchNumber}")
            .AppendLine($"Изделие: {productName}")
            .AppendLine($"Описание: {defectDescription}")
            .AppendLine()
            .AppendLine("Требуется решение по партии.")
            .ToString();

        foreach (var recipient in recipients)
        {
            await SendAsync(
                recipient,
                $"Критический дефект — партия {batchNumber}",
                body,
                cancellationToken);
        }
    }

    private async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (!_email.Enabled)
        {
            // Not a silent no-op: without this a developer testing password reset
            // would have no way to obtain the token.
            _logger.LogInformation(
                "Отправка почты отключена (Email:Enabled = false). Письмо для {To}:\nТема: {Subject}\n{Body}",
                to,
                subject,
                body);

            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_email.FromAddress, _email.FromDisplayName, Encoding.UTF8),
            Subject = subject,
            SubjectEncoding = Encoding.UTF8,
            Body = body,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = false
        };

        message.To.Add(to);

        using var client = new SmtpClient(_email.Host, _email.Port)
        {
            EnableSsl = _email.UseSsl,
            Credentials = string.IsNullOrWhiteSpace(_email.UserName)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_email.UserName, _email.Password)
        };

        await client.SendMailAsync(message, cancellationToken);

        _logger.LogInformation("Отправлено письмо «{Subject}» на {To}.", subject, to);
    }
}
