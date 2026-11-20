using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Options;
using QualityControl.Application.Features.Auth.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.Auth;

/// <inheritdoc cref="IAuthService"/>
public class AuthService : IAuthService
{
    private const string InvalidCredentialsMessage = "Неверное имя пользователя или пароль.";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;
    private readonly AuthOptions _options;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService audit,
        INotificationService notifications,
        IOptions<AuthOptions> options,
        ILogger<AuthService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _notifications = notifications;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var users = _unitOfWork.Repository<User>();
        var login = request.UserName.Trim();

        var user = await users.Query(tracking: true)
            .Include(u => u.Role)
            .Include(u => u.Employee!)
                .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(
                u => u.UserName == login || u.Email == login,
                cancellationToken);

        if (user is null)
        {
            // Hash anyway. Returning early here would make "unknown user" measurably
            // faster than "wrong password" and turn login into an account oracle.
            _passwordHasher.Verify(request.Password, DummyHash);

            await _audit.LogAsync(
                AuditAction.LoginFailed,
                description: $"Попытка входа под несуществующим пользователем «{login}».",
                isSuccess: false,
                userName: login,
                cancellationToken: cancellationToken);

            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(LoginRequest.UserName)] = [InvalidCredentialsMessage]
            });
        }

        var now = _clock.UtcNow;

        if (user.LockoutEndsAt is { } lockoutEnd && lockoutEnd > now)
        {
            var minutes = (int)Math.Ceiling((lockoutEnd - now).TotalMinutes);

            await _audit.LogAsync(
                AuditAction.LoginFailed,
                nameof(User),
                user.Id.ToString(),
                $"Вход заблокирован до {lockoutEnd:HH:mm} UTC.",
                isSuccess: false,
                user.Id,
                user.UserName,
                cancellationToken);

            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(LoginRequest.UserName)] =
                    [$"Учётная запись временно заблокирована. Повторите попытку через {minutes} мин."]
            });
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= _options.MaxFailedLoginAttempts)
            {
                user.LockoutEndsAt = now.AddMinutes(_options.LockoutMinutes);
                user.FailedLoginAttempts = 0;

                _logger.LogWarning(
                    "Учётная запись {UserId} заблокирована до {LockoutEnd} после {Attempts} неудачных попыток входа.",
                    user.Id,
                    user.LockoutEndsAt,
                    _options.MaxFailedLoginAttempts);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(
                AuditAction.LoginFailed,
                nameof(User),
                user.Id.ToString(),
                "Неверный пароль.",
                isSuccess: false,
                user.Id,
                user.UserName,
                cancellationToken);

            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(LoginRequest.UserName)] = [InvalidCredentialsMessage]
            });
        }

        if (!user.IsActive)
        {
            await _audit.LogAsync(
                AuditAction.LoginFailed,
                nameof(User),
                user.Id.ToString(),
                "Учётная запись отключена.",
                isSuccess: false,
                user.Id,
                user.UserName,
                cancellationToken);

            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(LoginRequest.UserName)] = ["Учётная запись отключена. Обратитесь к администратору."]
            });
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;
        user.LastLoginAt = now;

        var response = await IssueTokensAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Login,
            nameof(User),
            user.Id.ToString(),
            "Успешный вход в систему.",
            userId: user.Id,
            userName: user.UserName,
            cancellationToken: cancellationToken);

        return response;
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);
        var tokens = _unitOfWork.Repository<RefreshToken>();

        var stored = await tokens.Query(tracking: true)
            .Include(t => t.User)
                .ThenInclude(u => u.Role)
            .Include(t => t.User)
                .ThenInclude(u => u.Employee!)
                    .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (stored is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(RefreshTokenRequest.RefreshToken)] = ["Недействительный токен обновления."]
            });
        }

        var now = _clock.UtcNow;

        // A token that was already rotated away should never come back. If it does,
        // either it leaked or the client is broken — assume the worst and kill the
        // whole chain, forcing a fresh sign-in.
        if (stored.IsRevoked)
        {
            _logger.LogWarning(
                "Повторное использование отозванного refresh-токена пользователя {UserId}. Все сессии отозваны.",
                stored.UserId);

            await RevokeAllUserTokensAsync(
                stored.UserId,
                "Обнаружено повторное использование отозванного токена.",
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(RefreshTokenRequest.RefreshToken)] =
                    ["Сессия завершена по соображениям безопасности. Выполните вход заново."]
            });
        }

        if (stored.IsExpired(now))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(RefreshTokenRequest.RefreshToken)] = ["Срок действия токена обновления истёк."]
            });
        }

        if (!stored.User.IsActive || stored.User.IsDeleted)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(RefreshTokenRequest.RefreshToken)] = ["Учётная запись отключена."]
            });
        }

        var response = await IssueTokensAsync(stored.User, cancellationToken);

        stored.RevokedAt = now;
        stored.RevokedByIp = _currentUser.IpAddress;
        stored.RevocationReason = "Заменён при обновлении.";
        stored.ReplacedByTokenHash = _tokenService.HashToken(response.RefreshToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(request.RefreshToken);

        var stored = await _unitOfWork.Repository<RefreshToken>()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, tracking: true, cancellationToken);

        // Logging out with an unknown or already-dead token is not an error: the
        // desired end state (that token cannot be used) already holds.
        if (stored is null || stored.IsRevoked)
        {
            return;
        }

        stored.RevokedAt = _clock.UtcNow;
        stored.RevokedByIp = _currentUser.IpAddress;
        stored.RevocationReason = "Выход из системы.";

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Logout,
            nameof(User),
            stored.UserId.ToString(),
            "Выход из системы.",
            userId: stored.UserId,
            cancellationToken: cancellationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        var user = await _unitOfWork.Repository<User>()
            .FirstOrDefaultAsync(u => u.Email == email, tracking: true, cancellationToken);

        // Always return without complaint. Telling the caller that an address is
        // unknown would turn this endpoint into an account-enumeration oracle.
        if (user is null || !user.IsActive)
        {
            _logger.LogInformation(
                "Запрос сброса пароля для неизвестного или отключённого адреса. Ответ не раскрывает существование учётной записи.");
            return;
        }

        var now = _clock.UtcNow;
        var (rawToken, tokenHash) = _tokenService.CreateSecureToken();

        // Invalidate any earlier outstanding request, so only the newest link works.
        var outstanding = await _unitOfWork.Repository<PasswordResetToken>()
            .Query(tracking: true)
            .Where(t => t.UserId == user.Id && t.UsedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in outstanding)
        {
            token.UsedAt = now;
        }

        await _unitOfWork.Repository<PasswordResetToken>().AddAsync(
            new PasswordResetToken
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(_options.PasswordResetTokenLifetimeMinutes),
                RequestedByIp = _currentUser.IpAddress
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.SendPasswordResetAsync(user.Email, user.UserName, rawToken, cancellationToken);

        await _audit.LogAsync(
            AuditAction.PasswordResetRequested,
            nameof(User),
            user.Id.ToString(),
            "Запрошен сброс пароля.",
            userId: user.Id,
            userName: user.UserName,
            cancellationToken: cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var tokenHash = _tokenService.HashToken(request.Token);
        var now = _clock.UtcNow;

        var resetToken = await _unitOfWork.Repository<PasswordResetToken>()
            .Query(tracking: true)
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenHash == tokenHash && t.User.Email == email,
                cancellationToken);

        if (resetToken is null || !resetToken.IsActive(now))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(ResetPasswordRequest.Token)] =
                    ["Ссылка для сброса пароля недействительна или устарела. Запросите новую."]
            });
        }

        resetToken.UsedAt = now;

        var user = resetToken.User;
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;

        // Whoever knew the old password may still hold a live session. A reset is
        // exactly the moment to cut them off.
        await RevokeAllUserTokensAsync(user.Id, "Пароль сброшен.", cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.PasswordResetCompleted,
            nameof(User),
            user.Id.ToString(),
            "Пароль сброшен по ссылке восстановления.",
            userId: user.Id,
            userName: user.UserName,
            cancellationToken: cancellationToken);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenException("Требуется аутентификация.");

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Пользователь", userId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(ChangePasswordRequest.CurrentPassword)] = ["Текущий пароль указан неверно."]
            });
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);

        await RevokeAllUserTokensAsync(user.Id, "Пароль изменён.", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Update,
            nameof(User),
            user.Id.ToString(),
            "Пароль изменён пользователем.",
            userId: user.Id,
            userName: user.UserName,
            cancellationToken: cancellationToken);
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new ForbiddenException("Требуется аутентификация.");

        var user = await _unitOfWork.Repository<User>()
            .Query()
            .Include(u => u.Role)
            .Include(u => u.Employee!)
                .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("Пользователь", userId);

        return MapCurrentUser(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var (accessToken, accessExpiresAt) = _tokenService.CreateAccessToken(user);
        var (rawRefreshToken, refreshTokenHash) = _tokenService.CreateSecureToken();
        var refreshExpiresAt = now.AddDays(_options.RefreshTokenLifetimeDays);

        await _unitOfWork.Repository<RefreshToken>().AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                CreatedAt = now,
                CreatedByIp = _currentUser.IpAddress,
                ExpiresAt = refreshExpiresAt
            },
            cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = MapCurrentUser(user)
        };
    }

    private async Task RevokeAllUserTokensAsync(int userId, string reason, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var active = await _unitOfWork.Repository<RefreshToken>()
            .Query(tracking: true)
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAt = now;
            token.RevokedByIp = _currentUser.IpAddress;
            token.RevocationReason = reason;
        }
    }

    private static CurrentUserDto MapCurrentUser(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        Role = user.Role.Name,
        RoleDisplayName = user.Role.DisplayName,
        EmployeeId = user.EmployeeId,
        FullName = user.Employee?.FullName,
        Position = user.Employee?.Position,
        DepartmentId = user.Employee?.DepartmentId,
        DepartmentName = user.Employee?.Department?.Name
    };

    /// <summary>
    /// A real BCrypt hash of a value nobody knows, used to spend the same time
    /// verifying a password for an account that does not exist.
    /// </summary>
    private const string DummyHash = "$2a$12$C6UzMDM.H6dfI/f/IKcEe.6iVvUKRJ0OcZ8xkFRLj9C3q5eJ5RfKC";
}
