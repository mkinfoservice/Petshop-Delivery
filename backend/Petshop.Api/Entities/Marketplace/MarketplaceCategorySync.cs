using Petshop.Api.Models;

namespace Petshop.Api.Entities.Marketplace;

/// <summary>Categoria incluída na sincronização quando a integração está em modo SelectedCategories.</summary>
public class MarketplaceCategorySync
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MarketplaceIntegrationId { get; set; }
    public MarketplaceIntegration? Integration { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
