namespace Petshop.Api.Entities.Marketplace;

/// <summary>
/// Controla o que uma integração de marketplace publica do catálogo — padrão
/// para qualquer marketplace (não só Mercado Livre). Nunca sincroniza tudo
/// por padrão: uma integração nova nasce em NotConfigured (sync é no-op)
/// até o lojista escolher explicitamente um modo.
/// </summary>
public enum MarketplaceCatalogSyncMode
{
    NotConfigured = 0,
    AllProducts = 1,
    SelectedCategories = 2,
    SelectedProducts = 3,
}
