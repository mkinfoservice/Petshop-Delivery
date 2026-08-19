using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Fiscal;
using Petshop.Api.Entities.Pdv;
using Petshop.Api.Messaging.Contracts;
using Petshop.Api.Services.Fiscal;
using Petshop.Api.Services.Fiscal.Jobs;
using Petshop.Api.Services.WhatsApp;
using System.Security.Claims;

namespace Petshop.Api.Controllers;

/// <summary>
/// Endpoints de administração fiscal: configuração, status SEFAZ, DANFE e cancelamento.
/// </summary>
[ApiController]
[Route("admin/fiscal")]
[Authorize(Roles = "admin,gerente")]
public class FiscalAdminController : ControllerBase
{
    private readonly AppDbContext                _db;
    private readonly SefazHttpClient             _sefaz;
    private readonly IBackgroundJobClient        _jobs;
    private readonly IPublishEndpoint            _publisher;
    private readonly FiscalCertProtectionService _certSvc;
    private readonly RealFiscalEngine            _realEngine;

    public FiscalAdminController(
        AppDbContext db,
        SefazHttpClient sefaz,
        IBackgroundJobClient jobs,
        IPublishEndpoint publisher,
        FiscalCertProtectionService certSvc,
        RealFiscalEngine realEngine)
    {
        _db          = db;
        _sefaz       = sefaz;
        _jobs        = jobs;
        _publisher   = publisher;
        _certSvc     = certSvc;
        _realEngine  = realEngine;
    }

    private Guid CompanyId => Guid.Parse(User.FindFirstValue("companyId")!);

