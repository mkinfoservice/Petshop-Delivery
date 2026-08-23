using Petshop.Api.Models;

namespace Petshop.Api.Entities.Marketplace;

/// <summary>
/// Vínculo persistido Product ↔ item externo do marketplace, com estado de
/// sincronização. Criada/atualizada para todo produto que entra no escopo
/// (resolvido via MarketplaceIntegration.CatalogSyncMode) — auditável,
/// diferente do casamento por InternalCode/Barcode em tempo de execução que
/// o sync do iFood usa hoje.
/// </summary>
public class MarketplaceProductMapping
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MarketplaceIntegrationId { get; set; }
    public MarketplaceIntegration? Integration { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>ID do item no marketplace externo (ex: MLB123456789). Nulo até a primeira publicação.</summary>
    public string? ExternalItemId { get; set; }

    public MarketplaceSyncStatus Status { get; set; } = MarketplaceSyncStatus.Pending;

    public DateTime? LastSyncedAtUtc { get; set; }

    /// <summary>Último erro de sync para este produto (diagnóstico no painel admin).</summary>
    public string? LastErrorMessage { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
