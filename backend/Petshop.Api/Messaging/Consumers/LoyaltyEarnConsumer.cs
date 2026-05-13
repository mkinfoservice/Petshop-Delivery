using MassTransit;
using Petshop.Api.Messaging.Contracts;
using Petshop.Api.Services.Customers;

namespace Petshop.Api.Messaging.Consumers;

/// <summary>
/// Consumer fino: recebe OrderDeliveredEvent e acumula pontos de fidelidade.
/// Só executa quando CustomerId está presente no evento (cliente cadastrado).
/// Delega para LoyaltyService que possui toda a lógica de negócio e validações.
/// </summary>
public sealed class LoyaltyEarnConsumer : IConsumer<OrderDeliveredEvent>
{
    private readonly LoyaltyService _loyalty;
    private readonly ILogger<LoyaltyEarnConsumer> _logger;

    public LoyaltyEarnConsumer(
        LoyaltyService loyalty,
        ILogger<LoyaltyEarnConsumer> logger)
    {
        _loyalty = loyalty;
        _logger  = logger;
    }

    public async Task Consume(ConsumeContext<OrderDeliveredEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        // Sem cliente cadastrado → loyalty não se aplica, descarta silenciosamente
        if (msg.CustomerId is null)
        {
            _logger.LogDebug(
                "[LOYALTY_CONSUMER] SKIP | Pedido={PublicId} | Sem CustomerId — loyalty não aplicável",
                msg.PublicId);
            return;
        }

        _logger.LogInformation(
            "[LOYALTY_CONSUMER] START | Pedido={PublicId} | OrderId={OrderId} | CustomerId={CustomerId} | CorrelationId={CorrelationId}",
            msg.PublicId, msg.OrderId, msg.CustomerId, msg.CorrelationId);

        await _loyalty.EarnForOrderAsync(msg.OrderId, ct);

        _logger.LogInformation(
            "[LOYALTY_CONSUMER] DONE | Pedido={PublicId} | CustomerId={CustomerId}",
            msg.PublicId, msg.CustomerId);
    }
}

public sealed class LoyaltyEarnConsumerDefinition : ConsumerDefinition<LoyaltyEarnConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<LoyaltyEarnConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // 3 tentativas: 10s → 30s → 90s
        endpointConfigurator.UseMessageRetry(r =>
            r.Intervals(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(90)));
    }
}
