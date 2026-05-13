using MassTransit;
using Petshop.Api.Messaging.Contracts;
using Petshop.Api.Services.WhatsApp;

namespace Petshop.Api.Messaging.Consumers;

/// <summary>
/// Consumer fino: recebe PdvWhatsAppNotificationRequestedEvent e roteia
/// pelo TriggerStatus para o método correto de WhatsAppNotificationService.
/// Idempotência garantida pelos próprios métodos do service (log de duplicatas).
/// </summary>
public sealed class PdvWhatsAppNotificationConsumer : IConsumer<PdvWhatsAppNotificationRequestedEvent>
{
    private readonly WhatsAppNotificationService           _whatsapp;
    private readonly ILogger<PdvWhatsAppNotificationConsumer> _logger;

    public PdvWhatsAppNotificationConsumer(
        WhatsAppNotificationService whatsapp,
        ILogger<PdvWhatsAppNotificationConsumer> logger)
    {
        _whatsapp = whatsapp;
        _logger   = logger;
    }

    public async Task Consume(ConsumeContext<PdvWhatsAppNotificationRequestedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        _logger.LogInformation(
            "[PDV_WA_CONSUMER] START | SaleId={SaleId} | TriggerStatus={TriggerStatus} | CorrelationId={CorrelationId}",
            msg.SaleId, msg.TriggerStatus, msg.CorrelationId);

        switch (msg.TriggerStatus)
        {
            case "SALE_COMPLETED":
                await _whatsapp.NotifySaleCompletedAsync(msg.SaleId, ct);
                break;

            case "PDV_LOYALTY_COMPLEMENT":
                await _whatsapp.SendPdvLoyaltyComplementAsync(msg.SaleId, ct);
                break;

            default:
                _logger.LogWarning(
                    "[PDV_WA_CONSUMER] SKIP | TriggerStatus desconhecido={TriggerStatus} | SaleId={SaleId}",
                    msg.TriggerStatus, msg.SaleId);
                return;
        }

        _logger.LogInformation(
            "[PDV_WA_CONSUMER] DONE | SaleId={SaleId} | TriggerStatus={TriggerStatus}",
            msg.SaleId, msg.TriggerStatus);
    }
}

public sealed class PdvWhatsAppNotificationConsumerDefinition
    : ConsumerDefinition<PdvWhatsAppNotificationConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<PdvWhatsAppNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // 3 tentativas: 30s → 2min → 5min (respeita rate limits da Meta API)
        endpointConfigurator.UseMessageRetry(r =>
            r.Intervals(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(5)));
    }
}
