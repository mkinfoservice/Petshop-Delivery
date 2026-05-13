namespace Petshop.Api.Messaging.Contracts;

/// <summary>
/// Publicado para notificações WhatsApp originadas no fluxo PDV/fiscal.
/// TriggerStatus roteia para o método correto no consumer:
///   - "SALE_COMPLETED"         → NotifySaleCompletedAsync      (comprovante NFC-e)
///   - "PDV_LOYALTY_COMPLEMENT" → SendPdvLoyaltyComplementAsync (saldo de pontos PDV)
/// </summary>
public record PdvWhatsAppNotificationRequestedEvent
{
    public Guid     SaleId        { get; init; }
    public Guid     CompanyId     { get; init; }
    public string   TriggerStatus { get; init; } = "";
    public string?  CorrelationId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
