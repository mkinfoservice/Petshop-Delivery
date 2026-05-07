using System.Security.Claims;
using System.Text.Json;
using Petshop.Api.Data;
using Petshop.Api.Entities.Audit;
using Petshop.Api.Middleware;

namespace Petshop.Api.Services.Audit;

public class OperationalAuditService
{
    private readonly AppDbContext _db;

    public OperationalAuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        HttpContext? httpContext,
        string action,
        string targetType,
        string targetId,
        Guid? companyId = null,
        string? companySlug = null,
        string? targetName = null,
        object? payload = null,
        CancellationToken ct = default)
    {
        var actor = httpContext?.User;
        var resolvedCompanyId = companyId ?? ReadCompanyId(actor);

        _db.OperationalAuditLogs.Add(new OperationalAuditLog
        {
            CompanyId = resolvedCompanyId,
            CompanySlug = Truncate(companySlug, 120),
            ActorId = Truncate(ReadFirstClaim(actor, ClaimTypes.NameIdentifier, "sub"), 100),
            ActorUsername = Truncate(actor?.Identity?.Name ?? ReadFirstClaim(actor, "username", "email") ?? "unknown", 120) ?? "unknown",
            ActorRole = Truncate(ReadFirstClaim(actor, ClaimTypes.Role, "role") ?? "unknown", 40) ?? "unknown",
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            TargetName = Truncate(targetName, 200),
            PayloadJson = payload is not null
                ? JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false })
                : null,
            IpAddress = Truncate(httpContext?.Connection.RemoteIpAddress?.ToString(), 45),
            UserAgent = Truncate(httpContext?.Request.Headers.UserAgent.ToString(), 300),
            CorrelationId = Truncate(ReadCorrelationId(httpContext), 100),
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    private static Guid? ReadCompanyId(ClaimsPrincipal? actor)
    {
        var raw = ReadFirstClaim(actor, "companyId");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? ReadCorrelationId(HttpContext? httpContext)
    {
        if (httpContext?.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var value) == true)
            return value?.ToString();

        return httpContext?.TraceIdentifier;
    }

    private static string? ReadFirstClaim(ClaimsPrincipal? actor, params string[] claimTypes)
    {
        if (actor is null) return null;

        foreach (var claimType in claimTypes)
        {
            var value = actor.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
