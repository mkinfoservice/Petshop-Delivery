using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Catalog;
using Petshop.Api.Services.Audit;
using Petshop.Api.Services.Tenancy;

namespace Petshop.Api.Controllers;

[ApiController]
[Route("master/companies/{companyId:guid}/features")]
[Authorize(Roles = "master_admin")]
public class MasterCompanyFeaturesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PlanFeatureService _features;
    private readonly OperationalAuditService _audit;

    public MasterCompanyFeaturesController(
        AppDbContext db,
        PlanFeatureService features,
        OperationalAuditService audit)
    {
        _db = db;
        _features = features;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid companyId, CancellationToken ct = default)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.IsDeleted, ct);

        if (company is null) return NotFound();

        var resolved = await _features.ResolveFeaturesAsync(company, ct);
        var definitions = await _features.ResolveFeatureDefinitionsAsync(company, ct);
        return Ok(new { companyId = company.Id, plan = company.Plan, features = resolved, definitions });
    }

    [HttpPut]
    public async Task<IActionResult> Put(
        Guid companyId,
        [FromBody] UpdateCompanyFeaturesRequest req,
        CancellationToken ct = default)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.IsDeleted, ct);

        if (company is null) return NotFound();
        if (req.Features is null) return BadRequest(new { error = "Body inválido." });

        foreach (var key in req.Features.Keys)
        {
            if (!PlanFeatureService.IsFeatureKeySupported(key))
                return BadRequest(new { error = $"Feature '{key}' não suportada." });
        }

        var normalized = req.Features
            .ToDictionary(k => k.Key.Trim().ToLowerInvariant(), v => v.Value, StringComparer.OrdinalIgnoreCase);

        var beforeResolved = await _features.ResolveFeaturesAsync(company, ct);
        var changedFeatures = normalized
            .Where(pair => beforeResolved.TryGetValue(pair.Key, out var currentValue) && currentValue != pair.Value)
            .Select(pair =>
            {
                var definition = PlanFeatureService.FindDefinition(pair.Key);
                return new FeatureFlagChange(
                    pair.Key,
                    beforeResolved[pair.Key],
                    pair.Value,
                    definition?.Label ?? pair.Key,
                    definition?.Group ?? "unknown",
                    definition?.RiskLevel ?? "unknown",
                    definition?.RequiresExplicitOptIn ?? false);
            })
            .ToList();

        var highRiskChanges = changedFeatures
            .Where(change => string.Equals(change.RiskLevel, "high", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (highRiskChanges.Count > 0 && req.ConfirmHighRisk != true)
        {
            return BadRequest(new
            {
                error = "Alteracao de feature flag de alto risco requer confirmacao explicita.",
                requiredField = nameof(UpdateCompanyFeaturesRequest.ConfirmHighRisk),
                highRiskChanges
            });
        }

        var current = await _db.CompanyFeatureOverrides
            .Where(f => f.CompanyId == companyId)
            .ToListAsync(ct);

        foreach (var pair in normalized)
        {
            var existing = current.FirstOrDefault(x =>
                string.Equals(x.FeatureKey, pair.Key, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                _db.CompanyFeatureOverrides.Add(new CompanyFeatureOverride
                {
                    CompanyId = companyId,
                    FeatureKey = pair.Key,
                    IsEnabled = pair.Value,
                    UpdatedAtUtc = DateTime.UtcNow
                });
                continue;
            }

            existing.IsEnabled = pair.Value;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _features.InvalidateCompany(company.Id, company.Plan);

        await _audit.LogAsync(
            HttpContext,
            action: "company.features.update",
            targetType: "company",
            targetId: company.Id.ToString(),
            companyId: company.Id,
            companySlug: company.Slug,
            targetName: company.Name,
            payload: new
            {
                confirmedHighRisk = req.ConfirmHighRisk == true,
                changes = changedFeatures,
                requestedCount = normalized.Count
            },
            ct: ct);

        var resolved = await _features.ResolveFeaturesAsync(company, ct);
        var definitions = await _features.ResolveFeatureDefinitionsAsync(company, ct);
        return Ok(new { companyId = company.Id, plan = company.Plan, features = resolved, definitions });
    }
}

public record UpdateCompanyFeaturesRequest(
    Dictionary<string, bool> Features,
    bool? ConfirmHighRisk = null);

public record FeatureFlagChange(
    string Key,
    bool OldEnabled,
    bool NewEnabled,
    string Label,
    string Group,
    string RiskLevel,
    bool RequiresExplicitOptIn);
