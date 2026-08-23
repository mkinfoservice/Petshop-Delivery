using Petshop.Api.Services.Enrichment;

namespace Petshop.Api.Services.Enrichment.Jobs;

/// <summary>
/// Job Hangfire que executa a geração de descrições via IA de um lote de enriquecimento.
/// Só processa se EnableDescriptionGeneration = true na EnrichmentConfig da empresa.
/// Enfileirado pelo CatalogEnrichmentController (opcionalmente, após normalização).
/// </summary>
public sealed class EnrichGenerateDescriptionsJob
{
    private readonly CatalogEnrichmentOrchestrator _orchestrator;
    private readonly ILogger<EnrichGenerateDescriptionsJob> _logger;

    public EnrichGenerateDescriptionsJob(
        CatalogEnrichmentOrchestrator orchestrator,
        ILogger<EnrichGenerateDescriptionsJob> logger)
    {
        _orchestrator = orchestrator;
        _logger       = logger;
    }

    public async Task ExecuteAsync(Guid batchId, CancellationToken ct = default)
    {
        _logger.LogInformation("Job de geração de descrição iniciado para lote {BatchId}", batchId);
        await _orchestrator.RunDescriptionGenerationAsync(batchId, ct);
        _logger.LogInformation("Job de geração de descrição concluído para lote {BatchId}", batchId);
    }
}
