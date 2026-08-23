using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Marketplace;
using Petshop.Api.Models;

namespace Petshop.Api.Services.Marketplace.MercadoLivre;

/// <summary>
/// Publica/atualiza produtos do vendApps como anúncios no Mercado Livre,
/// respeitando o escopo escolhido em MarketplaceIntegration.CatalogSyncMode
/// (nunca sincroniza o catálogo inteiro sem essa escolha ser explícita).
///
/// ATENÇÃO — partes que precisam de validação contra conta real antes do
/// piloto (Fase 4), sinalizadas inline: listing_type_id fixo em
/// "gold_special", sem endpoint de descrição (POST /items/{id}/description),
/// GTIN tratado como falha visível (não bloqueia o resto do lote) quando a
/// categoria exige e o produto não tem.
/// </summary>
public class MercadoLivreCatalogSyncService
{
    private const string ApiBaseUrl = "https://api.mercadolibre.com";
    private const string SiteId = "MLB";

    private readonly AppDbContext _db;
    private readonly MercadoLivreAuthService _auth;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<MercadoLivreCatalogSyncService> _logger;

    public MercadoLivreCatalogSyncService(
        AppDbContext db,
        MercadoLivreAuthService auth,
        IHttpClientFactory http,
        ILogger<MercadoLivreCatalogSyncService> logger)
    {
        _db = db;
        _auth = auth;
        _http = http;
        _logger = logger;
    }

    // ── Resolução de escopo ───────────────────────────────────────────────────

    public async Task<List<Guid>> ResolveInScopeProductIdsAsync(MarketplaceIntegration integration, CancellationToken ct)
    {
        switch (integration.CatalogSyncMode)
        {
            case MarketplaceCatalogSyncMode.AllProducts:
                return await _db.Products
                    .Where(p => p.CompanyId == integration.CompanyId && p.IsActive && !p.IsSupply)
                    .Select(p => p.Id)
                    .ToListAsync(ct);

            case MarketplaceCatalogSyncMode.SelectedCategories:
                var categoryIds = await _db.MarketplaceCategorySyncs
                    .Where(c => c.MarketplaceIntegrationId == integration.Id)
                    .Select(c => c.CategoryId)
                    .ToListAsync(ct);
                if (categoryIds.Count == 0) return new List<Guid>();
                return await _db.Products
                    .Where(p => p.CompanyId == integration.CompanyId && p.IsActive && !p.IsSupply
                             && categoryIds.Contains(p.CategoryId))
                    .Select(p => p.Id)
                    .ToListAsync(ct);

            case MarketplaceCatalogSyncMode.SelectedProducts:
                return await _db.MarketplaceProductSelections
                    .Where(s => s.MarketplaceIntegrationId == integration.Id)
                    .Select(s => s.ProductId)
                    .ToListAsync(ct);

            case MarketplaceCatalogSyncMode.NotConfigured:
            default:
                return new List<Guid>();
        }
    }

    // ── Sync principal ────────────────────────────────────────────────────────

