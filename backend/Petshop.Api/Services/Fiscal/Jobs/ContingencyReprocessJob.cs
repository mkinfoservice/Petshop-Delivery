using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Fiscal;
using Petshop.Api.Entities.Master;
using Petshop.Api.Services.WhatsApp;

namespace Petshop.Api.Services.Fiscal.Jobs;

/// <summary>
/// Job Hangfire recorrente: monitora documentos em contingência e escalona a
/// visibilidade operacional conforme o prazo legal se esgota.
///
/// A retransmissão em si já acontece pelo caminho normal: quando um documento
/// entra em Contingency, FiscalQueueProcessorJob recoloca o item da fila em
/// Waiting com ScheduledForUtc +30min, e a próxima execução (a cada 1 minuto)
/// tenta emitir de novo via o mesmo RealFiscalEngine/Unimake.DFe — não existe
/// um canal SVC-AN separado implementado; contingência hoje é "espera e tenta
/// de novo com o motor normal", que é suficiente para quedas temporárias.
///
/// O que ESTE job garante, e que o caminho normal não cobre:
/// - Alerta acionável (AdminAlert + WhatsApp) quando o prazo está se esgotando,
///   não só um log que ninguém vê.
/// - Prioriza (Urgent) o reprocessamento dos itens perto do vencimento.
/// - Fecha o ciclo em 48h: marca como Expired (FiscalDocument) e Failed
///   (FiscalQueue) para parar retry automático — regularização vira manual,
///   e um alerta crítico avisa o operador uma única vez.
/// </summary>
public class ContingencyReprocessJob
{
    private const int MaxContingencyHours = 48;
    private const int AlertThresholdHours = 36;
    private const int BatchSize = 100;

    private readonly AppDbContext _db;
    private readonly WhatsAppClient _whatsApp;
    private readonly ILogger<ContingencyReprocessJob> _logger;

    public ContingencyReprocessJob(AppDbContext db, WhatsAppClient whatsApp, ILogger<ContingencyReprocessJob> logger)
    {
        _db = db;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    /// <summary>Executado a cada 5 minutos pelo Hangfire.</summary>
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expireCutoff = now.AddHours(-MaxContingencyHours);
        var alertCutoff = now.AddHours(-AlertThresholdHours);

        var pending = await _db.FiscalDocuments
            .Where(d => d.FiscalStatus == FiscalDocumentStatus.Contingency
                     && d.ContingencyType != ContingencyType.None
                     && d.CreatedAtUtc >= expireCutoff)
            .OrderBy(d => d.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        var expired = await _db.FiscalDocuments
            .Where(d => d.FiscalStatus == FiscalDocumentStatus.Contingency
                     && d.ContingencyType != ContingencyType.None
                     && d.CreatedAtUtc < expireCutoff)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0 && expired.Count == 0)
        {
            _logger.LogDebug("[Contingência] Nenhum documento pendente.");
            return;
        }

        var expiring = pending.Where(d => d.CreatedAtUtc < alertCutoff).ToList();

        foreach (var doc in expiring)
            await AlertAndPrioritizeAsync(doc, now, ct);

        foreach (var doc in expired)
            await ExpireAsync(doc, ct);

        _logger.LogInformation(
            "[Contingência] scan: {Pending} pendente(s), {Expiring} vencendo, {Expired} expirado(s) nesta execução.",
            pending.Count, expiring.Count, expired.Count);
    }

