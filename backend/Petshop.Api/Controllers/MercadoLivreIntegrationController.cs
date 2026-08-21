using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Marketplace;
using Petshop.Api.Services.Marketplace.MercadoLivre;

namespace Petshop.Api.Controllers;

/// <summary>
/// Fluxo OAuth2 authorization_code do Mercado Livre. Diferente do CRUD
/// generico de /admin/marketplace (formulario com client id/secret manual,
/// modelo do iFood): aqui o vendedor autoriza no proprio Mercado Livre, e o
/// MarketplaceIntegration e criado/atualizado automaticamente no callback.
///
/// Rotas fixadas no painel developer do ML (redirect URI exata):
///   GET  /api/integrations/mercadolivre/authorize  (autenticado, inicia o fluxo)
///   GET  /api/integrations/mercadolivre/callback   (publico, chamado pelo ML)
/// </summary>
[ApiController]
[Route("api/integrations/mercadolivre")]
public class MercadoLivreIntegrationController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MercadoLivreAuthService _auth;
    private readonly ILogger<MercadoLivreIntegrationController> _logger;

    public MercadoLivreIntegrationController(
        AppDbContext db,
        MercadoLivreAuthService auth,
        ILogger<MercadoLivreIntegrationController> logger)
    {
        _db = db;
        _auth = auth;
        _logger = logger;
    }

    private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);

    // ── GET /api/integrations/mercadolivre/authorize ──────────────────────────
    [Authorize(Roles = "admin,gerente")]
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var state = RandomNumberGenerator.GetHexString(48);

        _db.MarketplaceOAuthStates.Add(new MarketplaceOAuthState
        {
            State        = state,
            CompanyId    = CompanyId,
            Type         = MarketplaceType.MercadoLivre,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10),
        });
        await _db.SaveChangesAsync(ct);

        return Redirect(_auth.BuildAuthorizeUrl(state));
    }

    // ── GET /api/integrations/mercadolivre/callback ───────────────────────────
    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning("[MercadoLivre] Autorização recusada pelo vendedor. Error={Error}", error);
            return await RedirectToFrontendAsync(null, "ml_denied", ct);
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "code/state ausente." });

        var stateRow = await _db.MarketplaceOAuthStates
            .FirstOrDefaultAsync(s => s.State == state
                                   && s.Type == MarketplaceType.MercadoLivre
                                   && s.ConsumedAtUtc == null, ct);

        if (stateRow is null || stateRow.ExpiresAtUtc < DateTime.UtcNow)
        {
            _logger.LogWarning("[MercadoLivre] state inválido ou expirado no callback.");
            return BadRequest(new { error = "Sessão de autorização expirada — inicie a conexão novamente." });
        }

        stateRow.ConsumedAtUtc = DateTime.UtcNow;

        MercadoLivreTokenResponse token;
        try
        {
            token = await _auth.ExchangeCodeAsync(code, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoLivre] Falha ao trocar code por token.");
            await _db.SaveChangesAsync(ct); // persiste o consumo do state mesmo em erro
            return await RedirectToFrontendAsync(stateRow.CompanyId, "ml_error", ct);
        }

        var merchantId = token.UserId.ToString();

        var integration = await _db.MarketplaceIntegrations.FirstOrDefaultAsync(
            i => i.CompanyId == stateRow.CompanyId
              && i.Type == MarketplaceType.MercadoLivre
              && i.MerchantId == merchantId, ct);

        if (integration is null)
        {
            integration = new MarketplaceIntegration
            {
                CompanyId   = stateRow.CompanyId,
                Type        = MarketplaceType.MercadoLivre,
                MerchantId  = merchantId,
                DisplayName = $"Mercado Livre ({merchantId})",
                ClientId    = "", // credencial e global (MercadoLivreOptions), nao por integracao
                ClientSecretEncrypted = "",
                AutoAcceptOrders = true,
                AutoPrint = true,
                IsActive = true,
            };
            _db.MarketplaceIntegrations.Add(integration);
        }
        else
        {
            integration.IsActive = true;
        }

        await _auth.PersistTokensAsync(integration, _db, token, ct);

        _logger.LogInformation("[MercadoLivre] Integração conectada. CompanyId={Company} MerchantId={Merchant}",
            stateRow.CompanyId, merchantId);

        return await RedirectToFrontendAsync(stateRow.CompanyId, "connected", ct);
    }

    private async Task<IActionResult> RedirectToFrontendAsync(Guid? companyId, string status, CancellationToken ct)
    {
        // Redireciona o navegador do usuário de volta pro painel — subdomínio
        // resolvido pelo slug da empresa (mesmo padrão de multi-tenant do resto do app).
        string? slug = companyId is null
            ? null
            : await _db.Companies.Where(c => c.Id == companyId).Select(c => c.Slug).FirstOrDefaultAsync(ct);

        var host = string.IsNullOrWhiteSpace(slug) ? "app.vendapps.com.br" : $"{slug}.vendapps.com.br";

        return Redirect($"https://{host}/app/marketplace?ml={status}");
    }
}
