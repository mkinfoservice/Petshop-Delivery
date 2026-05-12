namespace Petshop.Api.Messaging.Contracts;

/// <summary>
/// Publicado quando o status de um pedido muda e deve disparar notificação WhatsApp ao cliente.
/// TriggerStatus é string (não enum) para evitar acoplamento de serialização entre producer/consumer.
/// O consumer repassa para WhatsAppNotificationService que possui toda a lógica de negócio,
/// idempotência via WhatsAppMessageLog e resolução de templates.
/// </summary>
public record WhatsAppNotificationRequestedEvent
{
    public Guid     OrderId       { get; init; }
    public Guid     CompanyId     { get; init; }
    public string   PublicId      { get; init; } = "";
    public string   TriggerStatus { get; init; } = ""; // ex: "RECEBIDO", "ENTREGUE"
    public string?  CorrelationId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
