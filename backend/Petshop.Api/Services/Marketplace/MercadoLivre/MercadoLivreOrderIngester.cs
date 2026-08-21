using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities;
using Petshop.Api.Entities.Marketplace;
using Petshop.Api.Services.Print;

namespace Petshop.Api.Services.Marketplace.MercadoLivre;

/// <summary>
/// Processa notificacao webhook do Mercado Livre (topico orders_v2):
///  1. Busca o pedido completo via GET no `resource` do payload
///  2. Normaliza para Order interno (mesmo modelo de delivery/telefone/mesa)
///  3. Cria MarketplaceOrder (vinculo + rastreamento)
///
/// O tenant ja chega resolvido em `integration` — quem faz esse trabalho e o
/// MercadoLivreWebhookController (resolve por MerchantId, nao por URL, ver
/// project_marketplace_hub_native.md para o porque dessa diferenca do iFood).
///
/// ATENCAO: mapeamento de endereco/telefone do comprador ainda nao validado
/// contra pedido real de sandbox — Fase 2 cobre esse teste antes do piloto.
/// </summary>
public class MercadoLivreOrderIngester : IMarketplaceOrderIngester
{
    public MarketplaceType Type => MarketplaceType.MercadoLivre;

    private const string ApiBaseUrl = "https://api.mercadolibre.com";

    private readonly AppDbContext _db;
    private readonly MercadoLivreAuthService _auth;
    private readonly IHttpClientFactory _http;
    private readonly PrintService _print;
    private readonly ILogger<MercadoLivreOrderIngester> _logger;

    public MercadoLivreOrderIngester(
        AppDbContext db,
        MercadoLivreAuthService auth,
        IHttpClientFactory http,
        PrintService print,
        ILogger<MercadoLivreOrderIngester> logger)
    {
        _db = db;
        _auth = auth;
        _http = http;
        _print = print;
        _logger = logger;
    }