    // ── Config ────────────────────────────────────────────────────────────────

    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken ct)
    {
        var cfg = await _db.FiscalConfigs
            .FirstOrDefaultAsync(f => f.CompanyId == CompanyId, ct);

        if (cfg == null)
            return Ok(new FiscalConfigDto());

        return Ok(MapToDto(cfg));
    }

    [HttpPut("config")]
    public async Task<IActionResult> SaveConfig([FromBody] FiscalConfigDto dto, CancellationToken ct)
    {
        var cfg = await _db.FiscalConfigs
            .FirstOrDefaultAsync(f => f.CompanyId == CompanyId, ct);

        if (cfg == null)
        {
            cfg = new FiscalConfig { CompanyId = CompanyId };
            _db.FiscalConfigs.Add(cfg);
        }

        cfg.Cnpj               = dto.Cnpj?.Replace(".", "").Replace("/", "").Replace("-", "") ?? "";
        cfg.InscricaoEstadual  = dto.InscricaoEstadual ?? "";
        cfg.Uf                 = dto.Uf?.ToUpperInvariant() ?? "";
        cfg.RazaoSocial        = dto.RazaoSocial ?? "";
        cfg.NomeFantasia       = dto.NomeFantasia;
        cfg.Logradouro         = dto.Logradouro ?? "";
        cfg.NumeroEndereco     = dto.NumeroEndereco ?? "";
        cfg.Complemento        = dto.Complemento;
        cfg.Bairro             = dto.Bairro ?? "";
        cfg.CodigoMunicipio    = dto.CodigoMunicipio;
        cfg.NomeMunicipio      = dto.NomeMunicipio ?? "";
        cfg.Cep                = dto.Cep?.Replace("-", "") ?? "";
        cfg.Telefone           = dto.Telefone;
        cfg.TaxRegime          = Enum.Parse<TaxRegime>(dto.TaxRegime ?? "SimplesNacional");
        cfg.SefazEnvironment   = Enum.Parse<SefazEnvironment>(dto.SefazEnvironment ?? "Homologacao");
        // Encrypt cert only if new value provided; keep existing encrypted value otherwise
        if (!string.IsNullOrWhiteSpace(dto.CertificateBase64))
            cfg.CertificateBase64 = _certSvc.Protect(dto.CertificateBase64);
        if (!string.IsNullOrWhiteSpace(dto.CertificatePassword))
            cfg.CertificatePassword = _certSvc.Protect(dto.CertificatePassword);
        cfg.CertificatePath     = dto.CertificatePath; // legado
        cfg.CscId              = dto.CscId;
        cfg.CscToken           = dto.CscToken;
        cfg.NfceSerie          = dto.NfceSerie;
        cfg.DefaultCfop        = dto.DefaultCfop ?? "5102";
        cfg.IsActive           = true;
        cfg.UpdatedAtUtc       = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return Ok(MapToDto(cfg));
    }

    // ── SEFAZ Status ──────────────────────────────────────────────────────────

    [HttpGet("status")]
    public async Task<IActionResult> SefazStatus(CancellationToken ct)
    {
        var cfg = await _db.FiscalConfigs
            .FirstOrDefaultAsync(f => f.CompanyId == CompanyId && f.IsActive, ct);

        if (cfg == null)
            return BadRequest(new { error = "FiscalConfig não configurado." });

        var online = await _sefaz.IsOnlineAsync(cfg.Uf, cfg.SefazEnvironment, ct);
        return Ok(new { online, uf = cfg.Uf, env = cfg.SefazEnvironment.ToString(), checkedAtUtc = DateTime.UtcNow });
    }

    // ── DANFE NFC-e ───────────────────────────────────────────────────────────

    [HttpGet("sale/{saleId:guid}/danfe")]
    public async Task<IActionResult> GetDanfe(Guid saleId, CancellationToken ct)
    {
        var sale = await _db.SaleOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == saleId && o.CompanyId == CompanyId, ct);

        if (sale == null) return NotFound();

        var fiscalDoc = sale.FiscalDocumentId.HasValue
            ? await _db.FiscalDocuments.FindAsync([sale.FiscalDocumentId.Value], ct)
            : null;

        // Prefere config do caixa; fallback para config empresa (legado)
        CashRegisterFiscalConfig? regCfg = null;
        if (sale.CashRegisterId != Guid.Empty)
            regCfg = await _db.CashRegisterFiscalConfigs
                .FirstOrDefaultAsync(f => f.CashRegisterId == sale.CashRegisterId, ct);

        FiscalConfig? compCfg = null;
        if (regCfg == null)
            compCfg = await _db.FiscalConfigs.FirstOrDefaultAsync(f => f.CompanyId == CompanyId, ct);

        var html = BuildDanfeHtml(sale, fiscalDoc, regCfg, compCfg);
        return Content(html, "text/html; charset=utf-8");
    }

    // ── Cancelamento NFC-e ────────────────────────────────────────────────────

    /// <summary>
    /// Cancela uma NFC-e autorizada via evento SEFAZ (tpEvento 110111).
    /// A janela legal de cancelamento (normalmente algumas horas após a autorização,
    /// varia por UF) é validada pela própria SEFAZ — o erro retornado por ela é repassado.
    /// Não reverte estoque nem lançamentos financeiros: isso é uma decisão operacional
    /// separada, a ser feita pelos fluxos de estoque/financeiro já existentes.
    /// </summary>
    [HttpPost("sale/{saleId:guid}/cancel")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> CancelNfce(
        Guid saleId,
        [FromBody] CancelNfceRequest req,
        CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Reason) || req.Reason.Trim().Length < 15)
            return BadRequest(new { error = "Reason é obrigatório e deve ter ao menos 15 caracteres (exigência da SEFAZ)." });

        var sale = await _db.SaleOrders
            .FirstOrDefaultAsync(o => o.Id == saleId && o.CompanyId == CompanyId, ct);
        if (sale is null) return NotFound("Venda não encontrada.");

        if (!sale.FiscalDocumentId.HasValue)
            return BadRequest(new { error = "Venda não possui documento fiscal." });

        var fiscalDoc = await _db.FiscalDocuments
            .FirstOrDefaultAsync(d => d.Id == sale.FiscalDocumentId.Value && d.CompanyId == CompanyId, ct);
        if (fiscalDoc is null) return NotFound("Documento fiscal não encontrado.");

        if (fiscalDoc.FiscalStatus == FiscalDocumentStatus.Cancelled)
            return Conflict(new { error = "NFC-e já está cancelada." });

        if (fiscalDoc.FiscalStatus != FiscalDocumentStatus.Authorized)
            return BadRequest(new { error = $"Só é possível cancelar NFC-e autorizada. Status atual: {fiscalDoc.FiscalStatus}." });

        if (string.IsNullOrWhiteSpace(fiscalDoc.AccessKey) || string.IsNullOrWhiteSpace(fiscalDoc.AuthorizationCode))
            return BadRequest(new { error = "Documento fiscal sem chave de acesso ou protocolo de autorização." });

        // Resolve config fiscal + certificado: caixa primeiro, fallback empresa (mesmo padrão do FiscalQueueProcessorJob).
        CashRegisterFiscalConfig? regCfg = null;
        if (sale.CashRegisterId != Guid.Empty)
            regCfg = await _db.CashRegisterFiscalConfigs
                .FirstOrDefaultAsync(c => c.CashRegisterId == sale.CashRegisterId && c.IsActive, ct);

        FiscalConfig? compCfg = null;
        if (regCfg is null)
            compCfg = await _db.FiscalConfigs.FirstOrDefaultAsync(f => f.CompanyId == CompanyId && f.IsActive, ct);

        var certBase64Raw = regCfg?.CertificateBase64 ?? compCfg?.CertificateBase64;
        var certPassRaw   = regCfg?.CertificatePassword ?? compCfg?.CertificatePassword;
        var cnpj          = regCfg?.Cnpj ?? compCfg?.Cnpj;
        var uf             = regCfg?.Uf ?? compCfg?.Uf;
        var sefazEnv      = regCfg?.SefazEnvironment ?? compCfg?.SefazEnvironment ?? SefazEnvironment.Homologacao;

        var certBase64 = _certSvc.Unprotect(certBase64Raw);
        var certPassword = _certSvc.Unprotect(certPassRaw);

        if (string.IsNullOrWhiteSpace(certBase64) || string.IsNullOrWhiteSpace(cnpj) || string.IsNullOrWhiteSpace(uf))
            return BadRequest(new { error = "Certificado, CNPJ ou UF não configurados para esta empresa/caixa." });

        var certBytes = Convert.FromBase64String(certBase64);
        var cancelReq = new FiscalCancelRequest(
            AccessKey: fiscalDoc.AccessKey,
            AuthorizationProtocol: fiscalDoc.AuthorizationCode,
            Reason: req.Reason.Trim(),
            Cnpj: cnpj,
            Uf: uf,
            SefazEnvironment: sefazEnv);

        var result = await _realEngine.CancelWithCertAsync(cancelReq, certBytes, certPassword, ct);

        _db.FiscalAuditLogs.Add(new FiscalAuditLog
        {
            CompanyId  = CompanyId,
            EntityType = "FiscalDocument",
            EntityId   = fiscalDoc.Id,
            Action     = result.Success ? "Cancelled" : "CancelRejected",
            NewStatus  = result.Success ? FiscalDocumentStatus.Cancelled.ToString() : fiscalDoc.FiscalStatus.ToString(),
            ActorType  = "Admin",
            Details    = result.Success
                ? $"{{\"reason\":\"{req.Reason.Trim()}\",\"protocol\":\"{result.Protocol}\"}}"
                : $"{{\"code\":\"{result.ErrorCode}\",\"msg\":\"{result.ErrorMessage}\"}}",
        });

        if (!result.Success)
        {
            await _db.SaveChangesAsync(ct);
            return BadRequest(new { error = $"SEFAZ rejeitou o cancelamento: [{result.ErrorCode}] {result.ErrorMessage}" });
        }

        fiscalDoc.FiscalStatus   = FiscalDocumentStatus.Cancelled;
        fiscalDoc.CancelReason   = req.Reason.Trim();
        fiscalDoc.CancelProtocol = result.Protocol;
        fiscalDoc.CancelledAtUtc = DateTime.UtcNow;
        fiscalDoc.UpdatedAtUtc   = DateTime.UtcNow;

        // Reverte o lançamento financeiro automático da venda — nota cancelada não é
        // mais receita real. O cancelamento em si já fica auditado via FiscalAuditLogs.
        var linkedEntry = await _db.FinancialEntries.FirstOrDefaultAsync(e =>
            e.CompanyId == CompanyId && e.ReferenceType == "SaleOrder" && e.ReferenceId == sale.Id, ct);
        if (linkedEntry is not null)
            _db.FinancialEntries.Remove(linkedEntry);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            fiscalDocumentId = fiscalDoc.Id,
            status = fiscalDoc.FiscalStatus.ToString(),
            cancelProtocol = fiscalDoc.CancelProtocol,
            cancelledAtUtc = fiscalDoc.CancelledAtUtc,
        });
    }

    private static string BuildDanfeHtml(
        Entities.Pdv.SaleOrder sale,
        FiscalDocument? fiscalDoc,
        CashRegisterFiscalConfig? regCfg,
        FiscalConfig? compCfg)
    {
        // Resolve dados do emitente da fonte disponível
        var razaoSocial   = regCfg?.RazaoSocial   ?? compCfg?.RazaoSocial   ?? "—";
        var nomeFantasia  = regCfg?.NomeFantasia   ?? compCfg?.NomeFantasia;
        var cnpj          = regCfg?.Cnpj           ?? compCfg?.Cnpj          ?? "";
        var ie            = regCfg?.InscricaoEstadual ?? compCfg?.InscricaoEstadual ?? "";
        var logradouro    = regCfg?.Logradouro     ?? compCfg?.Logradouro    ?? "";
        var numero        = regCfg?.NumeroEndereco ?? compCfg?.NumeroEndereco ?? "";
        var complemento   = regCfg?.Complemento   ?? compCfg?.Complemento;
        var bairro        = regCfg?.Bairro         ?? compCfg?.Bairro        ?? "";
        var municipio     = regCfg?.NomeMunicipio  ?? compCfg?.NomeMunicipio ?? "";
        var uf            = regCfg?.Uf             ?? compCfg?.Uf            ?? "";
        var cep           = regCfg?.Cep            ?? compCfg?.Cep           ?? "";
        var telefone      = regCfg?.Telefone       ?? compCfg?.Telefone;
        var cscId         = regCfg?.CscId           ?? compCfg?.CscId;
        var cscToken      = regCfg?.CscToken         ?? compCfg?.CscToken;
        var sefazEnv      = regCfg?.SefazEnvironment ?? compCfg?.SefazEnvironment ?? SefazEnvironment.Homologacao;

        static string Brl(int cents) => $"R$ {cents / 100m:F2}".Replace(".", ",");
        static string Enc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "");

        // Formata CNPJ: 00.000.000/0001-00
        var cnpjFmt = cnpj.Length == 14
            ? $"{cnpj[..2]}.{cnpj[2..5]}.{cnpj[5..8]}/{cnpj[8..12]}-{cnpj[12..]}"
            : cnpj;

        // Formata CEP: 00000-000
        var cepFmt = cep.Length == 8 ? $"{cep[..5]}-{cep[5..]}" : cep;

        var enderecoLine1 = $"{logradouro}, {numero}{(string.IsNullOrWhiteSpace(complemento) ? "" : " - " + complemento)}";
        var enderecoLine2 = $"{bairro} - {municipio}/{uf} - CEP {cepFmt}";

        var authorized = fiscalDoc?.FiscalStatus == FiscalDocumentStatus.Authorized;
        var chave      = fiscalDoc?.AccessKey ?? "";
        var chaveFormatted = chave.Length == 44
            ? string.Concat(Enumerable.Range(0, 11).Select(i => chave.Substring(i * 4, 4) + " ")).Trim()
            : chave;

        var nNF = fiscalDoc != null
            ? $"NFC-e Nº {fiscalDoc.Number:D9}  Série {fiscalDoc.Serie:D3}"
            : "NFC-e (não autorizada)";

        var dataEmissao = sale.CompletedAtUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") ?? "—";

        // Itens
        var itemRows = string.Join("", sale.Items.Select((i, idx) =>
        {
            var qtdStr  = i.IsSoldByWeight ? $"{i.WeightKg:F3} kg" : $"{i.Qty:F0} UN";
            var unitStr = Brl(i.UnitPriceCentsSnapshot);
            return $@"<tr>
  <td colspan='3' class='item-desc'>{idx + 1:D2} {Enc(i.ProductNameSnapshot)}</td>
</tr>
<tr>
  <td class='item-qty'>{Enc(qtdStr)}</td>
  <td class='item-unit'>x {unitStr}</td>
  <td class='item-total'>{Brl(i.TotalCents)}</td>
</tr>";
        }));

        // Pagamentos
        var payRows = string.Join("", sale.Payments.Select(p =>
        {
            var label = p.PaymentMethod.ToUpper() switch
            {
                "PIX"            => "PIX",
                "DINHEIRO"       => "Dinheiro",
                "CARTAO_CREDITO" => "Cartão Crédito",
                "CARTAO_DEBITO"  => "Cartão Débito",
                _                => p.PaymentMethod
            };
            var changeRow = p.ChangeCents > 0
                ? $"<tr><td>Troco</td><td class='right'>{Brl(p.ChangeCents)}</td></tr>"
                : "";
            return $"<tr><td>{label}</td><td class='right'>{Brl(p.AmountCents)}</td></tr>{changeRow}";
        }));

        var tpAmb     = sefazEnv == SefazEnvironment.Producao ? "1" : "2";
        var qrBaseUrl = SefazEndpoints.GetQrCodeBaseUrl(uf.Length == 2 ? uf : "SP", sefazEnv);
        var qrContent = authorized && chave.Length == 44 && !string.IsNullOrWhiteSpace(cscId) && !string.IsNullOrWhiteSpace(cscToken)
            ? $"{qrBaseUrl}?p={chave}|{tpAmb}|{GenerateQrHash(chave, cscId, cscToken)}|{cscId}"
            : "";

        // Protocolo
        var protocoloLine = authorized
            ? $"<p>Protocolo: {fiscalDoc!.AuthorizationCode}  {fiscalDoc.AuthorizationDateTimeUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")}</p>"
            : $"<p class='contingencia'>⚠ {(sale.FiscalDecision == "PermanentContingency" ? "Venda em contingência — NFC-e não emitida" : "Aguardando autorização SEFAZ")}</p>";

        // Texto PROCON obrigatório RJ
        const string proconText = "SAC e PROCON: O consumidor pode exigir o Documento Fiscal. " +
            "Guarde este cupom. Acesse www.procon.rj.gov.br ou ligue 151.";

        return $@"<!DOCTYPE html>
