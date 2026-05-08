using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Contracts.Admin.StoreFront;
using Petshop.Api.Data;
using Petshop.Api.Entities.Catalog;
using Petshop.Api.Entities.StoreFront;
using Petshop.Api.Services;
using Petshop.Api.Services.Audit;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Petshop.Api.Controllers;

// ═══════════════════════════════════════════════════════════════════════════
// ADMIN — configuração da loja
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
[Route("admin/storefront")]
[Authorize(Roles = "admin,gerente")]
public class StoreFrontAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly OperationalAuditService _audit;

    public StoreFrontAdminController(AppDbContext db, OperationalAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);

    // ── GET /admin/storefront/categories ─────────────────────────────────────
    /// <summary>Lista grupos de produtos da empresa (para preencher dropdown no formulário de slide).</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var cats = await _db.Categories
            .AsNoTracking()
            .Where(c => c.CompanyId == CompanyId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Slug })
            .ToListAsync(ct);
        return Ok(cats);
    }

    // ── GET /admin/storefront ─────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var config = await GetOrCreateConfig(ct);
        return Ok(ToResponse(config));
    }

    [HttpGet("branding-health")]
    public async Task<IActionResult> BrandingHealth(CancellationToken ct)
    {
        var company = await _db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == CompanyId, ct);

        if (company is null) return NotFound();

        var config = await GetOrCreateConfig(ct);
        return Ok(BuildBrandingHealth(company.Slug, company.Name, config));
    }

    // ── PUT /admin/storefront ─────────────────────────────────────────────────
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateStoreFrontConfigRequest req, CancellationToken ct)
    {
        var config = await GetOrCreateConfig(ct);

        if (req.PrimaryColor is not null)
            config.PrimaryColor = req.PrimaryColor;
        if (req.BannerIntervalSecs.HasValue)
            config.BannerIntervalSecs = Math.Max(0, req.BannerIntervalSecs.Value);
        if (req.LogoUrl    is not null) config.LogoUrl    = req.LogoUrl == "" ? null : req.LogoUrl;
        if (req.StoreName  is not null) config.StoreName  = req.StoreName == "" ? null : req.StoreName;
        if (req.StoreSlogan is not null) config.StoreSlogan = req.StoreSlogan == "" ? null : req.StoreSlogan;
        if (req.Announcements is not null)
            config.AnnouncementsJson = JsonSerializer.Serialize(req.Announcements);
        if (req.BgColor        is not null) config.BgColor        = req.BgColor;
        if (req.Surface2Color  is not null) config.Surface2Color  = req.Surface2Color;
        if (req.BorderColor    is not null) config.BorderColor    = req.BorderColor;
        if (req.TextColor      is not null) config.TextColor      = req.TextColor;
        if (req.TextMutedColor is not null) config.TextMutedColor = req.TextMutedColor;
        if (req.SecondaryColor is not null) config.SecondaryColor = req.SecondaryColor;
        if (req.AccentColor    is not null) config.AccentColor    = req.AccentColor;
        if (req.CatalogStyle   is not null) config.CatalogStyle   = req.CatalogStyle;

        config.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(
            HttpContext,
            action: "storefront.branding.update",
            targetType: "storefront",
            targetId: config.Id.ToString(),
            companyId: CompanyId,
            targetName: config.StoreName,
            payload: new
            {
                hasLogo = !string.IsNullOrWhiteSpace(config.LogoUrl),
                hasStoreName = !string.IsNullOrWhiteSpace(config.StoreName),
                config.PrimaryColor,
                config.SecondaryColor,
                config.AccentColor,
                config.CatalogStyle
            },
            ct: ct);
        return Ok(ToResponse(config));
    }

    // ── POST /admin/storefront/slides ──────────────────────────────────────────
    [HttpPost("slides")]
    public async Task<IActionResult> AddSlide([FromBody] UpsertBannerSlideRequest req, CancellationToken ct)
    {
        var config = await GetOrCreateConfig(ct);

        var maxOrder = config.BannerSlides.Any()
            ? config.BannerSlides.Max(s => s.SortOrder)
            : -1;

        var slide = new BannerSlide
        {
            StoreFrontConfigId = config.Id,
            ImageUrl           = req.ImageUrl,
            Title              = req.Title,
            Subtitle           = req.Subtitle,
            CtaText            = req.CtaText,
            CtaType            = NormalizeCtaType(req.CtaType),
            CtaTarget          = req.CtaTarget,
            CtaNewTab          = req.CtaNewTab ?? false,
            SortOrder          = req.SortOrder ?? maxOrder + 1,
            IsActive           = req.IsActive ?? true,
        };

        _db.BannerSlides.Add(slide);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(
            HttpContext,
            action: "storefront.slide.create",
            targetType: "storefront_slide",
            targetId: slide.Id.ToString(),
            companyId: CompanyId,
            targetName: slide.Title,
            payload: new { slide.IsActive, slide.SortOrder, hasImage = !string.IsNullOrWhiteSpace(slide.ImageUrl) },
            ct: ct);
        return Ok(ToSlideResponse(slide));
    }

    // ── PUT /admin/storefront/slides/{id} ─────────────────────────────────────
    [HttpPut("slides/{id:guid}")]
    public async Task<IActionResult> UpdateSlide(Guid id, [FromBody] UpsertBannerSlideRequest req, CancellationToken ct)
    {
        var config = await GetOrCreateConfig(ct);
        var slide  = config.BannerSlides.FirstOrDefault(s => s.Id == id);
        if (slide is null) return NotFound();

        if (req.ImageUrl  is not null) slide.ImageUrl  = req.ImageUrl;
        if (req.Title     is not null) slide.Title     = req.Title;
        if (req.Subtitle  is not null) slide.Subtitle  = req.Subtitle;
        if (req.CtaText   is not null) slide.CtaText   = req.CtaText;
        if (req.CtaType   is not null) slide.CtaType   = NormalizeCtaType(req.CtaType);
        if (req.CtaTarget is not null) slide.CtaTarget = req.CtaTarget;
        if (req.CtaNewTab.HasValue)    slide.CtaNewTab = req.CtaNewTab.Value;
        if (req.SortOrder.HasValue)    slide.SortOrder = req.SortOrder.Value;
        if (req.IsActive.HasValue)     slide.IsActive  = req.IsActive.Value;

        slide.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(
            HttpContext,
            action: "storefront.slide.update",
            targetType: "storefront_slide",
            targetId: slide.Id.ToString(),
            companyId: CompanyId,
            targetName: slide.Title,
            payload: new { slide.IsActive, slide.SortOrder, hasImage = !string.IsNullOrWhiteSpace(slide.ImageUrl) },
            ct: ct);
        return Ok(ToSlideResponse(slide));
    }

    // ── DELETE /admin/storefront/slides/{id} ──────────────────────────────────
    [HttpDelete("slides/{id:guid}")]
    public async Task<IActionResult> DeleteSlide(Guid id, CancellationToken ct)
    {
        var config = await GetOrCreateConfig(ct);
        var slide  = config.BannerSlides.FirstOrDefault(s => s.Id == id);
        if (slide is null) return NotFound();

        _db.BannerSlides.Remove(slide);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(
            HttpContext,
            action: "storefront.slide.delete",
            targetType: "storefront_slide",
            targetId: slide.Id.ToString(),
            companyId: CompanyId,
            targetName: slide.Title,
            payload: new { slide.SortOrder },
            ct: ct);
        return NoContent();
    }

    // ── POST /admin/storefront/slides/reorder ─────────────────────────────────
    [HttpPost("slides/reorder")]
    public async Task<IActionResult> ReorderSlides([FromBody] ReorderSlidesRequest req, CancellationToken ct)
    {
        var config = await GetOrCreateConfig(ct);

        for (var i = 0; i < req.OrderedIds.Count; i++)
        {
            var slide = config.BannerSlides.FirstOrDefault(s => s.Id == req.OrderedIds[i]);
            if (slide is not null)
                slide.SortOrder = i;
        }

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(
            HttpContext,
            action: "storefront.slide.reorder",
            targetType: "storefront",
            targetId: config.Id.ToString(),
            companyId: CompanyId,
            targetName: config.StoreName,
            payload: new { count = req.OrderedIds.Count },
            ct: ct);
        return Ok(ToResponse(config));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<StoreFrontConfig> GetOrCreateConfig(CancellationToken ct)
    {
        var config = await _db.StoreFrontConfigs
            .Include(c => c.BannerSlides)
            .FirstOrDefaultAsync(c => c.CompanyId == CompanyId, ct);

        if (config is null)
        {
            config = new StoreFrontConfig { CompanyId = CompanyId };
            _db.StoreFrontConfigs.Add(config);
            await _db.SaveChangesAsync(ct);
        }

        return config;
    }

    private static string NormalizeCtaType(string? raw) =>
        raw?.ToLowerInvariant() switch
        {
            "category" => "category",
            "product"  => "product",
            "external" => "external",
            _          => "none"
        };

    private static StoreFrontBrandingHealthResponse BuildBrandingHealth(
        string companySlug,
        string companyName,
        StoreFrontConfig config)
    {
        var items = new List<BrandingHealthItem>();

        AddPresence(items, "logo", "Logo", config.LogoUrl, "Envie uma logo da empresa para o header e catalogo.");
        AddPresence(items, "store_name", "Nome da loja", config.StoreName, "Defina o nome publico da loja.");
        AddColor(items, "primary_color", "Cor primaria", config.PrimaryColor, "#6366f1");
        AddColor(items, "secondary_color", "Cor secundaria", config.SecondaryColor, "#6366f1");
        AddColor(items, "accent_color", "Cor de destaque", config.AccentColor, "#f59e0b");
        AddColor(items, "background_color", "Fundo light", config.BgColor, "#ffffff");
        AddColor(items, "surface_color", "Superficie light", config.Surface2Color, "#f3f4f6");
        AddColor(items, "text_color", "Texto light", config.TextColor, "#111827");
        AddColor(items, "text_muted_color", "Texto secundario", config.TextMutedColor, "#6b7280");

        var activeSlides = config.BannerSlides.Count(s => s.IsActive);
        items.Add(activeSlides > 0
            ? Ok("slides", "Banners", $"{activeSlides} banner(s) ativo(s).")
            : Warn("slides", "Banners", "Nenhum banner ativo configurado.", "Adicione pelo menos um banner para lojas online com vitrine."));

        var announcements = ParseAnnouncements(config.AnnouncementsJson);
        var usesDefaultAnnouncement = announcements.Count == 1 && IsDefaultAnnouncement(announcements[0]);
        items.Add(!usesDefaultAnnouncement
            ? Ok("announcements", "Avisos", $"{announcements.Count} aviso(s) configurado(s).")
            : Info("announcements", "Avisos", "Usando aviso padrao.", "Configure avisos alinhados a campanha do tenant."));

        items.Add(string.Equals(config.CatalogStyle, "default", StringComparison.OrdinalIgnoreCase)
            ? Info("catalog_style", "Estilo do catalogo", "Usando estilo padrao.", "Use um estilo especifico quando a marca exigir composicao propria.")
            : Ok("catalog_style", "Estilo do catalogo", $"Estilo '{config.CatalogStyle}' configurado."));

        var score = CalculateScore(items);
        var coverage = BuildWhiteLabelCoverage();

        return new StoreFrontBrandingHealthResponse(
            config.CompanyId,
            companySlug,
            companyName,
            score,
            score >= 80 && items.All(i => i.Severity != "critical"),
            items,
            coverage);
    }

    private static IReadOnlyList<WhiteLabelCoverageItem> BuildWhiteLabelCoverage() =>
        new List<WhiteLabelCoverageItem>
        {
            new("admin_shell", "Painel administrativo", "partial", "StoreFrontConfig", "Continuar removendo cores hardcoded dos modulos internos."),
            new("storefront", "Loja online/catalogo", "covered", "StoreFrontConfig", "Manter componentes lendo variaveis de marca."),
            new("favicon", "Favicon/app icons", "not_configured", "static", "Adicionar campos de favicon e icones por tenant."),
            new("documents", "PDFs, recibos e impressao", "not_configured", "hardcoded", "Criar resolver de branding para documentos e impressao."),
            new("whatsapp", "WhatsApp", "partial", "CompanyIntegrationWhatsapp", "Adicionar nome/logo/textos por tenant nos templates."),
            new("email", "E-mails", "not_configured", "platform", "Criar remetente, assinatura e cores por tenant."),
            new("domain", "Dominio proprio", "not_configured", "tenant host", "Adicionar configuracao de dominio customizado por company.")
        };

    private static void AddPresence(
        List<BrandingHealthItem> items,
        string key,
        string label,
        string? value,
        string recommendation)
    {
        items.Add(!string.IsNullOrWhiteSpace(value)
            ? Ok(key, label, "Configurado.")
            : Critical(key, label, "Nao configurado.", recommendation));
    }

    private static void AddColor(
        List<BrandingHealthItem> items,
        string key,
        string label,
        string? value,
        string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            items.Add(Critical(key, label, "Cor ausente.", "Defina uma cor valida para evitar fallback visual."));
            return;
        }

        if (!IsValidCssColorToken(value))
        {
            items.Add(Critical(key, label, $"Valor invalido: {value}", "Use hexadecimal (#RRGGBB) ou rgba(...)."));
            return;
        }

        items.Add(string.Equals(value, defaultValue, StringComparison.OrdinalIgnoreCase)
            ? Info(key, label, $"Usando fallback {defaultValue}.", "Troque pelo valor real da identidade do tenant.")
            : Ok(key, label, $"Configurado como {value}."));
    }

    private static bool IsValidCssColorToken(string value)
    {
        var trimmed = value.Trim();
        return Regex.IsMatch(trimmed, "^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")
            || Regex.IsMatch(trimmed, "^rgba?\\([^\\)]{5,80}\\)$", RegexOptions.IgnoreCase);
    }

    private static int CalculateScore(IReadOnlyList<BrandingHealthItem> items)
    {
        if (items.Count == 0) return 0;
        var points = items.Sum(i => i.Severity switch
        {
            "ok" => 100,
            "info" => 70,
            "warning" => 45,
            _ => 0
        });
        return (int)Math.Round(points / (double)items.Count);
    }

    private static bool IsDefaultAnnouncement(string value)
    {
        var normalized = value
            .Trim()
            .ToLowerInvariant()
            .Replace("á", "a")
            .Replace("ã", "a")
            .Replace("â", "a")
            .Replace("à", "a")
            .Replace("é", "e")
            .Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o")
            .Replace("õ", "o")
            .Replace("ô", "o")
            .Replace("ú", "u")
            .Replace("ç", "c");

        return normalized.Contains("frete")
            && normalized.Contains("gratis")
            && normalized.Contains("100");
    }

    private static BrandingHealthItem Ok(string key, string label, string message) =>
        new(key, label, "ok", "ok", message);

    private static BrandingHealthItem Info(string key, string label, string message, string? recommendation = null) =>
        new(key, label, "attention", "info", message, recommendation);

    private static BrandingHealthItem Warn(string key, string label, string message, string? recommendation = null) =>
        new(key, label, "attention", "warning", message, recommendation);

    private static BrandingHealthItem Critical(string key, string label, string message, string? recommendation = null) =>
        new(key, label, "missing", "critical", message, recommendation);

    private static BannerSlideResponse ToSlideResponse(BannerSlide s) => new(
        s.Id, s.ImageUrl, s.Title, s.Subtitle, s.CtaText,
        s.CtaType, s.CtaTarget, s.CtaNewTab, s.SortOrder, s.IsActive);

    private static IReadOnlyList<string> ParseAnnouncements(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? ["Frete Grátis acima de R$ 100"]; }
        catch { return ["Frete Grátis acima de R$ 100"]; }
    }

    private static StoreFrontConfigResponse ToResponse(StoreFrontConfig c) => new(
        c.Id,
        c.PrimaryColor,
        c.BannerIntervalSecs,
        c.LogoUrl,
        c.StoreName,
        c.StoreSlogan,
        ParseAnnouncements(c.AnnouncementsJson),
        c.BannerSlides
            .OrderBy(s => s.SortOrder)
            .Select(ToSlideResponse)
            .ToList(),
        c.CompanyId,
        c.BgColor,
        c.Surface2Color,
        c.BorderColor,
        c.TextColor,
        c.TextMutedColor,
        c.SecondaryColor,
        c.AccentColor,
        c.CatalogStyle);
}

