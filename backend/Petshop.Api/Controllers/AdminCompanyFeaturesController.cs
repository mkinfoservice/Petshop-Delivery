using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Services.Tenancy;
using System.Security.Claims;

namespace Petshop.Api.Controllers;

/// <summary>
/// Expõe as feature flags resolvidas para a empresa do usuário autenticado.
/// Usado pelo frontend admin para adaptar a UI por empresa.
/// </summary>
[ApiController]
[Route("admin/company/features")]
[Authorize(Roles = "admin,gerente,atendente")]
public class AdminCompanyFeaturesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PlanFeatureService _features;

    public AdminCompanyFeaturesController(AppDbContext db, PlanFeatureService features)
    {
        _db = db;
        _features = features;
    }

    private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct = default)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == CompanyId && !c.IsDeleted, ct);

        if (company is null) return NotFound();

        var resolved = await _features.ResolveFeaturesAsync(company, ct);
        return Ok(new { features = resolved });
    }
}
