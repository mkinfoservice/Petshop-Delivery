using System.Text.Json;

namespace Petshop.Api.Services.Geocoding;

/// <summary>
/// Geocoding via Google Maps Geocoding API — terceiro nível de fallback.
/// Só é chamado quando ORS e Nominatim falharem.
/// </summary>
public class GoogleGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleGeocodingService> _logger;

    public GoogleGeocodingService(HttpClient http, IConfiguration config, ILogger<GoogleGeocodingService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<(double lat, double lon)?> GeocodeAsync(string address, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;

        var key = _config["Geocoding:Google:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;

        var url = "https://maps.googleapis.com/maps/api/geocode/json" +
                  $"?address={Uri.EscapeDataString(address)}" +
                  $"&region=br" +
                  $"&key={Uri.EscapeDataString(key)}";

        try
        {
            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Geocoding: HTTP {Status} para: {Address}", resp.StatusCode, address);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var status = doc.RootElement.TryGetProperty("status", out var sp) ? sp.GetString() : null;
            if (status != "OK")
            {
                _logger.LogWarning("Google Geocoding: status={Status} para: {Address}", status, address);
                return null;
            }

            if (!doc.RootElement.TryGetProperty("results", out var results)) return null;
            if (results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0) return null;

            var first = results[0];
            if (!first.TryGetProperty("geometry", out var geom)) return null;
            if (!geom.TryGetProperty("location", out var loc)) return null;

            var lat = loc.GetProperty("lat").GetDouble();
            var lon = loc.GetProperty("lng").GetDouble();

            if (!double.IsFinite(lat) || !double.IsFinite(lon)) return null;

            // Bounds do Brasil: lat -35 a 5.5, lon -74 a -28.8
            if (lat >= -35.0 && lat <= 5.5 && lon >= -74.0 && lon <= -28.8)
            {
                _logger.LogInformation("Google Geocoding: coords válidas BR - Lat={Lat}, Lon={Lon} - {Address}", lat, lon, address);
                return (lat, lon);
            }

            _logger.LogWarning("Google Geocoding: coords fora do Brasil - Lat={Lat}, Lon={Lon} - {Address}", lat, lon, address);
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Google Geocoding: timeout para: {Address}", address);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Geocoding: erro para: {Address}", address);
            return null;
        }
    }
}
