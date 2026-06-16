using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Services;

namespace Petshop.Api.Middleware;

/// <summary>
/// Valida que o token JWT pertence à empresa do subdomínio atual (qualquer role).
/// Impede que um token de empresa A seja usado no subdomínio de empresa B.
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

        // 2. Resolver tenant pelo subdominio padrao ou por dominio proprio ativo.
        var company = await tenantResolver.ResolveCompanyFromHostAsync(
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