<html lang='pt-BR'>
<head>
<meta charset='utf-8'/>
<meta name='viewport' content='width=device-width'/>
<title>DANFE NFC-e</title>
<style>
*{{margin:0;padding:0;box-sizing:border-box;}}
body{{font-family:'Courier New',monospace;font-size:10px;width:80mm;max-width:80mm;padding:3mm;color:#000;background:#fff;}}
h1{{text-align:center;font-size:13px;font-weight:bold;margin:2px 0;}}
h2{{text-align:center;font-size:11px;font-weight:bold;margin:1px 0;}}
.center{{text-align:center;}}
.right{{text-align:right;}}
p{{font-size:9px;margin:1px 0;}}
hr{{border:none;border-top:1px dashed #555;margin:4px 0;}}
table{{width:100%;border-collapse:collapse;}}
td{{padding:1px 0;font-size:10px;vertical-align:top;}}
.item-desc{{font-size:10px;padding-top:3px;}}
.item-qty{{width:25%;color:#333;}}
.item-unit{{width:30%;color:#333;}}
.item-total{{width:45%;text-align:right;font-weight:bold;}}
.total-row td{{font-size:12px;font-weight:bold;padding:3px 0;}}
.subtotal td{{font-size:10px;}}
.chave{{font-size:7.5px;word-break:break-all;text-align:center;letter-spacing:0.5px;margin:2px 0;}}
.procon{{font-size:8px;text-align:center;color:#444;margin:3px 0;border:1px solid #ccc;padding:3px;border-radius:2px;}}
.contingencia{{color:#c00;text-align:center;font-weight:bold;font-size:9px;}}
.danfe-title{{text-align:center;font-size:9px;color:#555;margin:2px 0;}}
#qr{{display:flex;justify-content:center;margin:4px 0;}}
@media print{{@page{{margin:0;size:80mm auto;}}body{{padding:2mm;}}}}
</style>
</head>
<body>

<!-- Cabeçalho emitente -->
<h1>{Enc(razaoSocial)}</h1>
{(string.IsNullOrWhiteSpace(nomeFantasia) ? "" : $"<h2>{Enc(nomeFantasia)}</h2>")}
<p class='center'>{Enc(enderecoLine1)}</p>
<p class='center'>{Enc(enderecoLine2)}</p>
{(string.IsNullOrWhiteSpace(telefone) ? "" : $"<p class='center'>Tel: {Enc(telefone)}</p>")}
<p class='center'>CNPJ: {Enc(cnpjFmt)}{(string.IsNullOrWhiteSpace(ie) ? "" : $"  IE: {Enc(ie)}")}</p>

<hr/>
<p class='danfe-title'>DOCUMENTO AUXILIAR DA NOTA FISCAL DE CONSUMIDOR ELETRÔNICA</p>
<p class='danfe-title'>{Enc(nNF)}</p>
<p class='center'>Emissão: {dataEmissao}</p>
<hr/>

<!-- Itens -->
<table>
<thead>
<tr><th colspan='3' style='text-align:left;font-size:9px;color:#555;padding-bottom:2px;'>ITENS</th></tr>
</thead>
<tbody>{itemRows}</tbody>
</table>
<hr/>

<!-- Totais -->
<table class='subtotal'>
<tr><td>Subtotal</td><td class='right'>{Brl(sale.SubtotalCents)}</td></tr>
{(sale.DiscountCents > 0 ? $"<tr><td>Desconto</td><td class='right'>-{Brl(sale.DiscountCents)}</td></tr>" : "")}
</table>
<table>
<tr class='total-row'><td>TOTAL</td><td class='right'>{Brl(sale.TotalCents)}</td></tr>
</table>
<hr/>

<!-- Formas de pagamento -->
<p style='font-size:9px;color:#555;margin-bottom:2px;'>PAGAMENTO</p>
<table>{payRows}</table>
<hr/>

<!-- Consumidor -->
<p class='center' style='font-size:9px;'>CONSUMIDOR {(string.IsNullOrWhiteSpace(sale.CustomerName) ? "NÃO IDENTIFICADO" : Enc(sale.CustomerName))}</p>
<hr/>

<!-- Protocolo / Status fiscal -->
{protocoloLine}

{(authorized && qrContent.Length > 0 ? $@"<hr/>
<!-- QR Code -->
<p class='center' style='font-size:8px;margin-bottom:2px;'>Consulte a NFC-e pela chave ou QR Code</p>
<div id='qr'></div>
<p class='chave'>{Enc(chaveFormatted)}</p>
<p class='center' style='font-size:8px;'>Consulte em {SefazEndpoints.GetConsultaChaveUrl(uf.Length == 2 ? uf : "SP", sefazEnv, "").Split('?')[0]}</p>" : "")}

<hr/>
<!-- PROCON -->
<div class='procon'>{proconText}</div>
<hr/>
<p class='center' style='margin-top:4px;font-size:10px;'>Obrigado pela preferência!</p>

{(authorized && qrContent.Length > 0 ? $@"<script src='https://cdn.jsdelivr.net/npm/qrcodejs@1.0.0/qrcode.min.js'></script>
<script>
new QRCode(document.getElementById('qr'),{{
  text: {System.Text.Json.JsonSerializer.Serialize(qrContent)},
  width:120,height:120,correctLevel:QRCode.CorrectLevel.M
}});
window.onload=function(){{setTimeout(function(){{window.print();}},600);}};
</script>" : "<script>window.onload=function(){setTimeout(function(){window.print();},300);};</script>")}
</body>
</html>";
    }

    // ── Documentos fiscais da empresa ─────────────────────────────────────────

    [HttpGet("documents")]
    public async Task<IActionResult> ListDocuments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? contingency = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;

        var q = _db.FiscalDocuments
            .AsNoTracking()
            .Where(d => d.CompanyId == CompanyId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FiscalDocumentStatus>(status, out var st))
            q = q.Where(d => d.FiscalStatus == st);

        if (!string.IsNullOrWhiteSpace(contingency) && Enum.TryParse<ContingencyType>(contingency, out var ct2))
            q = q.Where(d => d.ContingencyType == ct2);

        if (from.HasValue) q = q.Where(d => d.CreatedAtUtc >= from.Value);
        if (to.HasValue)   q = q.Where(d => d.CreatedAtUtc <= to.Value);

        var total = await q.CountAsync(ct);

        // Buscar documentos com JOIN manual para SaleOrder
        var rawDocs = await q
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.Number,
                d.Serie,
                d.AccessKey,
                FiscalStatus     = d.FiscalStatus.ToString(),
                ContingencyType  = d.ContingencyType.ToString(),
                IsContingency    = d.ContingencyType != ContingencyType.None,
                d.SaleOrderId,
                d.RejectCode,
                d.RejectMessage,
                d.TransmissionAttempts,
                d.AuthorizationDateTimeUtc,
                d.LastAttemptAtUtc,
                d.CreatedAtUtc,
                d.UpdatedAtUtc,
                d.CancelReason,
                d.CancelProtocol,
                d.CancelledAtUtc,
                HasXml           = d.XmlContent != null,
            })
            .ToListAsync(ct);

        // Enriquecer com PublicId da SaleOrder
        var saleIds = rawDocs.Where(d => d.SaleOrderId.HasValue)
                             .Select(d => d.SaleOrderId!.Value)
                             .Distinct().ToList();

        Dictionary<Guid, string?> salePublicIds = new();
        if (saleIds.Count > 0)
        {
            var sales = await _db.SaleOrders
                .AsNoTracking()
                .Where(s => saleIds.Contains(s.Id))
                .Select(s => new { s.Id, s.PublicId, s.CustomerName, s.TotalCents })
                .ToListAsync(ct);

            foreach (var s in sales)
                salePublicIds[s.Id] = s.PublicId;
        }

        var items = rawDocs.Select(d => new
        {
            d.Id,
            d.Number,
            d.Serie,
            d.AccessKey,
            d.FiscalStatus,
            d.ContingencyType,
            d.IsContingency,
            d.SaleOrderId,
            SalePublicId     = d.SaleOrderId.HasValue && salePublicIds.TryGetValue(d.SaleOrderId.Value, out var pid) ? pid : null,
            d.RejectCode,
            d.RejectMessage,
            d.TransmissionAttempts,
            d.AuthorizationDateTimeUtc,
            d.LastAttemptAtUtc,
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            d.CancelReason,
            d.CancelProtocol,
            d.CancelledAtUtc,
            d.HasXml,
        }).ToList();

        return Ok(new { total, page, pageSize, items });
    }

    // ── Debug / Testes ────────────────────────────────────────────────────────

    /// <summary>
    /// Enfileira o WhatsApp de comprovante para uma venda já existente (mock ou real).
    /// Útil para testar PDF + upload + envio sem precisar passar pelo fluxo fiscal completo.
    /// Apenas admin — não expor em produção para usuários finais.
    /// </summary>
    [HttpPost("debug/sale/{saleId:guid}/notify-whatsapp")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DebugNotifyWhatsApp(Guid saleId, CancellationToken ct)
    {
        var exists = await _db.SaleOrders
            .AnyAsync(s => s.Id == saleId && s.CompanyId == CompanyId, ct);

        if (!exists)
            return NotFound("Venda não encontrada.");

        try
        {
            await _publisher.Publish(new PdvWhatsAppNotificationRequestedEvent
            {
                SaleId        = saleId,
                CompanyId     = CompanyId,
                TriggerStatus = "SALE_COMPLETED",
                OccurredAtUtc = DateTime.UtcNow,
            }, ct);
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { message = "Falha ao publicar evento WhatsApp.", detail = ex.Message });
        }

        return Ok(new { message = "Evento SALE_COMPLETED publicado via MassTransit." });
    }

    /// <summary>
    /// Reprocessa a fila fiscal da empresa manualmente (dispara o FiscalQueueProcessorJob).
    /// Útil para testar o fluxo completo: fiscal → WhatsApp em sequência.
    /// </summary>
    [HttpPost("debug/process-queue")]
    [Authorize(Roles = "admin")]
    public IActionResult DebugProcessFiscalQueue()
    {
        var jobId = _jobs.Enqueue<FiscalQueueProcessorJob>(
            j => j.ProcessAsync(CompanyId, CancellationToken.None));

        return Ok(new { jobId, message = "Job fiscal enfileirado. Acompanhe em /hangfire." });
    }

    /// <summary>
    /// Retorna as últimas 10 entradas da fila fiscal com FailureReason (erro real do job).
    /// </summary>
    [HttpGet("debug/queue")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DebugQueueStatus(CancellationToken ct)
    {
        var items = await _db.FiscalQueues
            .Where(q => q.CompanyId == CompanyId)
            .OrderByDescending(q => q.CreatedAtUtc)
            .Take(10)
            .Select(q => new {
                q.Id,
                Status = q.Status.ToString(),
                q.RetryCount,
                q.FailureReason,
                q.SaleOrderId,
                q.FiscalDocumentId,
                q.CreatedAtUtc,
                q.ProcessedAtUtc
            })
            .ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Reseta itens Processing/Failed de volta para Waiting para reprocessamento.
    /// Útil quando o job falhou e deixou itens travados em Processing.
    /// </summary>
    [HttpPost("debug/reset-queue")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DebugResetQueue([FromQuery] bool force = false, CancellationToken ct = default)
    {
        var companyId = CompanyId;
        // Raw SQL para evitar problemas de conversão de enum EF
        var sql = force
            ? $"""
               UPDATE "FiscalQueues"
               SET "Status" = 'Waiting', "RetryCount" = 0
               WHERE "CompanyId" = '{companyId}'
                 AND "Status" IN ('Failed','Processing')
               """
            : $"""
               UPDATE "FiscalQueues"
               SET "Status" = 'Waiting', "RetryCount" = 0
               WHERE "CompanyId" = '{companyId}'
                 AND "Status" IN ('Failed','Processing')
                 AND "RetryCount" < 5
               """;

        var affected = await _db.Database.ExecuteSqlRawAsync(sql, ct);
        return Ok(new { reset = affected, message = $"{affected} item(ns) resetado(s) para Waiting." });
    }

    /// <summary>
    /// Remove documentos fiscais em contingência, mantendo os N mais recentes.
    /// Também apaga as entradas da FiscalQueue correspondentes.
    /// Uso: ambiente de homologação / limpeza de testes.
    /// </summary>
    [HttpDelete("debug/cleanup-contingency")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DebugCleanupContingency(
        [FromQuery] int keep = 50, CancellationToken ct = default)
    {
        if (keep < 0) keep = 0;
        var companyId = CompanyId;

        // IDs dos N mais recentes em contingência (para preservar)
        var keepIds = await _db.FiscalDocuments
            .Where(d => d.CompanyId == companyId && d.FiscalStatus == FiscalDocumentStatus.Contingency)
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(keep)
            .Select(d => d.Id)
            .ToListAsync(ct);

        // Documentos a apagar
        var toDelete = await _db.FiscalDocuments
            .Where(d => d.CompanyId == companyId
                     && d.FiscalStatus == FiscalDocumentStatus.Contingency
                     && !keepIds.Contains(d.Id))
            .ToListAsync(ct);

        var deletedDocs = toDelete.Count;
        if (deletedDocs > 0)
        {
            // Remove entradas da fila vinculadas
            var saleIds = toDelete.Select(d => d.SaleOrderId).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            if (saleIds.Count > 0)
            {
                var queueEntries = await _db.FiscalQueues
                    .Where(q => q.CompanyId == companyId && saleIds.Contains(q.SaleOrderId))
                    .ToListAsync(ct);
                _db.FiscalQueues.RemoveRange(queueEntries);
            }

            _db.FiscalDocuments.RemoveRange(toDelete);
            await _db.SaveChangesAsync(ct);
        }

        // Apaga também entradas orfãs da fila (sem FiscalDocument vinculado) em Failed/Waiting
        var orphanSql = $"""
            DELETE FROM "FiscalQueues"
            WHERE "CompanyId" = '{companyId}'
              AND "Status" IN ('Failed','Waiting','Processing')
              AND "FiscalDocumentId" IS NULL
            """;
        var deletedQueue = await _db.Database.ExecuteSqlRawAsync(orphanSql, ct);

        return Ok(new
        {
            deletedDocuments = deletedDocs,
            deletedQueueOrphans = deletedQueue,
            kept = keepIds.Count,
            message = $"Removidos {deletedDocs} documento(s) em contingência. Mantidos {keepIds.Count}.",
        });
    }

    /// <summary>
    /// Testa se o certificado salvo na FiscalConfig (empresa) e nos CashRegisterFiscalConfigs
    /// consegue ser carregado com a senha salva. Útil para diagnosticar falhas de cert.
    /// </summary>
    [HttpGet("debug/validate-cert")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DebugValidateCert(CancellationToken ct)
    {
        var companyCfg = await _db.FiscalConfigs
            .FirstOrDefaultAsync(f => f.CompanyId == CompanyId && f.IsActive, ct);

        var registerCfgs = await _db.CashRegisterFiscalConfigs
            .Where(r => r.CashRegister.CompanyId == CompanyId && r.IsActive)
            .ToListAsync(ct);

        var results = new List<object>();

        if (companyCfg != null)
            results.Add(new { source = "FiscalConfig (empresa)", result = TestCert(companyCfg.CertificateBase64, companyCfg.CertificatePassword) });

        foreach (var r in registerCfgs)
            results.Add(new { source = $"CashRegisterFiscalConfig ({r.CashRegisterId})", result = TestCert(r.CertificateBase64, r.CertificatePassword) });

        return Ok(results);
    }

    private object TestCert(string? certBase64Raw, string? certPasswordRaw)
    {
        var certBase64   = _certSvc.Unprotect(certBase64Raw);
        var certPassword = _certSvc.Unprotect(certPasswordRaw);

        if (string.IsNullOrWhiteSpace(certBase64))
            return new { ok = false, error = "certBase64 null/vazio após Unprotect. Reenvie o certificado (possível rotação de chave DP).", passwordSet = !string.IsNullOrWhiteSpace(certPassword) };

        byte[] certBytes;
        try { certBytes = Convert.FromBase64String(certBase64); }
        catch { return new { ok = false, error = "certBase64 não é base64 válido.", passwordSet = !string.IsNullOrWhiteSpace(certPassword) }; }

        try
        {
            using var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                certBytes, certPassword,
                System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet);
            return new { ok = true, subject = cert.Subject, validFrom = cert.NotBefore, validTo = cert.NotAfter, hasPrivKey = cert.HasPrivateKey, passwordSet = !string.IsNullOrWhiteSpace(certPassword) };
        }
        catch (Exception ex)
        {
            return new { ok = false, error = ex.Message, passwordSet = !string.IsNullOrWhiteSpace(certPassword), certBytesLen = certBytes.Length, hint = "Se passwordSet=false e o PFX exige senha, reenvie o certificado com a senha correta via FiscalConfig." };
        }
    }

    /// <summary>
    /// Gera o XML NFC-e não-assinado da venda mais recente na fila sem enviar à SEFAZ.
    /// Útil para inspecionar o XML e validar contra o schema NFC-e 4.00.
    /// </summary>
    [HttpGet("debug/generate-xml")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DebugGenerateXml(CancellationToken ct)
    {
        var item = await _db.FiscalQueues
            .Where(q => q.CompanyId == CompanyId)
            .OrderByDescending(q => q.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (item == null) return NotFound("Nenhum item na fila fiscal.");

        var sale = await _db.SaleOrders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == item.SaleOrderId, ct);

        if (sale == null) return NotFound("Venda não encontrada.");

        CashRegisterFiscalConfig? registerConfig = null;
        if (sale.CashRegisterId != Guid.Empty)
            registerConfig = await _db.CashRegisterFiscalConfigs
                .FirstOrDefaultAsync(c => c.CashRegisterId == sale.CashRegisterId && c.IsActive, ct);

        var fallbackConfig = await _db.FiscalConfigs
            .FirstOrDefaultAsync(f => f.CompanyId == CompanyId && f.IsActive, ct);

        EmitterData emitter;
        short nfceSerie;
        if (registerConfig != null) { emitter = DebugBuildEmitter(registerConfig); nfceSerie = registerConfig.NfceSerie; }
        else if (fallbackConfig != null) { emitter = DebugBuildEmitter(fallbackConfig); nfceSerie = fallbackConfig.NfceSerie; }
        else return BadRequest("Nenhuma FiscalConfig ativa.");

        var productIds = sale.Items.Select(i => i.ProductId).Distinct().ToList();
        var products   = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var defaultCfop = registerConfig?.DefaultCfop ?? fallbackConfig?.DefaultCfop ?? "5102";

        var fiscalItems = sale.Items.Select((i, idx) =>
        {
            var p = products.GetValueOrDefault(i.ProductId);
            return new FiscalItemData(
                idx + 1,
                p?.InternalCode ?? i.ProductId.ToString("N")[..5],
                p?.Barcode ?? i.ProductBarcodeSnapshot ?? "",
                i.ProductNameSnapshot,
                p?.Ncm ?? "00000000",
                defaultCfop,
                i.IsSoldByWeight ? "KG" : (p?.Unit ?? "UN"),
                i.IsSoldByWeight ? (decimal)(i.WeightKg ?? 0) : (decimal)i.Qty,
                (decimal)i.UnitPriceCentsSnapshot,
                (decimal)i.TotalCents,
                i.IsSoldByWeight);
        }).ToList();

        var fiscalPayments = sale.Payments.Select(p =>
            new FiscalPaymentData(p.PaymentMethod, p.AmountCents, p.ChangeCents)).ToList();

        var req = new FiscalDocumentRequest
        {
            CompanyId        = CompanyId,
            SaleOrderId      = sale.Id,
            FiscalDocumentId = Guid.Empty,
            DocumentType     = FiscalDocumentType.NFCe,
            Serie            = nfceSerie,
            Number           = 999999,
            SaleDateTimeUtc  = sale.CompletedAtUtc ?? sale.CreatedAtUtc,
            SubtotalCents    = sale.SubtotalCents,
            DiscountCents    = sale.DiscountCents,
            TotalCents       = sale.TotalCents,
            CustomerName     = sale.CustomerName,
            CustomerDocument = sale.CustomerDocument,
            ContingencyType  = ContingencyType.None,
            Emitter          = emitter,
            Items            = fiscalItems,
            Payments         = fiscalPayments,
        };

        var (unsignedXml, accessKey) = NfceXmlBuilder.Build(req);
        return Content($"<!-- accessKey={accessKey} -->\n{unsignedXml}", "application/xml; charset=utf-8");
    }

    private static EmitterData DebugBuildEmitter(CashRegisterFiscalConfig c) => new(
        c.Cnpj, c.InscricaoEstadual, c.RazaoSocial, c.NomeFantasia, c.Uf,
        c.Logradouro, c.NumeroEndereco, c.Complemento, c.Bairro,
        c.CodigoMunicipio, c.NomeMunicipio, c.Cep, c.Telefone,
        c.DefaultCfop, c.CscId, c.CscToken, c.SefazEnvironment, c.TaxRegime);

    private static EmitterData DebugBuildEmitter(FiscalConfig c) => new(
        c.Cnpj, c.InscricaoEstadual, c.RazaoSocial, c.NomeFantasia, c.Uf,
        c.Logradouro, c.NumeroEndereco, c.Complemento, c.Bairro,
        c.CodigoMunicipio, c.NomeMunicipio, c.Cep, c.Telefone,
        c.DefaultCfop, c.CscId, c.CscToken, c.SefazEnvironment, c.TaxRegime);

    // ── QR Code hash helper ───────────────────────────────────────────────────

    private static string GenerateQrHash(string chave, string cscId, string cscToken)
    {
        // SHA-1(chave + cscId(6 dígitos) + cscToken) — NT 2013.001
        var input = chave + cscId.PadLeft(6, '0') + cscToken;
        var bytes = System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FiscalConfigDto MapToDto(FiscalConfig cfg) => new()
    {
        Cnpj               = cfg.Cnpj,
        InscricaoEstadual  = cfg.InscricaoEstadual,
        Uf                 = cfg.Uf,
        RazaoSocial        = cfg.RazaoSocial,
        NomeFantasia       = cfg.NomeFantasia,
        Logradouro         = cfg.Logradouro,
        NumeroEndereco     = cfg.NumeroEndereco,
        Complemento        = cfg.Complemento,
        Bairro             = cfg.Bairro,
        CodigoMunicipio    = cfg.CodigoMunicipio,
        NomeMunicipio      = cfg.NomeMunicipio,
        Cep                = cfg.Cep,
        Telefone           = cfg.Telefone,
        TaxRegime          = cfg.TaxRegime.ToString(),
        SefazEnvironment   = cfg.SefazEnvironment.ToString(),
        // Nunca retornamos o certificado criptografado ao frontend
        HasCertificate     = !string.IsNullOrWhiteSpace(cfg.CertificateBase64) || !string.IsNullOrWhiteSpace(cfg.CertificatePath),
        CertificateBase64  = null,
        CertificatePassword = null,
        CertificatePath    = cfg.CertificatePath,
        CscId              = cfg.CscId,
        CscToken           = cfg.CscToken,
        NfceSerie          = cfg.NfceSerie,
        DefaultCfop        = cfg.DefaultCfop,
    };
}

// ── DTO ───────────────────────────────────────────────────────────────────────

public record CancelNfceRequest(string Reason);

public class FiscalConfigDto
{
    public string?  Cnpj              { get; set; }
    public string?  InscricaoEstadual { get; set; }
    public string?  Uf                { get; set; }
    public string?  RazaoSocial       { get; set; }
    public string?  NomeFantasia      { get; set; }
    public string?  Logradouro        { get; set; }
    public string?  NumeroEndereco    { get; set; }
    public string?  Complemento       { get; set; }
    public string?  Bairro            { get; set; }
    public int      CodigoMunicipio   { get; set; }
    public string?  NomeMunicipio     { get; set; }
    public string?  Cep               { get; set; }
    public string?  Telefone          { get; set; }
    public string?  TaxRegime         { get; set; } = "SimplesNacional";
    public string?  SefazEnvironment  { get; set; } = "Homologacao";
    public bool     HasCertificate      { get; set; }
    public string?  CertificateBase64  { get; set; }
    public string?  CertificatePassword { get; set; }
    public string?  CertificatePath    { get; set; } // legado
    public string?  CscId             { get; set; }
    public string?  CscToken          { get; set; }
    public short    NfceSerie         { get; set; } = 1;
    public string?  DefaultCfop       { get; set; } = "5102";
}
