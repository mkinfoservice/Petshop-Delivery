using MassTransit;
using Petshop.Api.Messaging.Contracts;
using Petshop.Api.Services.Dav.Jobs;

namespace Petshop.Api.Messaging.Consumers;

/// <summary>
/// Consumer fino: recebe OrderDeliveredEvent e cria o DAV (SalesQuote) automaticamente.
/// Delega para DeliveryOrderToDavJob que possui toda a lógica e já é idempotente
/// (não cria DAV duplicado para o mesmo pedido).
/// </summary>
public sealed class DavCreationConsumer : IConsumer<OrderDeliveredEvent>
{
    private readonly DeliveryOrderToDavJob _davJob;
    private readonly ILogger<DavCreationConsumer> _logger;

    public DavCreationConsumer(
        DeliveryOrderToDavJob davJob,
        ILogger<DavCreationConsumer> logger)
    {
        _davJob = davJob;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderDeliveredEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        _logger.LogInformation(
            "[DAV_CONSUMER] START | Pedido={PublicId} | OrderId={OrderId} | CompanyId={CompanyId} | CorrelationId={CorrelationId}",
            msg.PublicId, msg.OrderId, msg.CompanyId, msg.CorrelationId);

        await _davJob.RunAsync(msg.OrderId, ct);

        _logger.LogInformation(
            "[DAV_CONSUMER] DONE | Pedido={PublicId}",
            msg.PublicId);
    }
}

public sealed class DavCreationConsumerDefinition : ConsumerDefinition<DavCreationConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<DavCreationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // 3 tentativas: 10s → 30s → 90s
        // DAV é operação de BD — falha transitória (lock, timeout) é retentável
        endpointConfigurator.UseMessageRetry(r =>
            r.Intervals(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(90)));
    }
}
