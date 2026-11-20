using System.Security.Claims;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Infrastructure.Security;

namespace QualityControl.API.Services;

/// <summary>
/// Reads the caller's identity from the current request. This is the only place
/// ASP.NET Core's HttpContext leaks into the identity abstraction.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    public int? EmployeeId =>
        int.TryParse(Principal?.FindFirstValue(TokenService.EmployeeIdClaim), out var id) ? id : null;

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// Caller's address.
    /// </summary>
    /// <remarks>
    /// This is the socket's remote address. Behind a reverse proxy it is the proxy
    /// unless ForwardedHeaders is configured — see the note in Program.cs. Reading
    /// X-Forwarded-For directly here would be worse: the header is client-supplied
    /// and forgeable unless a trusted proxy has already normalised it.
    /// </remarks>
    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            // The column is nvarchar(512); a longer header must not fail the insert.
            return value.Length > 512 ? value[..512] : value;
        }
    }

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
}
