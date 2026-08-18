using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Catalog;
using Petshop.Api.Services;

namespace Petshop.Api.Middleware;

/// <summary>
/// Valida que o token JWT pertence à empresa do subdomínio atual (qualquer role).
/// Impede que um token de empresa A seja usado no subdomínio de empresa B.
///
/// Resolução do tenant: header X-Tenant-Slug (enviado pelo frontend em toda
/// request autenticada — ver adminFetch.ts/delivererFetch.ts) como fonte principal.
/// O Host da request NÃO serve para isso: frontend (Vercel) e backend (Render)
/// são domínios distintos, então Request.Host chega sempre como o domínio do
/// backend, nunca como o subdomínio do tenant. Fallback por Host é mantido só
/// para o caso raro de domínio próprio apontado direto para o Render.
///
/// Execução: após UseAuthentication + UseAuthorization.
/// Paths isentos: /master, /public, /auth, /hangfire.
/// </summary>
public class TenantHostValidationMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly string[] ExemptPrefixes =
        ["/master", "/public", "/auth", "/hangfire"];

    public TenantHostValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantResolverService tenantResolver,
        AppDbContext db)
    {
        var path = context.Request.Path.Value ?? "";

        // 1. Paths isentos — pular validação
        if (ExemptPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // 2. Resolver tenant: header explícito primeiro, Host como fallback.
        Company? company = null;

        var slugHeader = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(slugHeader))
        {
            var slug = slugHeader.Trim().ToLowerInvariant();
            company = await db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Slug == slug && !c.IsDeleted, context.RequestAborted);
        }

        company ??= await tenantResolver.ResolveCompanyFromHostAsync(
            context.Request.Host.Host,
            db,
            context.RequestAborted);

        if (company is null)
        {
            await _next(context);
            return;
        }

        // 3. Só aplicar restrição se houver JWT autenticado com companyId
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        // 4. Comparar companyId do JWT com o tenant do host (qualquer role)
        var jwtCompanyId = context.User.FindFirstValue("companyId");

        if (!string.IsNullOrEmpty(jwtCompanyId) &&
            !string.Equals(jwtCompanyId, company.Id.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Token inválido para este domínio.\"}");
            return;
        }

        await _next(context);
    }
}
