using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Common.Models;
using QualityControl.Application.Features.Users;
using QualityControl.Application.Features.Users.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.API.Controllers;

/// <summary>
/// Управление учётными записями. Раздел целиком доступен только администратору.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.Administrator)]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Список учётных записей с поиском и пагинацией.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged(
        [FromQuery] UserQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _userService.GetPagedAsync(query, cancellationToken));

    /// <summary>Справочник ролей.</summary>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken) =>
        Ok(await _userService.GetRolesAsync(cancellationToken));

    /// <summary>Карточка учётной записи.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken) =>
        Ok(await _userService.GetByIdAsync(id, cancellationToken));

    /// <summary>Создание учётной записи.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Изменение учётной записи.</summary>
    /// <response code="409">
    /// Попытка отключить собственную запись либо снять роль с последнего активного администратора.
    /// </response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(
        int id,
        UpdateUserRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _userService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Сброс пароля пользователя администратором.</summary>
    /// <remarks>Завершает все активные сессии этой учётной записи.</remarks>
    [HttpPost("{id:int}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        int id,
        AdminResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ResetPasswordAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Снятие блокировки после неудачных попыток входа.</summary>
    [HttpPost("{id:int}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unlock(int id, CancellationToken cancellationToken)
    {
        await _userService.UnlockAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Удаление учётной записи.</summary>
    /// <response code="409">Попытка удалить себя либо последнего активного администратора.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _userService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
