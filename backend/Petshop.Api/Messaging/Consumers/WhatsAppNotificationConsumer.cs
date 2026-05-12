using MassTransit;
using Petshop.Api.Entities;
using Petshop.Api.Messaging.Contracts;
using Petshop.Api.Services.WhatsApp;

namespace Petshop.Api.Messaging.Consumers;

/// <summary>
/// Consumer fino: recebe WhatsAppNotificationRequestedEvent e delega para
/// WhatsAppNotificationService, que possui toda a lógica de negócio,
/// resolução de template, idempotência via WhatsAppMessageLog e logging.
/// </summary>
public sealed class WhatsAppNotificationConsumer : IConsumer<WhatsAppNotificationRequestedEvent>
{
    private readonly WhatsAppNotificationService _whatsApp;
    private readonly ILogger<WhatsAppNotificationConsumer> _logger;

    public WhatsAppNotificationConsumer(
        WhatsAppNotificationService whatsApp,
        ILogger<WhatsAppNotificationConsumer> logger)
    {
        _whatsApp = whatsApp;
        _logger   = logger;
    }

    public async Task Consume(ConsumeContext<WhatsAppNotificationRequestedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        _logger.LogInformation(
            "[WA_CONSUMER] START | Pedido={PublicId} | Status={Status} | CompanyId={CompanyId} | CorrelationId={CorrelationId}",
            msg.PublicId, msg.TriggerStatus, msg.CompanyId, msg.CorrelationId);

        if (!Enum.TryParse<OrderStatus>(msg.TriggerStatus, ignoreCase: true, out var status))
        {
            _logger.LogWarning(
                "[WA_CONSUMER] INVALID_STATUS | Pedido={PublicId} | TriggerStatus={Status} — mensagem descartada",
                msg.PublicId, msg.TriggerStatus);
            return; // status desconhecido não é retentável
        }

        // WhatsAppNotificationService já trata: WhatsApp desabilitado, template não configurado,
        // telefone inválido, idempotência (WhatsAppMessageLog), falhas de envio.
        // Exceções de infraestrutura (DB/API 5xx) propagam → MassTransit faz retry automático.
        await _whatsApp.NotifyOrderStatusAsync(msg.OrderId, status, ct);

        _logger.LogInformation(
            "[WA_CONSUMER] DONE | Pedido={PublicId} | Status={Status}",
            msg.PublicId, msg.TriggerStatus);
    }
}

/// <summary>
/// Política de retry para notificações WhatsApp.
/// Intervalos maiores que geocoding: API Meta tem rate limits por número de telefone.
/// Após 3 falhas de infraestrutura, mensagem vai para dead-letter queue.
/// </summary>
public sealed class WhatsAppNotificationConsumerDefinition
    : ConsumerDefinition<WhatsAppNotificationConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<WhatsAppNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // 3 tentativas: 30s → 2min → 5min
        // Espaçamento maior para respeitar rate limits da Meta API
        endpointConfigurator.UseMessageRetry(r =>
            r.Intervals(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMinutes(2),
                TimeSpan.FromMinutes(5)));
    }
}
