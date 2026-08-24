using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Marketplace;
using Petshop.Api.Services.WhatsApp;

namespace Petshop.Api.Services.Marketplace.MercadoLivre;

/// <summary>
/// Job Hangfire recorrente: reconciliação periódica do Mercado Livre — cobre o caso
/// de webhook nunca entregue (outage do lado do ML, ou do nosso lado durante um
/// deploy/restart). Busca pedidos recentes via API (não depende do webhook) e
/// reingesta qualquer um que não esteja em MarketplaceOrders, usando o mesmo
/// pipeline de IngestAsync (idempotente — dedupe por ExternalOrderId já embutido).
///
/// ATENÇÃO: parâmetros de /orders/search (seller, order.date_created.from) ainda não
/// validados contra a API real de produção — mesma ressalva já registrada para
/// outros endpoints do Mercado Livre neste projeto (ver domain_discovery/search, que
/// substituiu category_predictor/predict, só descoberto via teste direto). Validar
/// no próximo pedido real e ajustar se o formato de resposta divergir.
/// </summary>
public class MercadoLivreReconciliationJob
{
    private const string ApiBaseUrl = "https://api.mercadolibre.com";
    private const int LookbackHours = 6; // roda a cada hora — margem de sobra pra cobrir qualquer gap

    private readonly AppDbContext _db;
    private readonly MercadoLivreAuthService _auth;
    private readonly IHttpClientFactory _http;
    private readonly MercadoLivreOrderIngester _ingester;
    private readonly WhatsAppClient _whatsApp;
    private readonly ILogger<MercadoLivreReconciliationJob> _logger;

    public MercadoLivreReconciliationJob(
        AppDbContext db,
        MercadoLivreAuthService auth,
        IHttpClientFactory http,
        MercadoLivreOrderIngester ingester,
        WhatsAppClient whatsApp,
        ILogger<MercadoLivreReconciliationJob> logger)
    {
        _db = db;
        _auth = auth;
        _http = http;
        _ingester = ingester;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var integrations = await _db.MarketplaceIntegrations
            .Where(i => i.Type == MarketplaceType.MercadoLivre && i.IsActive)
            .ToListAsync(ct);

        foreach (var integration in integrations)
        {
            try
            {
                await ReconcileOneAsync(integration, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MercadoLivre] Reconciliação falhou para integração {Id}.", integration.Id);
            }
        }
    }

    private async Task ReconcileOneAsync(MarketplaceIntegration integration, CancellationToken ct)
    {
        var token = await _auth.GetValidAccessTokenAsync(integration, _db, ct);
        using var client = _http.CreateClient("mercadolivre");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var since = DateTime.UtcNow.AddHours(-LookbackHours).ToString("yyyy-MM-ddTHH:mm:ss.000-00:00");
        var url = $"{ApiBaseUrl}/orders/search?seller={integration.MerchantId}&order.date_created.from={since}&sort=date_desc";

        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("[MercadoLivre] Reconciliação: GET {Url} retornou {Status}.", url, response.StatusCode);
            return;
        }

        var payload = await response.Content.ReadFromJsonAsync<MercadoLivreOrderSearchResponse>(cancellationToken: ct);
        if (payload?.Results is null || payload.Results.Count == 0) return;

        var recovered = 0;
        foreach (var o in payload.Results)
        {
            var externalOrderId = o.Id.ToString();
            var exists = await _db.MarketplaceOrders.AnyAsync(mo =>
                mo.MarketplaceIntegrationId == integration.Id && mo.ExternalOrderId == externalOrderId, ct);
            if (exists) continue;

            _logger.LogWarning(
                "[MercadoLivre] Reconciliação encontrou pedido não ingerido (webhook perdido?): {Id}", externalOrderId);

            // Payload sintético — reaproveita o mesmo pipeline de IngestAsync
            // (que só usa "resource" e "topic" do evento pra buscar o pedido real).
            var syntheticPayload = JsonSerializer.Serialize(new
            {
                resource = $"/orders/{externalOrderId}",
                topic = "orders_v2",
                user_id = 0,
            });

            var result = await _ingester.IngestAsync(syntheticPayload, signature: null, integration, ct);
            if (result.Success && result.InternalOrderId is not null) recovered++;
        }

        if (recovered > 0)
            await AlertRecoveredAsync(integration, recovered, ct);
    }

    private async Task AlertRecoveredAsync(MarketplaceIntegration integration, int count, CancellationToken ct)
    {
        var title = $"{count} pedido(s) do Mercado Livre recuperado(s) via reconciliação";
        var message = $"A reconciliação periódica encontrou {count} pedido(s) que não tinham chegado pelo webhook " +
                      $"e os processou agora ({integration.DisplayName}). Isso indica que a entrega de webhook do " +
                      "Mercado Livre falhou nesse intervalo — vale acompanhar se repete.";

        _db.AdminAlerts.Add(new Entities.Master.AdminAlert
        {
            CompanyId = integration.CompanyId,
            AlertType = "marketplace_orders_recovered",
            Title = title,
            Message = message,
            ReferenceId = integration.Id,
        });
        await _db.SaveChangesAsync(ct);

        try
        {
            var company = await _db.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == integration.CompanyId, ct);
            var ownerPhone = WhatsAppClient.NormalizeToE164Brazil(company?.OwnerAlertPhone);
            if (ownerPhone is not null)
                await _whatsApp.SendTextAsync(ownerPhone, $"[Alerta Mercado Livre] {title} — {message}", integration.CompanyId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[MercadoLivre] Falha ao enviar alerta de reconciliação.");
        }
    }
}
