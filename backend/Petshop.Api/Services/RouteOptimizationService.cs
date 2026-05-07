using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities;
using Petshop.Api.Services.Routes;

namespace Petshop.Api.Services;

public class RouteOptimizationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RouteOptimizationService> _logger;
    private readonly OrsMatrixService? _orsMatrix;
    private readonly GoogleDistanceMatrixService? _googleMatrix;

    public RouteOptimizationService(
        AppDbContext db,
        ILogger<RouteOptimizationService> logger,
        OrsMatrixService? orsMatrix = null,
        GoogleDistanceMatrixService? googleMatrix = null)
    {
        _db = db;
        _logger = logger;
        _orsMatrix = orsMatrix;
        _googleMatrix = googleMatrix;
    }

    // Distância aproximada (Haversine) em km
    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
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
    
    private static bool LooksLikeRio(double lat, double lon)
    {
        // Bounds do estado RJ: lat entre -23.4 e -20.7, lon entre -44.9 e -40.9
        return lat >= -23.2 && lat <= -22.6 && lon >= -44.1 && lon <= -43.0;
    }

    /// <summary>
    /// Otimização com ORS Matrix API (tempo real de trajeto) + fallback Haversine.
    /// Start = mais antigo; depois greedy "vizinho mais próximo".
    /// Pedidos sem coordenadas são colocados no final (ordenados por CreatedAtUtc).
    /// </summary>
    public async Task<List<Order>> OptimizeWithMatrixAsync(List<Order> orders, CancellationToken ct = default)
    {
        // Separa pedidos com/sem coordenadas
        var withCoords = orders
            .Where(o => o.Latitude != null && o.Longitude != null)
            .ToList();

        var withoutCoords = orders
            .Where(o => o.Latitude == null || o.Longitude == null)
            .OrderBy(o => o.CreatedAtUtc)
            .ToList();

        _logger.LogInformation("🚗 RouteOptimization (Matrix): received {Count} orders, withCoords={WithCoords}, withoutCoords={WithoutCoords}",
            orders.Count, withCoords.Count, withoutCoords.Count);

        // ⚠️ Alerta se receber pedidos sem coords
        if (withoutCoords.Count > 0)
        {
            _logger.LogWarning("⚠️ {Count} pedidos SEM coordenadas serão colocados no final: {Orders}",
                withoutCoords.Count,
                string.Join(", ", withoutCoords.Select(o => o.PublicId)));
        }

        // Se não há pedidos com coords, retorna só os sem coords (fallback)
        if (withCoords.Count == 0)
        {
            _logger.LogWarning("⚠️ NENHUM pedido com coordenadas! Retornando sem otimização.");
            return withoutCoords;
        }

        // Se só há 1 pedido com coords, não precisa otimizar
        if (withCoords.Count == 1)
        {
            _logger.LogInformation("✅ Apenas 1 pedido com coords, sem necessidade de otimização.");
            var result = new List<Order>(withCoords);
            result.AddRange(withoutCoords);
            return result;
        }

        // Log detalhado de cada pedido COM coordenadas
        foreach (var o in withCoords)
        {
            var looksLikeRio = LooksLikeRio(o.Latitude!.Value, o.Longitude!.Value);

            _logger.LogInformation("📍 Order={PublicId} CreatedAtUtc={CreatedAtUtc} Lat={Lat:F6} Lon={Lon:F6} LooksLikeRio={LooksLikeRio}",
                o.PublicId, o.CreatedAtUtc, o.Latitude, o.Longitude, looksLikeRio);

            if (!looksLikeRio)
            {
                _logger.LogWarning("⚠️ OUTLIER detectado! Order={PublicId} com coords fora do RJ: Lat={Lat:F6} Lon={Lon:F6}",
                    o.PublicId, o.Latitude, o.Longitude);
            }
        }

        // 1) Start fixo: pedido mais antigo
        var start = withCoords.OrderBy(o => o.CreatedAtUtc).First();
        _logger.LogInformation("🎯 START (oldest) = {PublicId} ({CreatedAtUtc:yyyy-MM-dd HH:mm})",
            start.PublicId, start.CreatedAtUtc);

        // 2) Tenta usar matrix API para tempos reais: ORS → Google → Haversine
        double[][]? travelTimeMatrix = null;
        var coords = withCoords.Select(o => (o.Latitude!.Value, o.Longitude!.Value)).ToList();

        if (_orsMatrix != null)
        {
            travelTimeMatrix = await _orsMatrix.GetTravelTimeMatrixAsync(coords, ct);
            if (travelTimeMatrix != null)
                _logger.LogInformation("✅ ORS Matrix: usando tempos reais de trajeto!");
            else
                _logger.LogWarning("⚠️ ORS Matrix falhou, tentando Google Distance Matrix...");
        }

        if (travelTimeMatrix == null && _googleMatrix != null)
        {
            travelTimeMatrix = await _googleMatrix.GetTravelTimeMatrixAsync(coords, ct);
            if (travelTimeMatrix != null)
                _logger.LogInformation("✅ Google Distance Matrix: usando tempos reais de trajeto!");
            else
                _logger.LogWarning("⚠️ Google Distance Matrix falhou, usando fallback Haversine");
        }

        if (travelTimeMatrix == null)
            _logger.LogInformation("ℹ️ Usando Haversine (distância em linha reta) para otimização");

        // 3) Greedy nearest neighbor (com Matrix API ou Haversine)
        var remaining = new List<Order>(withCoords);
        remaining.Remove(start);

        var optimized = new List<Order> { start };
        var current = start;

        while (remaining.Count > 0)
        {
            Order next;
            double metric;
            string metricUnit;

            if (travelTimeMatrix != null)
            {
                // Usa tempo real de trajeto (segundos)
                var currentIdx = withCoords.IndexOf(current);
                next = remaining
                    .OrderBy(o =>
                    {
                        var destIdx = withCoords.IndexOf(o);
                        return travelTimeMatrix[currentIdx][destIdx];
                    })
                    .First();

                var nextIdx = withCoords.IndexOf(next);
                metric = travelTimeMatrix[currentIdx][nextIdx] / 60.0; // Converte para minutos
                metricUnit = "min";
            }
            else
            {
                // Fallback: Haversine (distância em linha reta)
                next = remaining
                    .OrderBy(o => HaversineKm(
                        current.Latitude!.Value, current.Longitude!.Value,
                        o.Latitude!.Value, o.Longitude!.Value
                    ))
                    .First();

                metric = HaversineKm(
                    current.Latitude!.Value, current.Longitude!.Value,
                    next.Latitude!.Value, next.Longitude!.Value
                );
                metricUnit = "km";
            }

            _logger.LogInformation("🚗 pick next={Next} from current={Current} {Metric:N2} {Unit}",
                next.PublicId, current.PublicId, metric, metricUnit);

            // ⚠️ Alerta se métrica for muito grande
            if ((metricUnit == "min" && metric > 30) || (metricUnit == "km" && metric > 50))
            {
                _logger.LogWarning("⚠️ Valor MUITO GRANDE ({Metric:N2} {Unit}) entre {Current} e {Next}!",
                    metric, metricUnit, current.PublicId, next.PublicId);
            }

            optimized.Add(next);
            remaining.Remove(next);
            current = next;
        }

        // 4) Adiciona pedidos sem coords no final
        optimized.AddRange(withoutCoords);

        _logger.LogInformation("✅ RouteOptimization: final order => {Sequence}",
            string.Join(" -> ", optimized.Select(x => x.PublicId)));

        return optimized;
    }

    /// <summary>
    /// Versão síncrona (mantida para compatibilidade): usa apenas Haversine.
    /// </summary>
    public List<Order> Optimize(List<Order> orders)
    {
        // Separa pedidos com/sem coordenadas
        var withCoords = orders
            .Where(o => o.Latitude != null && o.Longitude != null)
            .ToList();

        var withoutCoords = orders
            .Where(o => o.Latitude == null || o.Longitude == null)
            .OrderBy(o => o.CreatedAtUtc)
            .ToList();

        _logger.LogInformation("RouteOptimization: received {Count} orders, withCoords={WithCoords}, withoutCoords={WithoutCoords}",
            orders.Count, withCoords.Count, withoutCoords.Count);

        // ⚠️ Alerta se receber pedidos sem coords
        if (withoutCoords.Count > 0)
        {
            _logger.LogWarning("RouteOptimization: {Count} pedidos SEM coordenadas serão colocados no final: {Orders}",
                withoutCoords.Count,
                string.Join(", ", withoutCoords.Select(o => o.PublicId)));
        }

        // Se não há pedidos com coords, retorna só os sem coords (fallback)
        if (withCoords.Count == 0)
        {
            _logger.LogWarning("RouteOptimization: NENHUM pedido com coordenadas! Retornando sem otimização.");
            return withoutCoords;
        }

        // Se só há 1 pedido com coords, não precisa otimizar
        if (withCoords.Count == 1)
        {
            _logger.LogInformation("RouteOptimization: apenas 1 pedido com coords, sem necessidade de otimização.");
            var result = new List<Order>(withCoords);
            result.AddRange(withoutCoords);
            return result;
        }

        // Log detalhado de cada pedido COM coordenadas
        foreach (var o in withCoords)
        {
            var looksLikeRio = LooksLikeRio(o.Latitude!.Value, o.Longitude!.Value);

            _logger.LogInformation("RouteOptimization: Order={PublicId} CreatedAtUtc={CreatedAtUtc} Lat={Lat:F6} Lon={Lon:F6} LooksLikeRio={LooksLikeRio}",
                o.PublicId, o.CreatedAtUtc, o.Latitude, o.Longitude, looksLikeRio);

            // ⚠️ Alerta se coordenadas parecem suspeitas
            if (!looksLikeRio)
            {
                _logger.LogWarning("RouteOptimization: ⚠️ OUTLIER detectado! Order={PublicId} com coords fora do RJ: Lat={Lat:F6} Lon={Lon:F6}",
                    o.PublicId, o.Latitude, o.Longitude);
            }
        }

        // 1) Start fixo: pedido mais antigo
        var start = withCoords.OrderBy(o => o.CreatedAtUtc).First();
        _logger.LogInformation("RouteOptimization: START (oldest) = {PublicId} ({CreatedAtUtc:yyyy-MM-dd HH:mm})",
            start.PublicId, start.CreatedAtUtc);

        var remaining = new List<Order>(withCoords);
        remaining.Remove(start);

        var optimized = new List<Order> { start };
        var current = start;

        // 2) Greedy encadeado: sempre pega o mais próximo do atual
        while (remaining.Count > 0)
        {
            var next = remaining
                .OrderBy(o => HaversineKm(
                    current.Latitude!.Value, current.Longitude!.Value,
                    o.Latitude!.Value, o.Longitude!.Value
                ))
                .First();

            var km = HaversineKm(
                current.Latitude!.Value, current.Longitude!.Value,
                next.Latitude!.Value, next.Longitude!.Value
            );

            _logger.LogInformation("RouteOptimization: pick next={Next} from current={Current} km={Km:N2}",
                next.PublicId, current.PublicId, km);

            // ⚠️ Alerta se distância for muito grande (possível problema de coords)
            if (km > 50)
            {
                _logger.LogWarning("RouteOptimization: ⚠️ Distância MUITO GRANDE ({Km:N2} km) entre {Current} e {Next}! Possível problema de geocoding.",
                    km, current.PublicId, next.PublicId);
            }

            optimized.Add(next);
            remaining.Remove(next);
            current = next;
        }

        // 3) Adiciona pedidos sem coords no final
        optimized.AddRange(withoutCoords);

        _logger.LogInformation("RouteOptimization: final order => {Sequence}",
            string.Join(" -> ", optimized.Select(x => x.PublicId)));

        return optimized;
    }

    /// <summary>
    /// Otimização com depot como ponto de partida fixo.
    /// Inclui depot na matriz ORS Matrix API ou usa Haversine com depot como origem.
    /// Start = depot; depois greedy "vizinho mais próximo".
    /// </summary>
    public async Task<List<Order>> OptimizeWithDepotAsync(
        List<Order> orders,
        (double lat, double lon) depot,
        CancellationToken ct = default)
    {
        // Separar pedidos com/sem coordenadas
        var withCoords = orders
            .Where(o => o.Latitude != null && o.Longitude != null)
            .ToList();

        var withoutCoords = orders
            .Where(o => o.Latitude == null || o.Longitude == null)
            .OrderBy(o => o.CreatedAtUtc)
            .ToList();

        _logger.LogInformation("🗺️ RouteOptimization (com Depot): received {Count} orders, withCoords={WithCoords}, withoutCoords={WithoutCoords}",
            orders.Count, withCoords.Count, withoutCoords.Count);

        if (withoutCoords.Count > 0)
        {
            _logger.LogWarning("⚠️ {Count} pedidos SEM coordenadas serão colocados no final",
                withoutCoords.Count);
        }

        // Se não há pedidos com coords, retorna só os sem coords
        if (withCoords.Count == 0)
        {
            _logger.LogWarning("⚠️ NENHUM pedido com coordenadas! Retornando sem otimização.");
            return withoutCoords.OrderBy(o => o.CreatedAtUtc).ToList();
        }

        _logger.LogInformation("📍 DEPOT como ponto de partida: ({DepotLat:F6}, {DepotLon:F6})",
            depot.lat, depot.lon);

        // Incluir depot como primeiro ponto na matriz
        var allCoordinates = new List<(double lat, double lon)> { depot };
        allCoordinates.AddRange(withCoords.Select(o => (o.Latitude!.Value, o.Longitude!.Value)));

        // Calcular matriz de tempos (ou fallback Haversine)
        double[][]? matrix = null;
        string metricType = "Haversine";

        var expectedSize = allCoordinates.Count;

        // Tenta ORS → Google → Haversine
        if (_orsMatrix != null)
        {
            try
            {
                var orsMatrix = await _orsMatrix.GetTravelTimeMatrixAsync(allCoordinates, ct);
                if (orsMatrix != null &&
                    orsMatrix.Length == expectedSize &&
                    orsMatrix.All(row => row != null && row.Length == expectedSize))
                {
                    matrix = orsMatrix;
                    metricType = "ORS Matrix";
                    _logger.LogInformation("🚗 ORS Matrix ({Size}x{Size}): tempos reais de trajeto", expectedSize, expectedSize);
                }
                else
                {
                    _logger.LogWarning("⚠️ ORS Matrix retornou dimensões inválidas, tentando Google...");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ ORS Matrix falhou ({Error}), tentando Google...", ex.Message);
            }
        }

        if (matrix == null && _googleMatrix != null)
        {
            try
            {
                var gMatrix = await _googleMatrix.GetTravelTimeMatrixAsync(allCoordinates, ct);
                if (gMatrix != null &&
                    gMatrix.Length == expectedSize &&
                    gMatrix.All(row => row != null && row.Length == expectedSize))
                {
                    matrix = gMatrix;
                    metricType = "Google Distance Matrix";
                    _logger.LogInformation("🚗 Google Distance Matrix ({Size}x{Size}): tempos reais de trajeto", expectedSize, expectedSize);
                }
                else
                {
                    _logger.LogWarning("⚠️ Google Distance Matrix retornou dimensões inválidas, usando Haversine.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("⚠️ Google Distance Matrix falhou ({Error}), usando Haversine.", ex.Message);
            }
        }

        if (matrix == null)
        {
            _logger.LogInformation("🚗 Usando Haversine (distância em linha reta) para otimização");
        }

        // Greedy nearest neighbor a partir do depot (índice 0)
        var remaining = new List<Order>(withCoords);
        var optimized = new List<Order>();
        int currentIdx = 0; // Depot é índice 0 na matriz

        while (remaining.Count > 0)
        {
            Order next;
            double metric;
            string metricUnit;

            try
            {
                if (matrix != null)
                {
                    // Usar tempos reais da matriz ORS
                    next = remaining.OrderBy(o =>
                    {
                        var destIdx = withCoords.IndexOf(o) + 1; // +1 porque depot é índice 0
                        if (destIdx < 0 || destIdx >= expectedSize || currentIdx >= expectedSize)
                            return double.MaxValue;
                        return matrix[currentIdx][destIdx];
                    }).First();

                    var nextIdx = withCoords.IndexOf(next) + 1;
                    if (nextIdx >= 0 && nextIdx < expectedSize && currentIdx < expectedSize)
                    {
                        metric = matrix[currentIdx][nextIdx] / 60.0;
                    }
                    else
                    {
                        metric = 0;
                    }
                    metricUnit = "min";
                    currentIdx = nextIdx;
                }
                else
                {
                    // Fallback Haversine
                    var currentCoords = currentIdx >= 0 && currentIdx < allCoordinates.Count
                        ? allCoordinates[currentIdx]
                        : depot;

                    next = remaining.OrderBy(o =>
                        HaversineKm(currentCoords.lat, currentCoords.lon,
                                   o.Latitude!.Value, o.Longitude!.Value)
                    ).First();

                    metric = HaversineKm(
                        currentCoords.lat, currentCoords.lon,
                        next.Latitude!.Value, next.Longitude!.Value
                    );
                    metricUnit = "km";
                    currentIdx = withCoords.IndexOf(next) + 1;
                }
            }
            catch (Exception ex)
            {
                // Fallback: pega o próximo restante e continua
                _logger.LogWarning(ex, "⚠️ Erro no loop greedy, pegando próximo pedido disponível");
                next = remaining[0];
                metric = 0;
                metricUnit = "km";
                currentIdx = withCoords.IndexOf(next) + 1;
            }

            if (optimized.Count == 0)
            {
                _logger.LogInformation("🚗 DEPOT → {Next} ({Metric:N2} {Unit})",
                    next.PublicId, metric, metricUnit);
            }
            else
            {
                var prev = optimized[^1];
                _logger.LogInformation("🚗 {Prev} → {Next} ({Metric:N2} {Unit})",
                    prev.PublicId, next.PublicId, metric, metricUnit);
            }

            // Alerta se métrica muito grande (possível outlier)
            if ((metricUnit == "km" && metric > 50) || (metricUnit == "min" && metric > 30))
            {
                _logger.LogWarning("⚠️ Métrica MUITO GRANDE: {Metric:N2} {Unit} até {Order}",
                    metric, metricUnit, next.PublicId);
            }

            optimized.Add(next);
            remaining.Remove(next);
        }

        // Adicionar pedidos sem coords no final
        optimized.AddRange(withoutCoords);

        _logger.LogInformation("✅ RouteOptimization com Depot ({Method}): DEPOT → {Sequence}",
            metricType,
            string.Join(" → ", optimized.Select(x => x.PublicId)));

        return optimized;
    }

    /// <summary>
    /// Versão async: busca PRONTO_PARA_ENTREGA e ordena (MVP).
    /// Preparada para no futuro usar ORS (tempo/distância real).
    /// </summary>
    public async Task<List<Order>> OptimizeAsync(CancellationToken ct = default)
{
    var orders = await _db.Orders
        .Where(o => o.Status == OrderStatus.PRONTO_PARA_ENTREGA
                    && o.Latitude != null && o.Longitude != null)
        .OrderBy(o => o.CreatedAtUtc)
        .ToListAsync(ct);

    if (orders.Count <= 1) return orders;

    var ordered = new List<Order>();
    var remaining = new List<Order>(orders);

    var current = remaining.First(); // mais antigo
    ordered.Add(current);
    remaining.Remove(current);

    while (remaining.Any())
    {
        var next = GetNearestLocal(current, remaining);
        ordered.Add(next);
        remaining.Remove(next);
        current = next;
    }

    return ordered;
}

private static Order GetNearestLocal(Order origin, List<Order> candidates)
{
    return candidates
        .OrderBy(o => HaversineKm(
            origin.Latitude!.Value, origin.Longitude!.Value,
            o.Latitude!.Value, o.Longitude!.Value))
        .First();
}

    /// <summary>
    /// Hoje: fallback Haversine (rápido e confiável).
    /// Futuro: substituir por ORS Matrix/Directions para tempo real de trajeto.
    /// </summary>
    private Task<Order> GetNearestAsync(Order origin, List<Order> candidates, CancellationToken ct)
    {
        // ✅ fallback simples e correto (Haversine)
        var next = candidates
            .OrderBy(o => HaversineKm(
                origin.Latitude!.Value, origin.Longitude!.Value,
                o.Latitude!.Value, o.Longitude!.Value))
            .First();

        return Task.FromResult(next);
    }
}