    private async Task AlertAndPrioritizeAsync(FiscalDocument doc, DateTime now, CancellationToken ct)
    {
        // Prioriza o reprocessamento — próxima execução do FiscalQueueProcessorJob pega primeiro.
        var queueItem = await _db.FiscalQueues
            .FirstOrDefaultAsync(q => q.FiscalDocumentId == doc.Id && q.Status == FiscalQueueStatus.Waiting, ct);
        if (queueItem is not null)
        {
            queueItem.Priority = FiscalQueuePriority.Urgent;
            queueItem.ScheduledForUtc = null;
        }

        var alreadyAlertedToday = await _db.AdminAlerts.AnyAsync(a =>
            a.CompanyId == doc.CompanyId &&
            a.AlertType == "fiscal_contingency_expiring" &&
            a.ReferenceId == doc.Id &&
            a.CreatedAtUtc >= now.Date, ct);

        if (!alreadyAlertedToday)
        {
            var hoursLeft = MaxContingencyHours - (int)Math.Floor((now - doc.CreatedAtUtc).TotalHours);
            var title = $"NFC-e em contingência vencendo em {hoursLeft}h";
            var message = $"Documento fiscal (nº {doc.Number}, série {doc.Serie}) está em contingência desde " +
                           $"{doc.CreatedAtUtc:dd/MM/yyyy HH:mm}. Prazo legal de {MaxContingencyHours}h se esgota em " +
                           $"{hoursLeft}h — se não regularizar, requer ação manual.";

            _db.AdminAlerts.Add(new AdminAlert
            {
                CompanyId = doc.CompanyId,
                AlertType = "fiscal_contingency_expiring",
                Title = title,
                Message = message,
                ReferenceId = doc.Id,
            });

            var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == doc.CompanyId, ct);
            var ownerPhone = WhatsAppClient.NormalizeToE164Brazil(company?.OwnerAlertPhone);
            if (ownerPhone is not null)
                await _whatsApp.SendTextAsync(ownerPhone, $"[Alerta fiscal] {title} — {message}", doc.CompanyId, ct);

            _logger.LogWarning(
                "[Contingência] ALERTA: doc {DocId} (empresa {CompanyId}) vence em {Hours}h.",
                doc.Id, doc.CompanyId, hoursLeft);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task ExpireAsync(FiscalDocument doc, CancellationToken ct)
    {
        const string reason = "Prazo de 48h em contingência expirado — regularização manual obrigatória junto ao contador.";

        doc.FiscalStatus = FiscalDocumentStatus.Expired;
        doc.RejectMessage = reason;
        doc.UpdatedAtUtc = DateTime.UtcNow;

        var queueItems = await _db.FiscalQueues
            .Where(q => q.FiscalDocumentId == doc.Id && q.Status != FiscalQueueStatus.Completed)
            .ToListAsync(ct);
        foreach (var q in queueItems)
        {
            q.Status = FiscalQueueStatus.Failed;
            q.FailureReason = reason;
        }

        // Alerta uma única vez — estado é terminal, não faz sentido repetir diariamente.
        var alreadyAlerted = await _db.AdminAlerts.AnyAsync(a =>
            a.CompanyId == doc.CompanyId &&
            a.AlertType == "fiscal_contingency_expired" &&
            a.ReferenceId == doc.Id, ct);

        if (!alreadyAlerted)
        {
            var title = "NFC-e em contingência expirou sem regularização";
            var message = $"Documento fiscal (nº {doc.Number}, série {doc.Serie}, criado em {doc.CreatedAtUtc:dd/MM/yyyy HH:mm}) " +
                           $"ultrapassou o prazo legal de {MaxContingencyHours}h em contingência. {reason}";

            _db.AdminAlerts.Add(new AdminAlert
            {
                CompanyId = doc.CompanyId,
                AlertType = "fiscal_contingency_expired",
                Title = title,
                Message = message,
                ReferenceId = doc.Id,
            });

            var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == doc.CompanyId, ct);
            var ownerPhone = WhatsAppClient.NormalizeToE164Brazil(company?.OwnerAlertPhone);
            if (ownerPhone is not null)
                await _whatsApp.SendTextAsync(ownerPhone, $"[Alerta fiscal CRÍTICO] {title} — {message}", doc.CompanyId, ct);

            _logger.LogError(
                "[Contingência] CRÍTICO: doc {DocId} (empresa {CompanyId}) expirou sem regularização.",
                doc.Id, doc.CompanyId);
        }

        await _db.SaveChangesAsync(ct);
    }
}
