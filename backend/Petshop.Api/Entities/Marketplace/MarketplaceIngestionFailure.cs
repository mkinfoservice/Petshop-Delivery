using System.ComponentModel.DataAnnotations;

namespace Petshop.Api.Entities.Marketplace;

public enum MarketplaceFailureStatus { Pending, Resolved }

/// <summary>
/// Registro persistido de falha ao ingerir um pedido de marketplace — sobrevive
/// a reinícios/deploys, ao contrário de MarketplaceIntegration.LastErrorMessage
/// (um único slot, sobrescrito a cada tentativa). Cada falha é reaproveitável
/// (contador de tentativas) e resolvida automaticamente quando uma tentativa
/// posterior (retry automático do Hangfire, reconciliação ou reprocessamento
/// manual) tem sucesso para o mesmo ExternalOrderId.
/// </summary>
public class MarketplaceIngestionFailure
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }

    public Guid MarketplaceIntegrationId { get; set; }
    public MarketplaceIntegration? Integration { get; set; }

    /// <summary>Nulo quando a falha ocorreu antes de conseguir extrair o ID (ex: payload malformado).</summary>
    [MaxLength(100)]
    public string? ExternalOrderId { get; set; }

    [MaxLength(4000)]
    public string RawPayload { get; set; } = "";

    [MaxLength(1000)]
    public string? LastErrorMessage { get; set; }

    public int AttemptCount { get; set; } = 1;

    public MarketplaceFailureStatus Status { get; set; } = MarketplaceFailureStatus.Pending;

    public DateTime FirstFailedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastAttemptAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}
