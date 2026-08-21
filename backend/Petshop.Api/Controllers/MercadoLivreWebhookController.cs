using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Petshop.Api.Data;
using Petshop.Api.Entities.Marketplace;
using Petshop.Api.Services.Marketplace.MercadoLivre;

namespace Petshop.Api.Controllers;

/// <summary>
/// Endpoint público (sem JWT) que recebe TODAS as notificações do Mercado
/// Livre — para TODOS os tenants — numa única URL fixa (diferente do iFood,
/// que tem uma URL distinta por integração). O tenant é resolvido pelo
/// `user_id` do payload, cruzado com MarketplaceIntegration.MerchantId.
///
/// URL fixada no painel developer do ML:
///   POST /api/webhooks/mercadolivre/notifications
/// </summary>
[ApiController]
[Route("api/webhooks/mercadolivre")]
public class MercadoLivreWebhookController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MercadoLivreOptions _options;
    private readonly IBackgroundJobClient _jobs;
    private readonly ILogger<MercadoLivreWebhookController> _logger;

    public MercadoLivreWebhookController(
        AppDbContext db,
        IOptions<MercadoLivreOptions> options,
        IBackgroundJobClient jobs,
        ILogger<MercadoLivreWebhookController> logger)
    {
        _db = db;
        _options = options.Value;
        _jobs = jobs;
        _logger = logger;
    }

    [HttpPost("notifications")]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        // Lê o corpo antes de qualquer await longo (ASP.NET Core pode descartar o stream).
        string rawPayload;
        using (var reader = new StreamReader(Request.Body))
            rawPayload = await reader.ReadToEndAsync(ct);

        MercadoLivreWebhookEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<MercadoLivreWebhookEvent>(rawPayload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MercadoLivre] Webhook com payload inválido.");
            return Ok(); // 200 — payload malformado não deve gerar retry do ML
        }

        if (evt is null)
            return Ok();

        if (evt.ApplicationId.ToString() != _options.AppId)
        {
            _logger.LogWarning("[MercadoLivre] Webhook com application_id inesperado: {AppId}", evt.ApplicationId);
            return Ok(); // não é nosso app — ignora sem erro
        }

        var merchantId = evt.UserId.ToString();
        var integration = await _db.MarketplaceIntegrations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Type == MarketplaceType.MercadoLivre
                                    && i.MerchantId == merchantId
                                    && i.IsActive, ct);

        if (integration is null)
        {
            _logger.LogWarning("[MercadoLivre] Nenhuma integração ativa para MerchantId={Merchant}", merchantId);
            return Ok(); // vendedor desconectou/desativou — não é erro transiente, não faz sentido reprocessar
        }

        // Responde rápido (~200) e processa em background via Hangfire —
        // retry automático embutido se a ingestão falhar (rede, marketplace fora do ar, etc.).
        _jobs.Enqueue<MercadoLivreOrderIngester>(
            s => s.ProcessWebhookAsync(integration.Id, rawPayload, CancellationToken.None));

        return Ok();
    }
}
