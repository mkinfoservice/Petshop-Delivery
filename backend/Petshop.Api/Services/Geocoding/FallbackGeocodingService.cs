namespace Petshop.Api.Services.Geocoding;

/// <summary>
/// Serviço de geocoding com fallback automático:
/// 1. ORS (gratuito, preciso)
/// 2. Nominatim/OSM (gratuito, open-source)
/// 3. Google Maps (crédito gratuito $200/mês — só acionado se os dois anteriores falharem)
/// </summary>
public class FallbackGeocodingService : IGeocodingService
{
    private readonly OrsGeocodingService _ors;
    private readonly NominatimGeocodingService _nominatim;
    private readonly GoogleGeocodingService _google;
    private readonly ILogger<FallbackGeocodingService> _logger;

    public FallbackGeocodingService(
        OrsGeocodingService ors,
        NominatimGeocodingService nominatim,
        GoogleGeocodingService google,
        ILogger<FallbackGeocodingService> logger)
    {
        _ors = ors;
        _nominatim = nominatim;
        _google = google;
        _logger = logger;
    }

    public async Task<(double lat, double lon)?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            _logger.LogDebug("📍 FallbackGeocoding: endereço vazio, retornando null");
            return null;
        }

        _logger.LogInformation("📍 FallbackGeocoding: iniciando para '{Address}'", address);

        // ========================================
        // TENTATIVA 1: ORS
        // ========================================
        try
        {
            _logger.LogDebug("🌍 Tentativa 1/3: chamando ORS...");
            var result = await _ors.GeocodeAsync(address, ct);
            if (result.HasValue)
            {
                _logger.LogInformation("✅ ORS encontrou: Lat={Lat:F6}, Lon={Lon:F6} para '{Address}'",
                    result.Value.lat, result.Value.lon, address);
                return result;
            }
            _logger.LogWarning("⚠️ ORS não encontrou para '{Address}', tentando Nominatim...", address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao chamar ORS para '{Address}'", address);
        }

        // ========================================
        // TENTATIVA 2: Nominatim (OSM)
        // ========================================
        try
        {
            _logger.LogDebug("🌍 Tentativa 2/3: chamando Nominatim (OSM)...");
            var result = await _nominatim.GeocodeAsync(address, ct);
            if (result.HasValue)
            {
                _logger.LogInformation("✅ Nominatim encontrou: Lat={Lat:F6}, Lon={Lon:F6} para '{Address}'",
                    result.Value.lat, result.Value.lon, address);
                return result;
            }
            _logger.LogWarning("⚠️ Nominatim não encontrou para '{Address}', tentando Google...", address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao chamar Nominatim para '{Address}'", address);
        }

        // ========================================
        // TENTATIVA 3: Google Maps Geocoding API
        // ========================================
        try
        {
            _logger.LogDebug("🌍 Tentativa 3/3: chamando Google Maps Geocoding...");
            var result = await _google.GeocodeAsync(address, ct);
            if (result.HasValue)
            {
                _logger.LogInformation("✅ Google encontrou: Lat={Lat:F6}, Lon={Lon:F6} para '{Address}'",
                    result.Value.lat, result.Value.lon, address);
                return result;
            }
            _logger.LogWarning("⚠️ Google também não encontrou para '{Address}'", address);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Erro ao chamar Google para '{Address}'", address);
        }

        // ========================================
        // TODOS FALHARAM
        // ========================================
        _logger.LogError(
            "🔥 FallbackGeocoding: todos os provedores falharam para '{Address}'.",
            address);
        return null;
    }
}
