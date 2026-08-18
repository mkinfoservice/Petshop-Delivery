using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Master;
using Petshop.Api.Services.WhatsApp;

namespace Petshop.Api.Services.Fiscal.Jobs;

/// <summary>
/// Varre certificados fiscais ativos (FiscalConfig e CashRegisterFiscalConfig) e cria
/// AdminAlert + WhatsApp quando um certificado está perto de vencer ou já venceu.
/// Sem isso, o Go-Live Readiness só avisa quando alguém abre a tela — um certificado
/// pode vencer silenciosamente entre uma visita e outra ao Master Admin.
/// </summary>
public class FiscalCertExpiryAlertJob
{
    private const int WarnDaysBeforeExpiry = 30;

    private readonly AppDbContext _db;
    private readonly FiscalCertProtectionService _certSvc;
    private readonly WhatsAppClient _whatsApp;
    private readonly ILogger<FiscalCertExpiryAlertJob> _logger;

    public FiscalCertExpiryAlertJob(
        AppDbContext db,
        FiscalCertProtectionService certSvc,
        WhatsAppClient whatsApp,
        ILogger<FiscalCertExpiryAlertJob> logger)
    {
        _db = db;
        _certSvc = certSvc;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var companyConfigs = await _db.FiscalConfigs
            .Where(f => f.IsActive && f.CertificateBase64 != null)
            .Select(f => new { f.Id, f.CompanyId, f.CertificateBase64, f.CertificatePassword, Source = "FiscalConfig (empresa)" })
            .ToListAsync(ct);

        var registerConfigs = await _db.CashRegisterFiscalConfigs
            .Where(r => r.IsActive && r.CertificateBase64 != null)
            .Select(r => new { r.Id, CompanyId = r.CashRegister.CompanyId, r.CertificateBase64, r.CertificatePassword, Source = "Caixa" })
            .ToListAsync(ct);

        foreach (var cfg in companyConfigs)
            await CheckOneAsync(cfg.Id, cfg.CompanyId, cfg.CertificateBase64, cfg.CertificatePassword, cfg.Source, ct);

        foreach (var cfg in registerConfigs)
            await CheckOneAsync(cfg.Id, cfg.CompanyId, cfg.CertificateBase64, cfg.CertificatePassword, cfg.Source, ct);
    }

    private async Task CheckOneAsync(
        Guid referenceId, Guid companyId, string? certBase64Raw, string? certPasswordRaw, string source, CancellationToken ct)
    {
        try
        {
            var certBase64 = _certSvc.Unprotect(certBase64Raw);
            if (string.IsNullOrWhiteSpace(certBase64)) return;

            var certPassword = _certSvc.Unprotect(certPasswordRaw);
            var certBytes = Convert.FromBase64String(certBase64);

            using var cert = new X509Certificate2(certBytes, certPassword ?? "", X509KeyStorageFlags.EphemeralKeySet);
            var daysToExpire = (int)Math.Floor((cert.NotAfter - DateTime.Now).TotalDays);

            if (daysToExpire > WarnDaysBeforeExpiry) return;

            // Dedup diário: uma varredura por dia é suficiente, mesmo sem marcar como lido —
            // um certificado vencendo merece lembrete recorrente, não só uma vez.
            var alreadyAlertedToday = await _db.AdminAlerts.AnyAsync(a =>
                a.CompanyId == companyId &&
                a.AlertType == "fiscal_cert_expiring" &&
                a.ReferenceId == referenceId &&
                a.CreatedAtUtc >= DateTime.UtcNow.Date, ct);

            if (alreadyAlertedToday) return;

            var expired = daysToExpire < 0;
            var title = expired
                ? $"Certificado fiscal vencido ({source})"
                : $"Certificado fiscal vencendo em {daysToExpire} dia(s) ({source})";
            var message = expired
                ? $"O certificado digital A1 usado para emissão de NFC-e ({source}) venceu em {cert.NotAfter:dd/MM/yyyy}. " +
                  "Nenhuma nota fiscal real será emitida até a renovação — configure um novo certificado."
                : $"O certificado digital A1 usado para emissão de NFC-e ({source}) vence em {cert.NotAfter:dd/MM/yyyy} " +
                  $"({daysToExpire} dia(s)). Renove antes do vencimento para não interromper a emissão fiscal.";

            _db.AdminAlerts.Add(new AdminAlert
            {
                CompanyId = companyId,
                AlertType = "fiscal_cert_expiring",
                Title = title,
                Message = message,
                ReferenceId = referenceId,
            });
            await _db.SaveChangesAsync(ct);

            var company = await _db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct);
            var ownerPhone = WhatsAppClient.NormalizeToE164Brazil(company?.OwnerAlertPhone);
            if (ownerPhone is not null)
                await _whatsApp.SendTextAsync(ownerPhone, $"[Alerta fiscal] {title} — {message}", companyId, ct);
        }
        catch (Exception ex)
        {
            // Best-effort: um certificado corrompido/ilegível não deve derrubar o scan dos demais.
            _logger.LogWarning(ex, "[FiscalCertExpiryAlert] Falha ao checar certificado {Source} ({ReferenceId}) da empresa {CompanyId}.",
                source, referenceId, companyId);
        }
    }
}
