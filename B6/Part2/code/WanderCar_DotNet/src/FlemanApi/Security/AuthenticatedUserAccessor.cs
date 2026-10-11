using System.Security.Claims;
using FlemanApi.Exceptions;
using System.Net;

namespace FlemanApi.Security;

public record CurrentUser(long UserId, string Email, string Role);

public interface IAuthenticatedUserAccessor
{
    bool IsAuthenticated { get; }
    CurrentUser GetCurrentUser();
    long GetCurrentUserId();
    string GetCurrentRole();
}

// Mirrors com.fleman.security.AuthenticatedUser — a thin wrapper over the
// current request's ClaimsPrincipal (populated by JWT bearer auth), used by
// controllers/services instead of parameter-binding [ClaimsPrincipal] every time.
public class AuthenticatedUserAccessor : IAuthenticatedUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticatedUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public CurrentUser GetCurrentUser()
    {
        var principal = _httpContextAccessor.HttpContext?.User
            ?? throw new ApiException("Not authenticated", HttpStatusCode.Unauthorized);

        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub")
                  ?? throw new ApiException("Not authenticated", HttpStatusCode.Unauthorized);

        var email = principal.FindFirstValue("email") ?? string.Empty;
        var role = principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role") ?? string.Empty;

        return new CurrentUser(long.Parse(sub), email, role);
    }

    public long GetCurrentUserId() => GetCurrentUser().UserId;

    public string GetCurrentRole() => GetCurrentUser().Role;
}
