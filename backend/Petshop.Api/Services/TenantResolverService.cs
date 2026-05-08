using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Catalog;

namespace Petshop.Api.Services;

/// <summary>
/// Extrai e valida o slug do tenant a partir do Host header.
/// Configurável via TENANT_BASE_DOMAIN (padrão: "vendapps.com.br").
/// </summary>
public partial class TenantResolverService
{
    private readonly string _baseDomain;

    private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "app", "admin", "api", "master", "suporte", "blog", "help", "status"
    };

    [GeneratedRegex(@"^[a-z0-9-]{3,63}$")]
    private static partial Regex SlugPattern();

    [GeneratedRegex(@"^(?=.{1,253}$)(?!-)(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$")]
    private static partial Regex HostnamePattern();

    public TenantResolverService(IConfiguration configuration)
    {
        _baseDomain = (configuration["TENANT_BASE_DOMAIN"] ?? "vendapps.com.br").ToLowerInvariant().Trim('.');
    }

    /// <summary>
    /// Valida um slug diretamente (sem Host header).
    /// Retorna null se válido, ou a mensagem de erro se inválido.
    /// </summary>
    public string? ValidateSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "Slug é obrigatório.";

        var s = slug.Trim().ToLowerInvariant();

        if (!SlugPattern().IsMatch(s))
            return "Slug inválido. Use apenas letras minúsculas, números e hífens (3–63 caracteres).";

        if (ReservedSlugs.Contains(s))
            return $"Slug '{s}' é reservado e não pode ser utilizado.";

        return null; // válido
    }

    public string? ValidateCustomDomain(string? hostname)
    {
        var normalized = NormalizeHost(hostname);
        if (normalized is null)
            return "Domínio é obrigatório.";

        if (!HostnamePattern().IsMatch(normalized))
            return "Domínio inválido. Informe um host completo, ex: pedidos.sualoja.com.br.";

        if (string.Equals(normalized, _baseDomain, StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("." + _baseDomain, StringComparison.OrdinalIgnoreCase))
            return $"Use o subdomínio padrão {_baseDomain} pelo slug; domínio próprio deve ser externo ao domínio vendApps.";

        return null;
    }

    /// <summary>
    /// Extrai o slug do tenant do valor do Host header.
    /// Retorna null se o host for o domínio apex, reservado, inválido ou não pertencer ao base domain.
    /// </summary>
    public string? ExtractSlug(string? host)
    {
        var h = NormalizeHost(host);
        if (h is null) return null;

        // Domínio apex → sem tenant
        if (string.Equals(h, _baseDomain, StringComparison.OrdinalIgnoreCase))
            return null;

        // Deve terminar com ".<baseDomain>"
        var suffix = "." + _baseDomain;
        if (!h.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            return null;

        // Extrai apenas o primeiro nível de subdomínio
        var subdomain = h[..^suffix.Length];

        // Não aceita sub.sub (ex: a.b.vendapps.com.br)
        if (subdomain.Contains('.'))
            return null;

        // Valida formato do slug
        if (!SlugPattern().IsMatch(subdomain))
            return null;

        // Bloqueia slugs reservados
        if (ReservedSlugs.Contains(subdomain))
            return null;

        return subdomain;
    }

    public async Task<Company?> ResolveCompanyFromHostAsync(
        string? host,
        AppDbContext db,
        CancellationToken ct)
    {
        var slug = ExtractSlug(host);
        if (slug is not null)
        {
            return await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug, ct);
        }

        var hostname = NormalizeHost(host);
        if (hostname is null)
            return null;

        return await db.CompanyCustomDomains
            .AsNoTracking()
            .Where(d => d.Hostname == hostname
                     && d.VerifiedAtUtc != null
                     && (d.Status == CompanyCustomDomainStatus.Active
                         || d.Status == CompanyCustomDomainStatus.Verified))
            .Select(d => d.Company)
            .FirstOrDefaultAsync(ct);
    }

    public string? NormalizeCustomDomain(string? hostname)
    {
        var normalized = NormalizeHost(hostname);
        return normalized is not null && ValidateCustomDomain(normalized) is null
            ? normalized
            : null;
    }

    private static string? NormalizeHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var value = host.Trim().ToLowerInvariant();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return null;
            value = uri.Host;
        }

        value = value.Split(':')[0].Trim().Trim('.');
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
