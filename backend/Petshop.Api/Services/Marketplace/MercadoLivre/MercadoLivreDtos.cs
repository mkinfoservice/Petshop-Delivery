using System.Text.Json.Serialization;

namespace Petshop.Api.Services.Marketplace.MercadoLivre;

/// <summary>
/// Envelope de notificacao webhook do Mercado Livre — payload leve, so avisa
/// que "algo mudou" no recurso indicado; o detalhe precisa ser buscado via
/// GET no proprio `resource`.
/// Docs: https://developers.mercadolivre.com.br/pt_br/produtos-receba-notificacoes
/// </summary>
public sealed class MercadoLivreWebhookEvent
{
    [JsonPropertyName("resource")]
    public string? Resource { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("application_id")]
    public long ApplicationId { get; set; }

    [JsonPropertyName("attempts")]
    public int Attempts { get; set; }

    [JsonPropertyName("sent")]
    public DateTime? Sent { get; set; }

    [JsonPropertyName("received")]
    public DateTime? Received { get; set; }
}

public sealed class MercadoLivreTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("user_id")]
    public long UserId { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}

/// <summary>
/// Subconjunto do payload de /orders/{id} — so os campos que o MVP consome.
/// ATENCAO: mapeamento a validar contra pedido real de sandbox antes do piloto
/// (Fase 4) — endereco/telefone do comprador em particular tem restricoes de
/// exposicao no Mercado Livre que precisam ser confirmadas na pratica.
/// </summary>
public sealed class MercadoLivreOrderPayload
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("date_created")]
    public DateTime? DateCreated { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("buyer")]
    public MercadoLivreBuyer? Buyer { get; set; }

    [JsonPropertyName("order_items")]
    public List<MercadoLivreOrderItem> OrderItems { get; set; } = new();

    [JsonPropertyName("payments")]
    public List<MercadoLivrePayment> Payments { get; set; } = new();

    [JsonPropertyName("shipping")]
    public MercadoLivreShippingRef? Shipping { get; set; }
}

public sealed class MercadoLivreBuyer
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }
}

public sealed class MercadoLivreOrderItem
{
    [JsonPropertyName("item")]
    public MercadoLivreItemRef? Item { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }
}

public sealed class MercadoLivreItemRef
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("seller_sku")]
    public string? SellerSku { get; set; }
}

public sealed class MercadoLivrePayment
{
    [JsonPropertyName("payment_method_id")]
    public string? PaymentMethodId { get; set; }

    [JsonPropertyName("transaction_amount")]
    public decimal TransactionAmount { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class MercadoLivreShippingRef
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
}

/// <summary>Resposta de GET /orders/search — usado pela reconciliação periódica.</summary>
public sealed class MercadoLivreOrderSearchResponse
{
    [JsonPropertyName("results")]
    public List<MercadoLivreOrderPayload> Results { get; set; } = new();
}
