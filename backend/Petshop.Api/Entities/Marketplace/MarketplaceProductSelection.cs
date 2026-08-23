using Petshop.Api.Models;

namespace Petshop.Api.Entities.Marketplace;

/// <summary>Produto incluído explicitamente na sincronização quando a integração está em modo SelectedProducts.</summary>
public class MarketplaceProductSelection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MarketplaceIntegrationId { get; set; }
    public MarketplaceIntegration? Integration { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
