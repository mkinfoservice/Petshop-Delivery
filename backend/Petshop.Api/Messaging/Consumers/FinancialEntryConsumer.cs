using MassTransit;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Financial;
using Petshop.Api.Messaging.Contracts;

namespace Petshop.Api.Messaging.Consumers;

/// <summary>
/// Consumer fino: recebe OrderDeliveredEvent e gera uma Receita paga no módulo
/// financeiro. Dedup por ReferenceType+ReferenceId — MassTransit garante at-least-once,
/// então essa checagem evita lançamento duplicado em caso de redelivery.
/// </summary>
public sealed class FinancialEntryConsumer : IConsumer<OrderDeliveredEvent>
{
    private const string ReferenceType = "Order";

    private readonly AppDbContext _db;
    private readonly ILogger<FinancialEntryConsumer> _logger;

    public FinancialEntryConsumer(AppDbContext db, ILogger<FinancialEntryConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderDeliveredEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var alreadyExists = await _db.FinancialEntries.AnyAsync(e =>
            e.CompanyId == msg.CompanyId &&
            e.ReferenceType == ReferenceType &&
            e.ReferenceId == msg.OrderId, ct);

        if (alreadyExists)
        {
            _logger.LogDebug("[FINANCIAL_CONSUMER] SKIP | Pedido={PublicId} — lançamento já existe.", msg.PublicId);
            return;
        }

        var today = DateOnly.FromDateTime(msg.OccurredAtUtc);
        _db.FinancialEntries.Add(new FinancialEntry
        {
            CompanyId = msg.CompanyId,
            Type = FinancialEntryType.Receita,
            Title = $"Pedido delivery {msg.PublicId}",
            AmountCents = msg.TotalCents,
            DueDate = today,
            PaidDate = today,
            IsPaid = true,
            Category = "Vendas Delivery",
            ReferenceType = ReferenceType,
            ReferenceId = msg.OrderId,
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("[FINANCIAL_CONSUMER] DONE | Pedido={PublicId} | TotalCents={TotalCents}", msg.PublicId, msg.TotalCents);
    }
}

public sealed class FinancialEntryConsumerDefinition : ConsumerDefinition<FinancialEntryConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<FinancialEntryConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
            r.Intervals(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(90)));
    }
}
