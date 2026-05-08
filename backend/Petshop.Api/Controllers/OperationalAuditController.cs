using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Contracts.Admin.Audit;
using Petshop.Api.Data;

namespace Petshop.Api.Controllers;

[ApiController]
[Route("admin/audit/operational")]
[Authorize(Roles = "admin,gerente")]
public class OperationalAuditController : ControllerBase
{
    private readonly AppDbContext _db;
    private static readonly string[] SensitiveActions =
    [
        "order.delivery.purge",
        "deliverer.pin.update",
        "feature_flags.update",
        "storefront.branding.update",
        "storefront.slide.delete",
        "route.delete",
        "dav.delete"
    ];

    public OperationalAuditController(AppDbContext db)
    {
        _db = db;
    }

    private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var last24Hours = now.AddHours(-24);
        var last7Days = now.AddDays(-7);

        var baseQuery = _db.OperationalAuditLogs
            .AsNoTracking()
            .Where(a => a.CompanyId == CompanyId);

        var last24Query = baseQuery.Where(a => a.CreatedAtUtc >= last24Hours);

        var totalLast24Hours = await last24Query.CountAsync(ct);
        var totalLast7Days = await baseQuery.CountAsync(a => a.CreatedAtUtc >= last7Days, ct);
        var sensitiveLast24Hours = await last24Query.CountAsync(a => SensitiveActions.Contains(a.Action), ct);
        var uniqueActorsLast24Hours = await last24Query
            .Select(a => a.ActorUsername)
            .Distinct()
            .CountAsync(ct);
        var latestEventAtUtc = await baseQuery
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => (DateTime?)a.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        var topActionsLast24Hours = await last24Query
            .GroupBy(a => a.Action)
            .Select(g => new OperationalAuditActionSummaryItem(g.Key, g.Count()))
            .OrderByDescending(a => a.Total)
            .ThenBy(a => a.Action)
            .Take(6)
            .ToListAsync(ct);

        return Ok(new OperationalAuditSummaryResponse(
            totalLast24Hours,
            totalLast7Days,
            sensitiveLast24Hours,
            uniqueActorsLast24Hours,
            latestEventAtUtc,
            topActionsLast24Hours));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? action = null,
        [FromQuery] string? targetType = null,
        [FromQuery] string? targetId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.OperationalAuditLogs
            .AsNoTracking()
            .Where(a => a.CompanyId == CompanyId);

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalized = action.Trim();
            query = query.Where(a => a.Action == normalized);
        }

        if (!string.IsNullOrWhiteSpace(targetType))
        {
            var normalized = targetType.Trim();
            query = query.Where(a => a.TargetType == normalized);
        }

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            var normalized = targetId.Trim();
            query = query.Where(a => a.TargetId == normalized);
        }

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var normalized = correlationId.Trim();
            query = query.Where(a => a.CorrelationId == normalized);
        }

        if (from.HasValue)
            query = query.Where(a => a.CreatedAtUtc >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAtUtc <= to.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new OperationalAuditListItem(
                a.Id,
                a.Action,
                a.TargetType,
                a.TargetId,
                a.TargetName,
                a.ActorUsername,
                a.ActorRole,
                a.CorrelationId,
                a.CreatedAtUtc,
                a.PayloadJson != null))
            .ToListAsync(ct);

        return Ok(new OperationalAuditListResponse(page, pageSize, total, items));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var item = await _db.OperationalAuditLogs
            .AsNoTracking()
            .Where(a => a.Id == id && a.CompanyId == CompanyId)
            .Select(a => new OperationalAuditDetailResponse(
                a.Id,
                a.CompanyId,
                a.CompanySlug,
                a.ActorId,
                a.ActorUsername,
                a.ActorRole,
                a.Action,
                a.TargetType,
                a.TargetId,
                a.TargetName,
                a.PayloadJson,
                a.CorrelationId,
                a.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);

        if (item is null) return NotFound();
        return Ok(item);
    }
}
