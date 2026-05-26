using Petshop.Api.Entities.Fiscal;

namespace Petshop.Api.Services.Fiscal;

/// <summary>
/// Motor fiscal real: gera o XML da NFC-e, assina com certificado A1 e transmite à SEFAZ.
/// Registrado no DI quando FiscalConfig está ativa e com certificado configurado.
/// Fallback: MockFiscalEngine (mantido no DI como default).
/// </summary>
public class RealFiscalEngine : IFiscalEngine
{
    private readonly UnimakeNfceEngine           _unimake;
    private readonly ILogger<RealFiscalEngine>   _logger;

    public RealFiscalEngine(
        UnimakeNfceEngine         unimake,
        ILogger<RealFiscalEngine> logger)
    {
        _unimake = unimake;
        _logger  = logger;
    }

    public Task<FiscalEngineResult> IssueAsync(FiscalDocumentRequest req, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[RealFiscalEngine] Emitindo NFC-e #{Number}/série {Serie} — SaleOrder {SaleId}.",
            req.Number, req.Serie, req.SaleOrderId);

        return Task.FromResult(FiscalEngineResult.InContingency("", "Certificado não configurado — use IssueWithCertAsync."));
    }

    /// <summary>Emite via Unimake.DFe com certificado A1 em bytes.</summary>
    internal Task<FiscalEngineResult> IssueWithCertAsync(
        FiscalDocumentRequest req,
        byte[] certBytes,
        string? certPassword,
        CancellationToken ct)
        => _unimake.IssueAsync(req, certBytes, certPassword, ct);

    /// <summary>Emite via Unimake.DFe lendo o certificado do disco (caminho de arquivo).</summary>
    internal async Task<FiscalEngineResult> IssueWithCertAsync(
        FiscalDocumentRequest req,
        string? certPath,
        string? certPassword,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(certPath) || !File.Exists(certPath))
        {
            _logger.LogWarning("[RealFiscalEngine] Certificado não encontrado em {Path}.", certPath);
            return FiscalEngineResult.InContingency("", $"Certificado não encontrado: {certPath}");
        }

        var certBytes = await File.ReadAllBytesAsync(certPath, ct);
        return await _unimake.IssueAsync(req, certBytes, certPassword, ct);
    }

    public Task<FiscalEngineResult> CancelAsync(
        string accessKey,
        string reason,
        CancellationToken ct = default)
    {
        // Cancelamento NFC-e requer evento fiscal (NFeEventoCancNFe4)
        // Implementação completa na próxima iteração
        _logger.LogWarning("[RealFiscalEngine] Cancelamento ainda não implementado para NFC-e.");
        return Task.FromResult(FiscalEngineResult.Rejected("999", "Cancelamento não implementado."));
    }

    public Task<bool> IsSefazOnlineAsync(string uf, CancellationToken ct = default)
        => Task.FromResult(true); // Unimake verifica online internamente durante Executar()
}