// ═══════════════════════════════════════════════════════════════════════════
// PÚBLICO — catálogo lê configuração (sem auth)
// ═══════════════════════════════════════════════════════════════════════════

[ApiController]
public class StoreFrontPublicController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TenantResolverService _tenantResolver;

    public StoreFrontPublicController(AppDbContext db, TenantResolverService tenantResolver)
    {
        _db             = db;
        _tenantResolver = tenantResolver;
    }

    // GET /catalog/{slug}/storefront  — via slug na URL
    [HttpGet("catalog/{companySlug}/storefront")]
    public async Task<IActionResult> GetBySlug([FromRoute] string companySlug, CancellationToken ct)
        => await GetCore(companySlug, ct);

    // GET /catalog/storefront         — via subdomínio
    [HttpGet("catalog/storefront")]
    public async Task<IActionResult> GetByHost(CancellationToken ct)
    {
        var slug = _tenantResolver.ExtractSlug(Request.Host.Host);
        if (slug is null)
            return BadRequest(new { error = "Tenant não identificado." });
        return await GetCore(slug, ct);
    }

    private async Task<IActionResult> GetCore(string companySlug, CancellationToken ct)
    {
        var company = await _db.Companies
            .FirstOrDefaultAsync(c => c.Slug == companySlug && c.IsActive && !c.IsDeleted, ct);
        if (company is null) return NotFound();

        var config = await _db.StoreFrontConfigs
            .Include(c => c.BannerSlides)
            .FirstOrDefaultAsync(c => c.CompanyId == company.Id, ct);

        // Empresa ainda sem configuração → defaults neutros
        if (config is null)
            return Ok(new StoreFrontConfigResponse(
                Guid.Empty, "#6366f1", 5,
                null, null, null,
                ["Bem-vindo à nossa loja!"],
                Array.Empty<BannerSlideResponse>(),
                company.Id));

        var slides = config.BannerSlides
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .Select(s => new BannerSlideResponse(
                s.Id, s.ImageUrl, s.Title, s.Subtitle, s.CtaText,
                s.CtaType, s.CtaTarget, s.CtaNewTab, s.SortOrder, s.IsActive))
            .ToList();

        IReadOnlyList<string> announcements;
        try { announcements = JsonSerializer.Deserialize<List<string>>(config.AnnouncementsJson) ?? ["Frete Grátis acima de R$ 100"]; }
        catch { announcements = ["Frete Grátis acima de R$ 100"]; }

        return Ok(new StoreFrontConfigResponse(
            config.Id, config.PrimaryColor, config.BannerIntervalSecs,
            config.LogoUrl, config.StoreName, config.StoreSlogan,
            announcements,
            slides,
            company.Id,
            config.BgColor,
            config.Surface2Color,
            config.BorderColor,
            config.TextColor,
            config.TextMutedColor,
            config.SecondaryColor,
            config.AccentColor,
            config.CatalogStyle));
    }
}
