using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Contracts.Master.Companies;
using Petshop.Api.Data;
using Petshop.Api.Entities.Fiscal;
using Petshop.Api.Services.Fiscal;

namespace Petshop.Api.Services.Master;

/// <summary>
/// Avalia se um tenant está pronto para operar com um cliente real, substituindo
/// a checklist manual do docs/SAAS_OPERATIONS.md por um cálculo automático.
/// Não é um gate de acesso — é diagnóstico para o Master Admin decidir a ativação.
/// </summary>
public class TenantGoLiveReadinessService
{
    private readonly AppDbContext _db;
    private readonly FiscalCertProtectionService _certSvc;

    public TenantGoLiveReadinessService(AppDbContext db, FiscalCertProtectionService certSvc)
    {
        _db = db;
        _certSvc = certSvc;
    }

    public async Task<GoLiveReadinessDto> EvaluateAsync(Guid companyId, CancellationToken ct = default)
    {
        var checks = new List<ReadinessCheckDto>();

        var fiscalConfig = await _db.FiscalConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.CompanyId == companyId, ct);

        checks.AddRange(EvaluateFiscal(fiscalConfig, _certSvc));

        var hasAdmin = await _db.AdminUsers
            .AsNoTracking()
            .AnyAsync(u => u.CompanyId == companyId && u.IsActive, ct);
        checks.Add(new ReadinessCheckDto("operacao", "admin_user", "Administrador cadastrado",
            hasAdmin ? ReadinessStatus.Ok : ReadinessStatus.Blocked,
            hasAdmin ? null : "Nenhum AdminUser ativo — use o wizard de provisionamento ou crie manualmente."));

        var hasStoreFront = await _db.StoreFrontConfigs
            .AsNoTracking()
            .AnyAsync(s => s.CompanyId == companyId, ct);
        checks.Add(new ReadinessCheckDto("operacao", "storefront", "Branding configurado",
            hasStoreFront ? ReadinessStatus.Ok : ReadinessStatus.Warning,
            hasStoreFront ? null : "StoreFrontConfig ausente — loja vai usar o branding padrão do vendApps."));

        var hasSettings = await _db.CompanySettings
            .AsNoTracking()
            .AnyAsync(s => s.CompanyId == companyId, ct);
        checks.Add(new ReadinessCheckDto("operacao", "settings", "Configurações operacionais",
            hasSettings ? ReadinessStatus.Ok : ReadinessStatus.Blocked,
            hasSettings ? null : "CompanySettings ausente — depósito, raio de entrega e pagamento não configurados."));

        var company = await _db.Companies.AsNoTracking().FirstAsync(c => c.Id == companyId, ct);
        var planOk = company.SuspendedAtUtc is null && !company.IsDeleted && company.IsActive
            && (company.PlanExpiresAtUtc is null || company.PlanExpiresAtUtc > DateTime.UtcNow);
        checks.Add(new ReadinessCheckDto("operacao", "plan", "Plano ativo",
            planOk ? ReadinessStatus.Ok : ReadinessStatus.Blocked,
            planOk ? null : DescribePlanProblem(company)));

        var okCount = checks.Count(c => c.Status == ReadinessStatus.Ok);
        var score = checks.Count == 0 ? 0 : (int)Math.Round(okCount * 100.0 / checks.Count);
        var fiscalReady = checks.Where(c => c.Category == "fiscal").All(c => c.Status != ReadinessStatus.Blocked);

