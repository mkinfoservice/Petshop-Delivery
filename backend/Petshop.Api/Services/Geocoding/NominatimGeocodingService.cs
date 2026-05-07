using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Petshop.Api.Services.Geocoding;

public interface IGeocodingService
{
    Task<(double lat, double lon)?> GeocodeAsync(string address, CancellationToken ct = default);
}

public class NominatimGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly ILogger<NominatimGeocodingService> _logger;

    public NominatimGeocodingService(HttpClient http, ILogger<NominatimGeocodingService> logger)
    {
        _http = http;
        _logger = logger;

        _http.DefaultRequestHeaders.UserAgent.Clear();
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("PetshopDelivery", "1.0"));
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("(contato: skinl2oficial2@gmail.com)"));
    }

    public async Task<(double lat, double lon)?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        // ✅ CRÍTICO: adicionar countrycodes=br e bounded=1 para focar no Brasil/RJ
        var url =
            $"https://nominatim.openstreetmap.org/search" +
            $"?format=json" +
            $"&limit=5" + // Top 5 para validar
            $"&addressdetails=0" +
            $"&countrycodes=br" + // Apenas Brasil
            $"&q={Uri.EscapeDataString(address)}";

        using var resp = await _http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Nominatim retornou status {StatusCode} para: {Address}", resp.StatusCode, address);
            return null;
        }

        var json = await resp.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            _logger.LogWarning("Nominatim não retornou resultados para: {Address}", address);
            return null;
        }

        // Procura o primeiro resultado dentro do território brasileiro
        // Bounds do Brasil: lat -35 a 5.5, lon -74 a -28.8
        foreach (var result in root.EnumerateArray())
        {
            if (!result.TryGetProperty("lat", out var latProp)) continue;
            if (!result.TryGetProperty("lon", out var lonProp)) continue;

            var latStr = latProp.GetString();
            var lonStr = lonProp.GetString();

            if (!double.TryParse(latStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                continue;

            if (!double.TryParse(lonStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                continue;

            if (!double.IsFinite(lat) || !double.IsFinite(lon)) continue;

            if (lat >= -35.0 && lat <= 5.5 && lon >= -74.0 && lon <= -28.8)
            {
                _logger.LogInformation("Nominatim: coords válidas BR - Lat={Lat}, Lon={Lon} - {Address}", lat, lon, address);
                return (lat, lon);
            }
            else
            {
                _logger.LogDebug("Nominatim: coords fora do Brasil - Lat={Lat}, Lon={Lon} - {Address}", lat, lon, address);
            }
        }

        _logger.LogWarning("Nominatim: nenhuma coord válida no Brasil para: {Address}", address);
        return null;
    }
}
