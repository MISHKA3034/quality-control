using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QualityControl.Application.Common.Exceptions;
using QualityControl.Application.Common.Extensions;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Users.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;
using QualityControl.Domain.Exceptions;

namespace QualityControl.Application.Features.Users;

/// <inheritdoc cref="IUserService"/>
public class UserService : IUserService
{
    private static readonly Dictionary<string, Expression<Func<User, object?>>> Sorts =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["userName"] = u => u.UserName,
            ["email"] = u => u.Email,
            ["role"] = u => u.Role.Name,
            ["lastLoginAt"] = u => u.LastLoginAt,
            ["createdAt"] = u => u.CreatedAt
        };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditService _audit;

    public UserService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IAuditService audit)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
    }

    public Task<PagedResult<UserDto>> GetPagedAsync(
        UserQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim();

        var source = _unitOfWork.Repository<User>()
            .Query()
            .WhereIf(query.RoleId is not null, u => u.RoleId == query.RoleId)
            .WhereIf(query.IsActive is not null, u => u.IsActive == query.IsActive)
            .WhereIf(
                !string.IsNullOrWhiteSpace(search),
                u => EF.Functions.Like(u.UserName, $"%{search}%")
                     || EF.Functions.Like(u.Email, $"%{search}%")
                     || (u.Employee != null && EF.Functions.Like(u.Employee.LastName, $"%{search}%")))
            .ApplySort(query.SortBy, query.SortDescending, Sorts, u => u.UserName);

        return source.ToPagedResultAsync<User, UserDto>(_mapper.ConfigurationProvider, query, cancellationToken);
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == id)
            .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Пользователь", id);

    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await _unitOfWork.Repository<Role>()
            .Query()
            .OrderBy(r => r.Id)
            .ProjectTo<RoleDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

    public async Task<UserDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();
        var email = request.Email.Trim();

        await EnsureUserNameIsFreeAsync(userName, null, cancellationToken);
        await EnsureEmailIsFreeAsync(email, null, cancellationToken);
        await EnsureRoleExistsAsync(request.RoleId, cancellationToken);
        await EnsureEmployeeIsFreeAsync(request.EmployeeId, null, cancellationToken);

        var user = new User
        {
            UserName = userName,
            Email = email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            RoleId = request.RoleId,
            EmployeeId = request.EmployeeId,
            IsActive = true
        };

        await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Create,
            nameof(User),
            user.Id.ToString(),
            $"Создана учётная запись «{user.UserName}».",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Пользователь", id);

        var email = request.Email.Trim();

        await EnsureEmailIsFreeAsync(email, id, cancellationToken);
        await EnsureRoleExistsAsync(request.RoleId, cancellationToken);
        await EnsureEmployeeIsFreeAsync(request.EmployeeId, id, cancellationToken);

        // Locking yourself out of your own admin session is a support call; refuse it.
        if (id == _currentUser.UserId && !request.IsActive)
        {
            throw new DomainException("Нельзя отключить собственную учётную запись.");
        }

        var losesAdmin = user.RoleId != request.RoleId || !request.IsActive;

        if (losesAdmin)
        {
            await EnsureNotLastActiveAdministratorAsync(user, cancellationToken);
        }

        user.Email = email;
        user.RoleId = request.RoleId;
        user.EmployeeId = request.EmployeeId;
        user.IsActive = request.IsActive;

        // A disabled account must not keep working off a live refresh token.
        if (!request.IsActive)
        {
            await RevokeSessionsAsync(id, "Учётная запись отключена.", cancellationToken);
        }

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Update,
            nameof(User),
            id.ToString(),
            $"Изменена учётная запись «{user.UserName}».",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task ResetPasswordAsync(
        int id,
        AdminResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Пользователь", id);

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;

        await RevokeSessionsAsync(id, "Пароль сброшен администратором.", cancellationToken);

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Update,
            nameof(User),
            id.ToString(),
            $"Администратор сбросил пароль пользователя «{user.UserName}».",
            cancellationToken: cancellationToken);
    }

    public async Task UnlockAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Пользователь", id);

        user.FailedLoginAttempts = 0;
        user.LockoutEndsAt = null;

        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Update,
            nameof(User),
            id.ToString(),
            $"Снята блокировка учётной записи «{user.UserName}».",
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Пользователь", id);

        if (id == _currentUser.UserId)
        {
            throw new DomainException("Нельзя удалить собственную учётную запись.");
        }

        await EnsureNotLastActiveAdministratorAsync(user, cancellationToken);
        await RevokeSessionsAsync(id, "Учётная запись удалена.", cancellationToken);

        _unitOfWork.Repository<User>().Remove(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(
            AuditAction.Delete,
            nameof(User),
            id.ToString(),
            $"Удалена учётная запись «{user.UserName}».",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Refuses to strip the last usable administrator. Without this, one careless
    /// edit leaves the installation with nobody who can manage accounts — and no
    /// in-app way back.
    /// </summary>
    private async Task EnsureNotLastActiveAdministratorAsync(User user, CancellationToken cancellationToken)
    {
        var isAdministrator = await _unitOfWork.Repository<Role>()
            .Query()
            .AnyAsync(r => r.Id == user.RoleId && r.Name == RoleNames.Administrator, cancellationToken);

        if (!isAdministrator)
        {
            return;
        }

        var otherAdministrators = await _unitOfWork.Repository<User>()
            .Query()
            .CountAsync(
                u => u.Id != user.Id && u.IsActive && u.Role.Name == RoleNames.Administrator,
                cancellationToken);

        if (otherAdministrators == 0)
        {
            throw new DomainException(
                "Это последняя активная учётная запись администратора. "
                + "Назначьте другого администратора перед изменением или удалением этой записи.");
        }
    }

    private async Task RevokeSessionsAsync(int userId, string reason, CancellationToken cancellationToken)
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

    private async Task EnsureUserNameIsFreeAsync(
        string userName,
        int? excludingId,
        CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<User>()
            .ExistsAsync(u => u.UserName == userName && (excludingId == null || u.Id != excludingId),
                cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateUserRequest.UserName)] = [$"Имя пользователя «{userName}» уже занято."]
            });
        }
    }

    private async Task EnsureEmailIsFreeAsync(string email, int? excludingId, CancellationToken cancellationToken)
    {
        var taken = await _unitOfWork.Repository<User>()
            .ExistsAsync(u => u.Email == email && (excludingId == null || u.Id != excludingId), cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateUserRequest.Email)] = [$"Адрес «{email}» уже используется другой учётной записью."]
            });
        }
    }

    private async Task EnsureRoleExistsAsync(int roleId, CancellationToken cancellationToken)
    {
        if (!await _unitOfWork.Repository<Role>().ExistsAsync(r => r.Id == roleId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateUserRequest.RoleId)] = ["Указанная роль не найдена."]
            });
        }
    }

    /// <summary>
    /// One account per employee. Two logins for the same person would make the
    /// signature on an act ambiguous.
    /// </summary>
    private async Task EnsureEmployeeIsFreeAsync(
        int? employeeId,
        int? excludingUserId,
        CancellationToken cancellationToken)
    {
        if (employeeId is null)
        {
            return;
        }

        if (!await _unitOfWork.Repository<Employee>().ExistsAsync(e => e.Id == employeeId, cancellationToken))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateUserRequest.EmployeeId)] = ["Указанный сотрудник не найден."]
            });
        }

        var taken = await _unitOfWork.Repository<User>()
            .ExistsAsync(
                u => u.EmployeeId == employeeId && (excludingUserId == null || u.Id != excludingUserId),
                cancellationToken);

        if (taken)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(CreateUserRequest.EmployeeId)] = ["К этому сотруднику уже привязана учётная запись."]
            });
        }
    }
}
