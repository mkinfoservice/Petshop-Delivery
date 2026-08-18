using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Catalog;
using Petshop.Api.Entities.Master;
using Petshop.Api.Services.WhatsApp;

namespace Petshop.Api.Services.Master;

/// <summary>
/// Enforça PlanExpiresAtUtc automaticamente — hoje esse campo existia mas nada o lia
/// (docs/SAAS_OPERATIONS.md já registrava isso como pendência: "requer cron externo").
///
/// Reaproveita o mecanismo de suspensão já existente (Company.SuspendedAtUtc), o mesmo
/// usado pelo Master Admin manualmente — não introduz uma máquina de estados nova.
/// Dá um período de carência antes de suspender de fato, avisando o dono via
/// AdminAlert + WhatsApp nesse meio-tempo.
/// </summary>
public class PlanExpirationJob
{
    private const int GraceDays = 7;

    private readonly AppDbContext _db;
    private readonly WhatsAppClient _whatsApp;
    private readonly ILogger<PlanExpirationJob> _logger;

    public PlanExpirationJob(AppDbContext db, WhatsAppClient whatsApp, ILogger<PlanExpirationJob> logger)
    {
        _db = db;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var expired = await _db.Companies
            .Where(c =>
                !c.IsDeleted &&
                c.IsActive &&
                c.SuspendedAtUtc == null &&
                c.PlanExpiresAtUtc != null &&
                c.PlanExpiresAtUtc <= now)
            .ToListAsync(ct);

        foreach (var company in expired)
        {
            try
            {
                await ProcessAsync(company, now, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PlanExpiration] Falha ao processar empresa {CompanyId}.", company.Id);
            }
        }
    }

    private async Task ProcessAsync(Company company, DateTime now, CancellationToken ct)
    {
        var daysSinceExpiry = (now - company.PlanExpiresAtUtc!.Value).TotalDays;

        if (daysSinceExpiry >= GraceDays)
        {
            company.SuspendedAtUtc = now;
            company.SuspendedReason =
                $"Plano '{company.Plan}' expirado em {company.PlanExpiresAtUtc:dd/MM/yyyy} — " +
                $"suspensão automática após {GraceDays} dias de carência sem renovação.";
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("[PlanExpiration] Empresa {CompanyId} ({Slug}) suspensa automaticamente.", company.Id, company.Slug);

            var ownerPhone = WhatsAppClient.NormalizeToE164Brazil(company.OwnerAlertPhone);
            if (ownerPhone is not null)
                await _whatsApp.SendTextAsync(
                    ownerPhone,
                    $"[vendApps] Sua loja \"{company.Name}\" foi suspensa por falta de renovação do plano. " +
                    "Entre em contato para reativar.",
                    company.Id, ct);

            return;
        }

        // Ainda em carência — avisa 1x/dia (dedup diário, mesmo padrão do alerta de certificado).
        var daysUntilSuspension = (int)Math.Ceiling(GraceDays - daysSinceExpiry);

        var alreadyAlertedToday = await _db.AdminAlerts.AnyAsync(a =>
            a.CompanyId == company.Id &&
            a.AlertType == "plan_expiring" &&
            a.CreatedAtUtc >= now.Date, ct);

        if (alreadyAlertedToday) return;

        var title = $"Plano expirado — suspensão em {daysUntilSuspension} dia(s)";
        var message = $"O plano '{company.Plan}' expirou em {company.PlanExpiresAtUtc:dd/MM/yyyy}. " +
                       $"Sem renovação, a loja será suspensa automaticamente em {daysUntilSuspension} dia(s).";

        _db.AdminAlerts.Add(new AdminAlert
        {
            CompanyId = company.Id,
            AlertType = "plan_expiring",
            Title = title,
            Message = message,
            ReferenceId = company.Id,
        });
        await _db.SaveChangesAsync(ct);

        var ownerAlertPhone = WhatsAppClient.NormalizeToE164Brazil(company.OwnerAlertPhone);
        if (ownerAlertPhone is not null)
            await _whatsApp.SendTextAsync(ownerAlertPhone, $"[vendApps] {title} — {message}", company.Id, ct);
    }
}
