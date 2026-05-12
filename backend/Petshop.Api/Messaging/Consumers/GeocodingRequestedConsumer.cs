using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Messaging.Contracts;
using Petshop.Api.Services.Geocoding;

namespace Petshop.Api.Messaging.Consumers;

public sealed class GeocodingRequestedConsumer : IConsumer<GeocodingRequestedEvent>
{
    private readonly AppDbContext     _db;
    private readonly IGeocodingService _geo;
    private readonly ViaCepService    _viaCep;
    private readonly IConfiguration  _config;
    private readonly ILogger<GeocodingRequestedConsumer> _logger;

    public GeocodingRequestedConsumer(
        AppDbContext db,
        IGeocodingService geo,
        ViaCepService viaCep,
        IConfiguration config,
        ILogger<GeocodingRequestedConsumer> logger)
    {
        _db     = db;
        _geo    = geo;
        _viaCep = viaCep;
        _config = config;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GeocodingRequestedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        _logger.LogInformation(
            "[GEO_CONSUMER] START | Pedido={PublicId} | OrderId={OrderId} | CompanyId={CompanyId} | CorrelationId={CorrelationId}",
            msg.PublicId, msg.OrderId, msg.CompanyId, msg.CorrelationId);

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == msg.OrderId && o.CompanyId == msg.CompanyId, ct);

        if (order is null)
        {
            _logger.LogWarning(
                "[GEO_CONSUMER] ORDER_NOT_FOUND | Pedido={PublicId} | OrderId={OrderId} — mensagem descartada",
                msg.PublicId, msg.OrderId);
            return; // não relança: pedido deletado ou CompanyId errado não é retentável
        }

        // Idempotência: coordenadas já preenchidas por reprocessamento manual ou retry anterior
        if (order.Latitude is not null && order.Longitude is not null)
        {
            _logger.LogInformation(
                "[GEO_CONSUMER] ALREADY_GEOCODED | Pedido={PublicId} | Lat={Lat:F6} | Lon={Lon:F6} — sem ação",
                msg.PublicId, order.Latitude, order.Longitude);
            return;
        }

        var providerName = (_config["Geocoding:Provider"] ?? "NOMINATIM").ToUpperInvariant();
        var hasAddress   = !string.IsNullOrWhiteSpace(order.Address);
        var hasCep       = !string.IsNullOrWhiteSpace(order.Cep);
        var cepIsValid   = hasCep && order.Cep!.Replace("-", "").Length == 8;

        _logger.LogInformation(
            "[GEO_CONSUMER] VALIDATE | Pedido={PublicId} | Provider={Provider} | HasAddress={HasAddress} | HasCep={HasCep} | CepValid={CepValid}",
            msg.PublicId, providerName, hasAddress, hasCep, cepIsValid);

        if (!hasAddress || !hasCep)
        {
            _logger.LogWarning(
                "[GEO_CONSUMER] SKIPPED | Pedido={PublicId} | Motivo: endereço ou CEP ausente | Address={Address} | Cep={Cep}",
                msg.PublicId, order.Address ?? "(null)", order.Cep ?? "(null)");

            order.GeocodedAtUtc  = DateTime.UtcNow;
            order.GeocodeProvider = $"{providerName} (incomplete_address)";
            await _db.SaveChangesAsync(ct);
            return;
        }

        if (!cepIsValid)
        {
            _logger.LogWarning(
                "[GEO_CONSUMER] SKIPPED | Pedido={PublicId} | Motivo: CEP inválido={Cep}",
                msg.PublicId, order.Cep);

            order.GeocodedAtUtc  = DateTime.UtcNow;
            order.GeocodeProvider = $"{providerName} (invalid_cep)";
            await _db.SaveChangesAsync(ct);
            return;
        }

        var queryAddress = await BuildGeocodingQueryAsync(order.PublicId, order.Address!, order.Cep!, ct);

        _logger.LogInformation(
            "[GEO_CONSUMER] CALL | Pedido={PublicId} | Provider={Provider} | Query={Query}",
            msg.PublicId, providerName, queryAddress);

        // Lança exceção em falha de rede → MassTransit faz retry automático
        var coords = await _geo.GeocodeAsync(queryAddress, ct);

        if (coords is not null)
        {
            order.Latitude       = coords.Value.lat;
            order.Longitude      = coords.Value.lon;
            order.GeocodedAtUtc  = DateTime.UtcNow;
            order.GeocodeProvider = providerName;

            _logger.LogInformation(
                "[GEO_CONSUMER] SUCCESS | Pedido={PublicId} | Lat={Lat:F6} | Lon={Lon:F6} | Provider={Provider}",
                msg.PublicId, coords.Value.lat, coords.Value.lon, providerName);
        }
        else
        {
            order.GeocodedAtUtc  = DateTime.UtcNow;
            order.GeocodeProvider = $"{providerName} (not_found)";

            _logger.LogWarning(
                "[GEO_CONSUMER] NOT_FOUND | Pedido={PublicId} | Provider={Provider} | Query={Query}",
                msg.PublicId, providerName, queryAddress);
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[GEO_CONSUMER] DONE | Pedido={PublicId} | Provider={GeocodeProvider}",
            msg.PublicId, order.GeocodeProvider);
    }

    /// <summary>
    /// Enriquece o endereço via ViaCEP antes de geocodificar.
    /// Mesma lógica do OrdersController — CEP → logradouro + bairro + cidade → query precisa.
    /// </summary>
    private async Task<string> BuildGeocodingQueryAsync(
        string publicId, string address, string cep, CancellationToken ct)
    {
        var viaCep = await _viaCep.GetAddressAsync(cep, ct);

        if (viaCep != null && !string.IsNullOrWhiteSpace(viaCep.Logradouro))
        {
            var numMatch    = Regex.Match(address, @"[Nn](?:r\.?o?)?\.?[°º]?\s*(\d+)");
            var houseNumber = numMatch.Success ? numMatch.Groups[1].Value : "";

            var parts = new List<string> { viaCep.Logradouro };
            if (!string.IsNullOrEmpty(houseNumber)) parts.Add(houseNumber);
            if (!string.IsNullOrEmpty(viaCep.Bairro)) parts.Add(viaCep.Bairro);
            parts.Add(viaCep.Localidade ?? "Rio de Janeiro");
            parts.Add(viaCep.Uf ?? "RJ");
            parts.Add("Brasil");

            var enriched = string.Join(", ", parts);

            _logger.LogInformation(
                "[GEO_CONSUMER] VIACEP_ENRICHED | Pedido={PublicId} | Original={Original} | Enriched={Enriched}",
                publicId, address, enriched);

            return enriched;
        }

        var fallback = $"{address}, {cep}, Brasil";

        _logger.LogWarning(
            "[GEO_CONSUMER] VIACEP_FALLBACK | Pedido={PublicId} | ViaCEP falhou, usando: {Query}",
            publicId, fallback);

        return fallback;
    }
}

/// <summary>
/// Define política de retry por mensagem para o GeocodingRequestedConsumer.
/// MassTransit usa esta classe automaticamente ao detectar o nome <Consumer>Definition.
/// </summary>
public sealed class GeocodingRequestedConsumerDefinition : ConsumerDefinition<GeocodingRequestedConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<GeocodingRequestedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // 3 tentativas com backoff: 5s → 20s → 60s
        // Após a 3ª falha, a mensagem vai para a dead-letter queue automaticamente
        endpointConfigurator.UseMessageRetry(r =>
            r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(60)));
    }
}
