using System.Diagnostics;
using System.Security.Claims;

namespace Petshop.Api.Middleware;

public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemName = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[ItemName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceIdentifier"] = context.TraceIdentifier,
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.Value,
            ["Host"] = context.Request.Host.Value,
            ["CompanyId"] = context.User.FindFirstValue("companyId"),
            ["UserId"] = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub"),
            ["UserName"] = context.User.Identity?.Name,
            ["Role"] = context.User.FindFirstValue(ClaimTypes.Role)
        }))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(incoming))
        {
            var trimmed = incoming.Trim();
            if (trimmed.Length <= 100 && trimmed.All(IsSafeCorrelationChar))
                return trimmed;
        }

        return Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier
            ?? Guid.NewGuid().ToString("N");
    }

    private static bool IsSafeCorrelationChar(char value)
    {
        return char.IsLetterOrDigit(value)
            || value is '-' or '_' or '.' or ':';
    }
}
