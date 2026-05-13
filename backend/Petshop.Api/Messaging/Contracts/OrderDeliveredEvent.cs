namespace Petshop.Api.Messaging.Contracts;

/// <summary>
/// Publicado quando um pedido de delivery é marcado como ENTREGUE.
/// Dispara dois side effects desacoplados via consumers independentes:
///   - DavCreationConsumer: cria SalesQuote (DAV) para confirmação fiscal
///   - LoyaltyEarnConsumer: acumula pontos de fidelidade se cliente cadastrado
///
/// Nota: a notificação WhatsApp de ENTREGUE é publicada separadamente via
/// WhatsAppNotificationRequestedEvent pelo mesmo UpdateStatus, sem dependência deste evento.
/// </summary>
public record OrderDeliveredEvent
{
    public Guid  OrderId       { get; init; }
    public Guid  CompanyId     { get; init; }
    public string PublicId     { get; init; } = "";
    public Guid? CustomerId    { get; init; } // null = sem cliente cadastrado, loyalty não roda
    public string? CorrelationId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
