using System.Text.Json.Serialization;

namespace Petshop.Api.Services.Marketplace.MercadoLivre;

public sealed class MercadoLivreCategoryPrediction
{
    [JsonPropertyName("category_id")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }
}

public sealed class MercadoLivreCategoryAttribute
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tags")]
    public MercadoLivreAttributeTags? Tags { get; set; }
}

public sealed class MercadoLivreAttributeTags
{
    [JsonPropertyName("required")]
    public bool Required { get; set; }

    [JsonPropertyName("catalog_required")]
    public bool CatalogRequired { get; set; }
}

public sealed class MercadoLivreItemAttributeValue
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("value_name")]
    public string? ValueName { get; set; }
}

public sealed class MercadoLivreItemRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; } = "";

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("currency_id")]
    public string CurrencyId { get; set; } = "BRL";

    [JsonPropertyName("available_quantity")]
    public int AvailableQuantity { get; set; }

    [JsonPropertyName("buying_mode")]
    public string BuyingMode { get; set; } = "buy_it_now";

    // ATENÇÃO: tipo de anúncio varia por vendedor/categoria/plano — "gold_special"
    // é um valor comum no MLB mas não garantido para toda conta. Validar contra
    // GET /users/{id}/available_listing_types antes do piloto real (Fase 4).
    [JsonPropertyName("listing_type_id")]
    public string ListingTypeId { get; set; } = "gold_special";

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = "new";

    [JsonPropertyName("pictures")]
    public List<MercadoLivrePictureRequest>? Pictures { get; set; }

    [JsonPropertyName("attributes")]
    public List<MercadoLivreItemAttributeValue>? Attributes { get; set; }

    [JsonPropertyName("shipping")]
    public MercadoLivreShippingRequest? Shipping { get; set; }
}

public sealed class MercadoLivrePictureRequest
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "";
}

/// <summary>
/// Retirada no local — evita depender de adesão prévia a Mercado Envios
/// (ME1/ME2) na conta do vendedor (decisão de produto, 2026-08-23).
/// </summary>
public sealed class MercadoLivreShippingRequest
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "not_specified";

    [JsonPropertyName("local_pick_up")]
    public bool LocalPickUp { get; set; } = true;
}

public sealed class MercadoLivreItemResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class MercadoLivreApiError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("cause")]
    public List<MercadoLivreApiErrorCause>? Cause { get; set; }
}

public sealed class MercadoLivreApiErrorCause
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
