using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Users.Dtos;

namespace QualityControl.Application.Features.Users;

/// <summary>
/// Account administration. Every method here is administrator-only; a user's own
/// profile and password go through <see cref="Auth.IAuthService"/>.
/// </summary>
public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default);

    Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sets a new password and ends every session the account has open.</summary>
    Task ResetPasswordAsync(
        int id,
        AdminResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Clears a lockout caused by failed sign-in attempts.</summary>
    Task UnlockAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the account and revokes its sessions.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
