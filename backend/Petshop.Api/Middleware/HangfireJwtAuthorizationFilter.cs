using System.Security.Claims;
using Hangfire.Dashboard;

namespace Petshop.Api.Middleware;

/// <summary>
/// Restringe o Hangfire Dashboard a tokens JWT com role=admin.
/// Funciona porque UseAuthentication popula HttpContext.User antes do middleware Hangfire.
/// </summary>
public class HangfireJwtAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();

        if (http.User.Identity?.IsAuthenticated != true)
            return false;

        var role = http.User.FindFirstValue(ClaimTypes.Role)
                ?? http.User.FindFirstValue("role");

        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
    }
}
