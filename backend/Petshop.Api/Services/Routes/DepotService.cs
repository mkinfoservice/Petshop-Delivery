using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Petshop.Api.Data;
using Petshop.Api.Entities;
using Petshop.Api.Entities.Master;

namespace Petshop.Api.Services.Routes;

public class DepotService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<DepotService> _logger;

    public DepotService(AppDbContext db, IConfiguration config, ILogger<DepotService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Obtem as coordenadas do deposito da empresa. Fallback: appsettings.json.
    /// </summary>
    public (double lat, double lon) GetDepotCoordinates(Guid? companyId = null)
    {
        var settings = GetCompanySettings(companyId);
        var lat = settings?.DepotLatitude ?? _config.GetValue<double?>("Geocoding:Depot:Latitude");
        var lon = settings?.DepotLongitude ?? _config.GetValue<double?>("Geocoding:Depot:Longitude");

        if (lat is null || lon is null)
        {
            _logger.LogWarning("Depot nao configurado para CompanyId={CompanyId} nem em appsettings.json", companyId);
            throw new InvalidOperationException("Depot nao configurado. Verifique Settings da empresa ou appsettings.json -> Geocoding:Depot");
        }

        return (lat.Value, lon.Value);
    }

    /// <summary>
    /// Obtem o endereco legivel do deposito da empresa. Fallback: appsettings.json.
    /// </summary>
    public string GetDepotAddress(Guid? companyId = null)
    {
        var settings = GetCompanySettings(companyId);
        var address = settings?.DepotAddress;

        if (string.IsNullOrWhiteSpace(address))
            address = _config.GetValue<string>("Geocoding:Depot:Address");

        return string.IsNullOrWhiteSpace(address) ? "Depot nao configurado" : address;
    }

    /// <summary>
    /// Obtem o raio de entrega da empresa. Fallback: appsettings.json e, por ultimo, 11km.
    /// </summary>
    public double GetDeliveryRadiusKm(Guid? companyId = null)
    {
        var settings = GetCompanySettings(companyId);
        if (settings?.CoverageRadiusKm is > 0)
            return settings.CoverageRadiusKm.Value;

        return _config.GetValue<double?>("Geocoding:Depot:RadiusKm") ?? 11.0;
    }

    /// <summary>
    /// Verifica se pedido esta dentro do raio de entrega a partir do deposito da empresa.
    /// </summary>
    public bool IsWithinDeliveryRadius(Order order, Guid? companyId = null, double? radiusKm = null)
    {
        if (!order.Latitude.HasValue || !order.Longitude.HasValue)
        {
            _logger.LogWarning("Pedido {OrderId} ({PublicId}) nao possui coordenadas para validar raio",
                order.Id, order.PublicId);
            return false;
        }

        var effectiveCompanyId = companyId ?? order.CompanyId;
        var radius = radiusKm ?? GetDeliveryRadiusKm(effectiveCompanyId);
        var distance = GetDistanceFromDepot(order.Latitude.Value, order.Longitude.Value, effectiveCompanyId);

        var isWithin = distance <= radius;

        if (!isWithin)
        {
            _logger.LogWarning("Pedido {OrderId} ({PublicId}) esta FORA do raio de entrega: {Distance:F2}km > {Radius:F2}km | CompanyId={CompanyId}",
                order.Id, order.PublicId, distance, radius, effectiveCompanyId);
        }
        else
        {
            _logger.LogDebug("Pedido {OrderId} ({PublicId}) esta DENTRO do raio: {Distance:F2}km <= {Radius:F2}km | CompanyId={CompanyId}",
                order.Id, order.PublicId, distance, radius, effectiveCompanyId);
        }

        return isWithin;
    }

    /// <summary>
    /// Calcula distancia em km de coordenadas ate o deposito usando formula de Haversine.
    /// </summary>
    public double GetDistanceFromDepot(double lat, double lon, Guid? companyId = null)
    {
        var depot = GetDepotCoordinates(companyId);
        return HaversineKm(depot.lat, depot.lon, lat, lon);
    }

    private CompanySettings? GetCompanySettings(Guid? companyId)
    {
        if (!companyId.HasValue) return null;

        return _db.CompanySettings
            .AsNoTracking()
            .FirstOrDefault(s => s.CompanyId == companyId.Value);
    }

    /// <summary>
    /// Formula de Haversine para calcular distancia entre dois pontos em km.
    /// </summary>
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0; // Raio da Terra em km
        static double ToRad(double deg) => deg * (Math.PI / 180.0);

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}