    /// <summary>
    /// Ponto de entrada chamado pelo Hangfire (job em background, retry automático
    /// embutido). Recebe só o ID — nunca a entidade — porque o job roda numa
    /// instância de DbContext nova; passar a entidade serializada quebraria o
    /// tracking do EF Core silenciosamente.
    /// </summary>
    public async Task ProcessWebhookAsync(Guid integrationId, string rawPayload, CancellationToken ct = default)
    {
        var integration = await _db.MarketplaceIntegrations
            .FirstOrDefaultAsync(i => i.Id == integrationId && i.IsActive, ct);

        if (integration is null)
        {
            _logger.LogWarning("[MercadoLivre] IntegrationId não encontrado ou inativo: {Id}", integrationId);
            return;
        }

        var result = await IngestAsync(rawPayload, signature: null, integration, ct);

        if (!result.Success && result.ErrorMessage is not null)
        {
            integration.LastErrorMessage = result.ErrorMessage;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IngestResult> IngestAsync(
        string rawPayload,
        string? signature,
        MarketplaceIntegration integration,
        CancellationToken ct = default)
    {
        MercadoLivreWebhookEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<MercadoLivreWebhookEvent>(rawPayload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoLivre] Falha ao deserializar evento webhook.");
            return IngestResult.Fail("Payload inválido");
        }

        if (evt is null || string.IsNullOrEmpty(evt.Resource))
        {
            _logger.LogWarning("[MercadoLivre] Evento sem resource. Payload={P}", rawPayload);
            return IngestResult.Fail("resource ausente no evento");
        }

        // So processa orders_v2 — items/shipments ficam so registrados por ora (MVP).
        if (!string.Equals(evt.Topic, "orders_v2", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("[MercadoLivre] Evento ignorado (topico {Topic}).", evt.Topic);
            return IngestResult.Duplicate();
        }

        var externalOrderId = evt.Resource.TrimStart('/').Replace("orders/", "");

        var exists = await _db.MarketplaceOrders
            .AnyAsync(mo => mo.MarketplaceIntegrationId == integration.Id
                         && mo.ExternalOrderId == externalOrderId, ct);
        if (exists)
        {
            _logger.LogInformation("[MercadoLivre] Pedido duplicado ignorado. ExternalId={Id}", externalOrderId);
            return IngestResult.Duplicate();
        }

        MercadoLivreOrderPayload? payload;
        try
        {
            payload = await FetchOrderAsync(integration, evt.Resource, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MercadoLivre] Falha ao buscar pedido {Resource} na API.", evt.Resource);
            return IngestResult.Fail($"Erro ao buscar pedido: {ex.Message}");
        }

        if (payload is null)
            return IngestResult.Fail("Pedido não encontrado na API do Mercado Livre");

        var order = MapToOrder(payload, integration);
        _db.Orders.Add(order);

        var mktOrder = new MarketplaceOrder
        {
            MarketplaceIntegrationId = integration.Id,
            OrderId = order.Id,
            ExternalOrderId = externalOrderId,
            ExternalStatus = payload.Status ?? "",
            ReceivedAtUtc = DateTime.UtcNow,
            RawPayloadJson = rawPayload,
        };
        _db.MarketplaceOrders.Add(mktOrder);

        integration.LastOrderReceivedAtUtc = DateTime.UtcNow;
        integration.LastErrorMessage = null;

        await _db.SaveChangesAsync(ct);

        if (integration.AutoPrint)
        {
            try { await _print.EnqueueAsync(order, ct); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MercadoLivre] Falha na impressão automática do pedido {Id}.", order.PublicId);
            }
        }

        _logger.LogInformation("[MercadoLivre] Pedido ingested. External={Ext} Internal={Int}",
            externalOrderId, order.PublicId);

        return IngestResult.Ok(order.Id.ToString());
    }

    private async Task<MercadoLivreOrderPayload?> FetchOrderAsync(
        MarketplaceIntegration integration,
        string resourcePath,
        CancellationToken ct)
    {
        var token = await _auth.GetValidAccessTokenAsync(integration, _db, ct);
        using var client = _http.CreateClient("mercadolivre");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var url = $"{ApiBaseUrl}{resourcePath}";
        var response = await client.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("[MercadoLivre] GET {Url} retornou {Status}", url, response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MercadoLivreOrderPayload>(cancellationToken: ct);
    }

    private Order MapToOrder(MercadoLivreOrderPayload p, MarketplaceIntegration integration)
    {
        var buyerName = p.Buyer is null
            ? "Cliente Mercado Livre"
            : $"{p.Buyer.FirstName} {p.Buyer.LastName}".Trim() is { Length: > 0 } full
                ? full
                : (p.Buyer.Nickname ?? "Cliente Mercado Livre");

        var order = new Order
        {
            Id            = Guid.NewGuid(),
            CompanyId     = integration.CompanyId,
            PublicId      = OrderIdGenerator.NewPublicId(),
            CustomerName  = buyerName,
            Phone         = "", // ML nao expõe telefone do comprador via /orders — endereco/contato vem do shipment
            PaymentMethod = NormalizePaymentMethod(p.Payments),
            OriginChannel = "mercadolivre",
            SubtotalCents = ToCents(p.TotalAmount),
            DeliveryCents = 0,
            TotalCents    = ToCents(p.TotalAmount),
            Status        = OrderStatus.RECEBIDO,
            CreatedAtUtc  = p.DateCreated ?? DateTime.UtcNow,
            UpdatedAtUtc  = DateTime.UtcNow,
        };

        foreach (var item in p.OrderItems)
        {
            order.Items.Add(new OrderItem
            {
                Id                     = Guid.NewGuid(),
                OrderId                = order.Id,
                ProductNameSnapshot    = item.Item?.Title ?? "Item Mercado Livre",
                UnitPriceCentsSnapshot = ToCents(item.UnitPrice),
                Qty                    = item.Quantity,
            });
        }

        return order;
    }

    private static int ToCents(decimal value) => (int)Math.Round(value * 100);

    private static string NormalizePaymentMethod(List<MercadoLivrePayment> payments)
    {
        var method = payments.FirstOrDefault()?.PaymentMethodId;
        if (string.IsNullOrWhiteSpace(method)) return "PIX";

        return method.ToUpperInvariant() switch
        {
            "PIX"                                    => "PIX",
            "MASTERCARD" or "VISA" or "AMEX" or "ELO" => "CARTAO_CREDITO",
            "DEBIT"                                   => "CARTAO_DEBITO",
            _                                          => "PIX",
        };
    }
}
