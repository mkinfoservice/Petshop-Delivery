using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Master;
using Petshop.Api.Services.Master;

namespace Petshop.Api.Controllers;

[ApiController]
[Route("master/integrations/whatsapp/platform")]
[Authorize(Roles = "master_admin")]
public class MasterPlatformWhatsappController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MasterCryptoService _crypto;

    public MasterPlatformWhatsappController(AppDbContext db, MasterCryptoService crypto)
    {
        _db = db;
        _crypto = crypto;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct = default)
    {
        var cfg = await _db.PlatformWhatsappConfigs
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (cfg is null)
            return Ok(new PlatformWhatsappConfigDto(null, null, false, "pt_BR", null, null, false));

        return Ok(MapPlatform(cfg));
    }

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertPlatformWhatsappConfigRequest req, CancellationToken ct = default)
    {
        var cfg = await _db.PlatformWhatsappConfigs
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (cfg is null)
        {
            cfg = new PlatformWhatsappConfig();
            _db.PlatformWhatsappConfigs.Add(cfg);
        }

        if (req.WabaId is not null)                    cfg.WabaId                    = req.WabaId.Trim();
        if (req.PhoneNumberId is not null)             cfg.PhoneNumberId             = req.PhoneNumberId.Trim();
        if (req.AccessToken is not null)               cfg.AccessTokenEncrypted      = _crypto.Encrypt(req.AccessToken);
        if (req.TemplateLanguageCode is not null)      cfg.TemplateLanguageCode      = req.TemplateLanguageCode.Trim();
        if (req.NotifyOnStatusesJson is not null)      cfg.NotifyOnStatusesJson      = req.NotifyOnStatusesJson;
        if (req.NotificationTemplatesJson is not null) cfg.NotificationTemplatesJson = req.NotificationTemplatesJson;
        if (req.IsActive.HasValue)                     cfg.IsActive                  = req.IsActive.Value;

        cfg.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(MapPlatform(cfg));
    }

    /// <summary>
    /// Copia as credenciais WhatsApp de uma empresa para a config global da plataforma.
    /// Copia também os templates e statuses de notificação.
    /// </summary>
    [HttpPost("import-from-company/{companyId:guid}")]
    public async Task<IActionResult> ImportFromCompany(Guid companyId, CancellationToken ct = default)
    {
        var source = await _db.CompanyIntegrationsWhatsapp
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.CompanyId == companyId && w.Mode == "cloud_api", ct);

        if (source is null)
            return NotFound("Empresa não possui integração WhatsApp no modo cloud_api configurada.");

        var cfg = await _db.PlatformWhatsappConfigs
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (cfg is null)
        {
            cfg = new PlatformWhatsappConfig();
            _db.PlatformWhatsappConfigs.Add(cfg);
        }

        cfg.WabaId                    = source.WabaId;
        cfg.PhoneNumberId             = source.PhoneNumberId;
        cfg.AccessTokenEncrypted      = source.AccessTokenEncrypted; // mesma chave — cópia direta
        cfg.TemplateLanguageCode      = source.TemplateLanguageCode ?? "pt_BR";
        cfg.NotifyOnStatusesJson      = source.NotifyOnStatuses;
        cfg.NotificationTemplatesJson = source.NotificationTemplatesJson;
        cfg.IsActive                  = true;
        cfg.UpdatedAtUtc              = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(MapPlatform(cfg));
    }

    /// <summary>
    /// Aplica a config global da plataforma a uma empresa específica.
    /// Cria ou atualiza a CompanyIntegrationWhatsapp com todos os campos da config global:
    /// credenciais (WABA, PhoneId, Token), templates, statuses de notificação e idioma.
    /// Não altera a config global nem a de outras empresas.
    /// </summary>
    [HttpPost("apply-to-company/{companyId:guid}")]
    public async Task<IActionResult> ApplyToCompany(Guid companyId, CancellationToken ct = default)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId && !c.IsDeleted, ct);
        if (company is null)
            return NotFound("Empresa não encontrada.");

        var cfg = await _db.PlatformWhatsappConfigs
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (cfg is null || !cfg.IsActive)
            return BadRequest(new { error = "Config global não configurada ou inativa. Acesse Plataforma → WhatsApp para configurar." });

        if (cfg.AccessTokenEncrypted is null)
            return BadRequest(new { error = "Config global não possui Access Token. Configure-o em Plataforma → WhatsApp antes de aplicar." });

        var w = await _db.CompanyIntegrationsWhatsapp
            .FirstOrDefaultAsync(x => x.CompanyId == companyId, ct);

        if (w is null)
        {
            w = new CompanyIntegrationWhatsapp { CompanyId = companyId };
            _db.CompanyIntegrationsWhatsapp.Add(w);
        }

        w.Mode                      = "cloud_api";
        w.WabaId                    = cfg.WabaId;
        w.PhoneNumberId             = cfg.PhoneNumberId;
        w.AccessTokenEncrypted      = cfg.AccessTokenEncrypted; // mesma chave de criptografia
        w.TemplateLanguageCode      = cfg.TemplateLanguageCode;
        w.NotifyOnStatuses          = cfg.NotifyOnStatusesJson;
        w.NotificationTemplatesJson = cfg.NotificationTemplatesJson;
        w.IsActive                  = true;
        w.UpdatedAtUtc              = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new WhatsappIntegrationDto(
            w.Id,
            w.CompanyId,
            w.Mode,
            w.WabaId,
            w.PhoneNumberId,
            w.AccessTokenEncrypted is not null,
            w.WebhookSecret,
            w.NotifyOnStatuses,
            w.NotificationTemplatesJson,
            w.TemplateLanguageCode,
            w.IsActive,
            w.CreatedAtUtc,
            w.UpdatedAtUtc
        ));
    }

    // ── Helpers ────────────────────────────────────────────────

    private static PlatformWhatsappConfigDto MapPlatform(PlatformWhatsappConfig cfg) => new(
        cfg.WabaId,
        cfg.PhoneNumberId,
        cfg.AccessTokenEncrypted is not null,
        cfg.TemplateLanguageCode,
        cfg.NotifyOnStatusesJson,
        cfg.NotificationTemplatesJson,
        cfg.IsActive
    );
}

public record PlatformWhatsappConfigDto(
    string? WabaId,
    string? PhoneNumberId,
    bool HasAccessToken,
    string TemplateLanguageCode,
    string? NotifyOnStatusesJson,
    string? NotificationTemplatesJson,
    bool IsActive
);

public record UpsertPlatformWhatsappConfigRequest(
    string? WabaId,
    string? PhoneNumberId,
    string? AccessToken,
    string? TemplateLanguageCode,
    string? NotifyOnStatusesJson,
    string? NotificationTemplatesJson,
    bool? IsActive
);
