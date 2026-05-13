using MassTransit;
using Petshop.Api.Data;
using Petshop.Api.Messaging.Consumers;

namespace Petshop.Api.Messaging.Configuration;

public static class MessagingSetup
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        if (!options.Enabled)
        {
            // RabbitMQ desabilitado: bus em memória com consumers registrados.
            // Eventos publicados são processados no mesmo processo — mesma semântica
            // assíncrona, mas sem infraestrutura RabbitMQ.
            services.AddMassTransit(x =>
            {
                RegisterConsumers(x);
                x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            });

            return services;
        }

        services.AddMassTransit(x =>
        {
            RegisterConsumers(x);

            // Outbox Pattern: eventos são gravados na tabela OutboxMessage dentro da mesma
            // transação do SaveChanges. Um poller em background lê e publica no broker.
            // Garante zero perda de eventos mesmo se o processo cair após o commit.
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.UsingRabbitMq((ctx, cfg) =>
            {
                // Prioridade: RabbitMq__Uri (URL completa) > campos individuais
                // CloudAMQP fornece a URL completa no painel: amqps://user:pass@host/vhost
                if (!string.IsNullOrWhiteSpace(options.Uri)
                    && System.Uri.TryCreate(options.Uri, UriKind.Absolute, out var amqpUri))
                {
                    cfg.Host(amqpUri);
                }
                else
                {
                    var scheme = options.UseSsl ? "amqps" : "amqp";
                    var vhost  = options.VirtualHost.TrimStart('/');
                    var uri    = new Uri($"{scheme}://{options.Host}:{options.Port}/{System.Uri.EscapeDataString(vhost)}");

                    cfg.Host(uri, h =>
                    {
                        h.Username(options.Username);
                        h.Password(options.Password);

                        if (options.UseSsl)
                            h.UseSsl(ssl => ssl.Protocol = System.Security.Authentication.SslProtocols.Tls12);
                    });
                }

                cfg.ConfigureEndpoints(ctx, new DefaultEndpointNameFormatter(options.QueuePrefix + "-", false));
            });
        });

        return services;
    }

    /// <summary>
    /// Centraliza o registro de todos os consumers.
    /// Adicionar novos consumers aqui nas próximas etapas.
    /// </summary>
    private static void RegisterConsumers(IBusRegistrationConfigurator x)
    {
        // Etapa 3 — Geocoding assíncrono
        x.AddConsumer<GeocodingRequestedConsumer, GeocodingRequestedConsumerDefinition>();

        // Etapa 4 — Notificações WhatsApp (status de pedidos delivery)
        x.AddConsumer<WhatsAppNotificationConsumer, WhatsAppNotificationConsumerDefinition>();

        // Etapa 5 — OrderDeliveredEvent: DAV automático + fidelidade
        x.AddConsumer<DavCreationConsumer, DavCreationConsumerDefinition>();
        x.AddConsumer<LoyaltyEarnConsumer, LoyaltyEarnConsumerDefinition>();

        // Etapa 8 — PDV/fiscal WhatsApp: comprovante NFC-e + fidelidade PDV
        x.AddConsumer<PdvWhatsAppNotificationConsumer, PdvWhatsAppNotificationConsumerDefinition>();
    }
}
