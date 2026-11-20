using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QualityControl.Application.Features.Auth;
using QualityControl.Application.Features.Auth.Dtos;

namespace QualityControl.API.Controllers;

/// <summary>
/// Аутентификация и управление паролем.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Вход в систему.</summary>
    /// <response code="200">Выданы токены доступа и обновления.</response>
    /// <response code="400">Неверные учётные данные или запись заблокирована.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.LoginAsync(request, cancellationToken));

    /// <summary>Обновление пары токенов по refresh-токену.</summary>
    /// <response code="200">Выдана новая пара токенов; прежний refresh-токен отозван.</response>
    /// <response code="400">Токен недействителен, истёк или уже был использован.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _authService.RefreshAsync(request, cancellationToken));

    /// <summary>Выход из системы: отзыв переданного refresh-токена.</summary>
    /// <response code="204">Токен отозван либо уже был недействителен.</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Запрос ссылки для восстановления пароля.</summary>
    /// <remarks>
    /// Всегда возвращает 204, независимо от того, зарегистрирован ли адрес.
    /// Иначе эндпоинт позволял бы перебором выяснять существующие учётные записи.
    /// </remarks>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Установка нового пароля по ссылке восстановления.</summary>
    /// <response code="204">Пароль изменён, все сессии завершены.</response>
    /// <response code="400">Ссылка недействительна или устарела.</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Смена собственного пароля.</summary>
    /// <response code="204">Пароль изменён, остальные сессии завершены.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Профиль текущего пользователя.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await _authService.GetCurrentUserAsync(cancellationToken));
}
