using Microsoft.Extensions.Diagnostics.HealthChecks;
using Petshop.Api.Data;

namespace Petshop.Api.Health;

/// <summary>Readiness: confirma que a API consegue de fato conversar com o Postgres.</summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public DatabaseHealthCheck(AppDbContext db) => _db = db;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(ct);
            return canConnect
                ? HealthCheckResult.Healthy("Postgres alcançável.")
                : HealthCheckResult.Unhealthy("Postgres não respondeu.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao conectar no Postgres.", ex);
        }
    }
}