        return new GoLiveReadinessDto(companyId, score, fiscalReady, checks);
    }

    private static List<ReadinessCheckDto> EvaluateFiscal(FiscalConfig? config, FiscalCertProtectionService certSvc)
    {
        var checks = new List<ReadinessCheckDto>();

        if (config is null || !config.IsActive)
        {
            checks.Add(new ReadinessCheckDto("fiscal", "config", "Configuração fiscal",
                ReadinessStatus.Warning,
                "Sem FiscalConfig ativa — tenant não emite NFC-e (ok se o segmento não exigir nota fiscal)."));
            return checks;
        }

        var hasCnpj = !string.IsNullOrWhiteSpace(config.Cnpj) && !string.IsNullOrWhiteSpace(config.Uf);
        checks.Add(new ReadinessCheckDto("fiscal", "cnpj", "CNPJ / UF / IE",
            hasCnpj ? ReadinessStatus.Ok : ReadinessStatus.Blocked,
            hasCnpj ? null : "CNPJ ou UF do estabelecimento não preenchidos."));

        var hasCsc = !string.IsNullOrWhiteSpace(config.CscId) && !string.IsNullOrWhiteSpace(config.CscToken);
        checks.Add(new ReadinessCheckDto("fiscal", "csc", "CSC (QR Code NFC-e)",
            hasCsc ? ReadinessStatus.Ok : ReadinessStatus.Blocked,
            hasCsc ? null : "CscId/CscToken ausentes — QR Code da NFC-e não pode ser gerado."));

        var certCheck = EvaluateCertificate(config, certSvc);
        checks.Add(certCheck);

        var isProd = config.SefazEnvironment == SefazEnvironment.Producao;
        checks.Add(new ReadinessCheckDto("fiscal", "environment", "Ambiente SEFAZ",
            isProd ? ReadinessStatus.Ok : ReadinessStatus.Warning,
            isProd ? null : "SefazEnvironment=Homologação — notas emitidas não têm valor fiscal até trocar para Produção."));

        return checks;
    }

    private static ReadinessCheckDto EvaluateCertificate(FiscalConfig config, FiscalCertProtectionService certSvc)
    {
        var hasCertBytes = !string.IsNullOrWhiteSpace(config.CertificateBase64) || !string.IsNullOrWhiteSpace(config.CertificatePath);
        if (!hasCertBytes)
        {
            var blockedByEnv = config.SefazEnvironment == SefazEnvironment.Producao;
            return new ReadinessCheckDto("fiscal", "certificate", "Certificado digital A1",
                blockedByEnv ? ReadinessStatus.Blocked : ReadinessStatus.Warning,
                blockedByEnv
                    ? "Sem certificado configurado e ambiente já é Produção — a fila fiscal vai falhar (ver FiscalQueueProcessorJob)."
                    : "Sem certificado configurado ainda — necessário antes de trocar para Produção.");
        }

        if (string.IsNullOrWhiteSpace(config.CertificateBase64))
        {
            // Certificado legado por CertificatePath (arquivo em disco) — não dá pra validar aqui sem I/O.
            return new ReadinessCheckDto("fiscal", "certificate", "Certificado digital A1",
                ReadinessStatus.Warning,
                "Certificado configurado via CertificatePath (legado) — não foi possível validar automaticamente.");
        }

        try
        {
            var certBytes = Convert.FromBase64String(config.CertificateBase64);
            var password = certSvc.Unprotect(config.CertificatePassword);
            using var cert = new X509Certificate2(certBytes, password ?? "", X509KeyStorageFlags.EphemeralKeySet);

            var daysToExpire = (cert.NotAfter - DateTime.Now).TotalDays;
            if (daysToExpire < 0)
                return new ReadinessCheckDto("fiscal", "certificate", "Certificado digital A1",
                    ReadinessStatus.Blocked, $"Certificado expirado em {cert.NotAfter:dd/MM/yyyy}.");

            if (daysToExpire < 30)
                return new ReadinessCheckDto("fiscal", "certificate", "Certificado digital A1",
                    ReadinessStatus.Warning, $"Certificado válido, mas vence em {daysToExpire:F0} dias ({cert.NotAfter:dd/MM/yyyy}).");

            return new ReadinessCheckDto("fiscal", "certificate", "Certificado digital A1",
                ReadinessStatus.Ok, $"Válido até {cert.NotAfter:dd/MM/yyyy}.");
        }
        catch (Exception ex)
        {
            return new ReadinessCheckDto("fiscal", "certificate", "Certificado digital A1",
                ReadinessStatus.Blocked, $"Não foi possível abrir o certificado com a senha configurada ({ex.GetType().Name}).");
        }
    }

    private static string DescribePlanProblem(Petshop.Api.Entities.Catalog.Company company)
    {
        if (company.IsDeleted) return "Empresa está marcada como deletada.";
        if (!company.IsActive) return "Empresa está inativa.";
        if (company.SuspendedAtUtc is not null) return $"Empresa suspensa: {company.SuspendedReason ?? "sem motivo registrado"}.";
        if (company.PlanExpiresAtUtc is not null && company.PlanExpiresAtUtc <= DateTime.UtcNow)
            return $"Plano expirou em {company.PlanExpiresAtUtc:dd/MM/yyyy}.";
        return "Plano inativo.";
    }
}
