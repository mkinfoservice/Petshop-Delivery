using System.Text.Json;

namespace Petshop.Api.Services.Geocoding;

public class OrsGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OrsGeocodingService> _logger;

    public OrsGeocodingService(HttpClient http, IConfiguration config, ILogger<OrsGeocodingService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
        // Timeout configurado no Program.cs via AddHttpClient
    }

    public async Task<(double lat, double lon)?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        var key = _config["Geocoding:Ors:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;

        // ✅ CRÍTICO: adicionar boundary.country=BR e foco no Rio de Janeiro
        var url =
            $"https://api.openrouteservice.org/geocode/search" +
            $"?api_key={Uri.EscapeDataString(key)}" +
            $"&text={Uri.EscapeDataString(address)}" +
            $"&boundary.country=BR" +
            $"&focus.point.lat=-22.9" +
            $"&focus.point.lon=-43.2" +
            $"&size=5"; // Top 5 para validar

        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("ORS retornou status {StatusCode} para: {Address}", resp.StatusCode, address);
            return null;
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("features", out var features)) return null;
        if (features.ValueKind != JsonValueKind.Array || features.GetArrayLength() == 0) return null;

        // Procura o primeiro resultado dentro do território brasileiro
        // Bounds do Brasil: lat -35 a 5.5, lon -74 a -28.8
        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("geometry", out var geom)) continue;
            if (!geom.TryGetProperty("coordinates", out var coords)) continue;
            if (coords.ValueKind != JsonValueKind.Array || coords.GetArrayLength() < 2) continue;

            var lon = coords[0].GetDouble();
            var lat = coords[1].GetDouble();

            if (!double.IsFinite(lat) || !double.IsFinite(lon)) continue;

            if (lat >= -35.0 && lat <= 5.5 && lon >= -74.0 && lon <= -28.8)
            {
                _logger.LogInformation("ORS: coords válidas BR - Lat={Lat}, Lon={Lon} - {Address}", lat, lon, address);
                return (lat, lon);
            }
            else
            {
                _logger.LogDebug("ORS: coords fora do Brasil - Lat={Lat}, Lon={Lon} - {Address}", lat, lon, address);
            }
        }

        _logger.LogWarning("ORS: nenhuma coord válida no Brasil para: {Address}", address);
        return null;
    }
}
