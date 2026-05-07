using System.Globalization;
using System.Text.Json;

namespace Petshop.Api.Services.Routes;

/// <summary>
/// Matriz de distâncias/tempos reais via Google Distance Matrix API.
/// Fallback quando ORS Matrix falhar.
/// Limite: 25 origens × 25 destinos por chamada (max 100 elementos).
/// </summary>
public class GoogleDistanceMatrixService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleDistanceMatrixService> _logger;

    public GoogleDistanceMatrixService(HttpClient http, IConfiguration config, ILogger<GoogleDistanceMatrixService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Calcula matriz de tempos de trajeto entre múltiplos pontos.
    /// Retorna matrix[origem][destino] = segundos de viagem (ou null se falhar).
    /// </summary>
    public async Task<double[][]?> GetTravelTimeMatrixAsync(
        List<(double lat, double lon)> coordinates,
        CancellationToken ct = default)
    {
        if (coordinates == null || coordinates.Count < 2)
        {
            _logger.LogWarning("Google Matrix: precisa de pelo menos 2 coordenadas");
            return null;
        }

        var key = _config["Geocoding:Google:ApiKey"];
        if (string.IsNullOrWhiteSpace(key)) return null;

        // Google Distance Matrix: máximo 25×25 = 625 elementos por chamada
        if (coordinates.Count > 25)
        {
            _logger.LogWarning("Google Matrix: {Count} pontos excedem limite de 25 por chamada, usando Haversine", coordinates.Count);
            return null;
        }

        // Pipe-separated lat,lon — Google não requer encoding do pipe
        var latLngStr = string.Join("|", coordinates.Select(c =>
            $"{c.lat.ToString("F6", CultureInfo.InvariantCulture)},{c.lon.ToString("F6", CultureInfo.InvariantCulture)}"));

        var url = "https://maps.googleapis.com/maps/api/distancematrix/json" +
                  $"?origins={latLngStr}" +
                  $"&destinations={latLngStr}" +
                  $"&mode=driving" +
                  $"&key={Uri.EscapeDataString(key)}";

        try
        {
            _logger.LogInformation("Google Matrix: calculando tempos de trajeto para {Count} pontos...", coordinates.Count);

            using var resp = await _http.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Matrix: HTTP {Status}", resp.StatusCode);
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var status = doc.RootElement.TryGetProperty("status", out var sp) ? sp.GetString() : null;
            if (status != "OK")
            {
                _logger.LogWarning("Google Matrix: status={Status}", status);
                return null;
            }

            if (!doc.RootElement.TryGetProperty("rows", out var rows)) return null;

            var n = coordinates.Count;
            var matrix = new double[n][];

            var i = 0;
            foreach (var row in rows.EnumerateArray())
            {
                if (i >= n) break;
                matrix[i] = new double[n];

                if (!row.TryGetProperty("elements", out var elements)) { i++; continue; }

                var j = 0;
                foreach (var element in elements.EnumerateArray())
                {
                    if (j >= n) break;

                    var elemStatus = element.TryGetProperty("status", out var es) ? es.GetString() : null;

                    if (elemStatus == "OK" &&
                        element.TryGetProperty("duration", out var dur) &&
                        dur.TryGetProperty("value", out var val))
                    {
                        matrix[i][j] = val.GetDouble();
                    }
                    else
                    {
                        // Elemento inacessível — usa valor alto para o algoritmo evitar esta aresta
                        matrix[i][j] = double.MaxValue / 2;
                        _logger.LogDebug("Google Matrix: elemento [{I}][{J}] inacessível (status={S})", i, j, elemStatus);
                    }

                    j++;
                }

                i++;
            }

            _logger.LogInformation("✅ Google Matrix: matriz {Size}x{Size} calculada com sucesso", n, n);
            return matrix;
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Google Matrix: timeout");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Matrix: erro ao calcular matriz");
            return null;
        }
    }
}