    public async Task<MercadoLivreCatalogSyncResult> SyncAsync(MarketplaceIntegration integration, CancellationToken ct)
    {
        var result = new MercadoLivreCatalogSyncResult();

        if (integration.CatalogSyncMode == MarketplaceCatalogSyncMode.NotConfigured)
        {
            result.ErrorMessage = "Escopo de sincronização não configurado para esta integração.";
            return result;
        }

        var productIds = await ResolveInScopeProductIdsAsync(integration, ct);
        if (productIds.Count == 0)
            return result;

        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        string token;
        try
        {
            token = await _auth.GetValidAccessTokenAsync(integration, _db, ct);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Falha ao obter token: {ex.Message}";
            return result;
        }

        foreach (var product in products)
        {
            var mapping = await _db.MarketplaceProductMappings
                .FirstOrDefaultAsync(m => m.MarketplaceIntegrationId == integration.Id && m.ProductId == product.Id, ct);

            if (mapping is null)
            {
                mapping = new MarketplaceProductMapping
                {
                    MarketplaceIntegrationId = integration.Id,
                    ProductId = product.Id,
                };
                _db.MarketplaceProductMappings.Add(mapping);
            }

            try
            {
                if (string.IsNullOrEmpty(mapping.ExternalItemId))
                {
                    var itemId = await CreateItemAsync(product, token, ct);
                    mapping.ExternalItemId = itemId;
                    result.Created++;
                }
                else
                {
                    await UpdateItemAsync(mapping.ExternalItemId, product, token, ct);
                    result.Updated++;
                }

                mapping.Status = MarketplaceSyncStatus.Synced;
                mapping.LastErrorMessage = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[MercadoLivre] Falha ao sincronizar produto {ProductId}.", product.Id);
                mapping.Status = MarketplaceSyncStatus.Failed;
                mapping.LastErrorMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
                result.Failed++;
            }
            finally
            {
                mapping.LastSyncedAtUtc = DateTime.UtcNow;
                mapping.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        integration.LastCatalogSyncAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return result;
    }

    // ── Criação de anúncio ────────────────────────────────────────────────────

    private async Task<string> CreateItemAsync(Product product, string token, CancellationToken ct)
    {
        using var client = _http.CreateClient("mercadolivre");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var prediction = await PredictCategoryAsync(product.Name, client, ct)
            ?? throw new InvalidOperationException("Não foi possível prever a categoria no Mercado Livre para este produto.");

        var attributes = await GetCategoryAttributesAsync(prediction.CategoryId!, client, ct);
        var itemAttributes = new List<MercadoLivreItemAttributeValue>();

        var gtinAttr = attributes.FirstOrDefault(a => a.Id == "GTIN");
        if (gtinAttr is not null && (gtinAttr.Tags?.Required == true || gtinAttr.Tags?.CatalogRequired == true))
        {
            // vendApps ainda não tem campo de GTIN no cadastro de produto —
            // sem ele, categorias que exigem GTIN vão falhar aqui de propósito
            // (visível no LastErrorMessage) em vez de publicar incompleto.
            throw new InvalidOperationException(
                $"Categoria \"{prediction.CategoryName}\" exige GTIN e o produto não tem um cadastrado.");
        }

        var request = new MercadoLivreItemRequest
        {
            Title = product.Name.Length > 60 ? product.Name[..60] : product.Name,
            CategoryId = prediction.CategoryId!,
            Price = product.PriceCents / 100m,
            AvailableQuantity = (int)Math.Max(0, product.StockQty),
            Pictures = string.IsNullOrWhiteSpace(product.ImageUrl)
                ? null
                : new List<MercadoLivrePictureRequest> { new() { Source = product.ImageUrl! } },
            Attributes = itemAttributes.Count > 0 ? itemAttributes : null,
        };

        var response = await client.PostAsJsonAsync($"{ApiBaseUrl}/items", request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, ct);
            throw new InvalidOperationException($"Mercado Livre recusou a criação do item: {error}");
        }

        var created = await response.Content.ReadFromJsonAsync<MercadoLivreItemResponse>(cancellationToken: ct);
        return created?.Id ?? throw new InvalidOperationException("Mercado Livre não retornou o ID do item criado.");
    }

    private async Task UpdateItemAsync(string externalItemId, Product product, string token, CancellationToken ct)
    {
        using var client = _http.CreateClient("mercadolivre");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            price = product.PriceCents / 100m,
            available_quantity = (int)Math.Max(0, product.StockQty),
        };

        var request = new HttpRequestMessage(HttpMethod.Put, $"{ApiBaseUrl}/items/{externalItemId}")
        {
            Content = JsonContent.Create(payload),
        };
        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await ReadErrorAsync(response, ct);
            throw new InvalidOperationException($"Mercado Livre recusou a atualização do item {externalItemId}: {error}");
        }
    }

    // ── Categoria/atributos ───────────────────────────────────────────────────

    private static async Task<MercadoLivreCategoryPrediction?> PredictCategoryAsync(
        string title, HttpClient client, CancellationToken ct)
    {
        var url = $"{ApiBaseUrl}/sites/{SiteId}/category_predictor/predict?q={Uri.EscapeDataString(title)}&limit=1";
        var response = await client.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;

        var predictions = await response.Content
            .ReadFromJsonAsync<List<MercadoLivreCategoryPrediction>>(cancellationToken: ct);
        return predictions?.FirstOrDefault();
    }

    private static async Task<List<MercadoLivreCategoryAttribute>> GetCategoryAttributesAsync(
        string categoryId, HttpClient client, CancellationToken ct)
    {
        var response = await client.GetAsync($"{ApiBaseUrl}/categories/{categoryId}/attributes", ct);
        if (!response.IsSuccessStatusCode) return new List<MercadoLivreCategoryAttribute>();

        return await response.Content
            .ReadFromJsonAsync<List<MercadoLivreCategoryAttribute>>(cancellationToken: ct)
            ?? new List<MercadoLivreCategoryAttribute>();
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<MercadoLivreApiError>(cancellationToken: ct);
            return error?.Message ?? error?.Error ?? response.StatusCode.ToString();
        }
        catch
        {
            return response.StatusCode.ToString();
        }
    }
}

public class MercadoLivreCatalogSyncResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Failed { get; set; }
    public string? ErrorMessage { get; set; }
}
