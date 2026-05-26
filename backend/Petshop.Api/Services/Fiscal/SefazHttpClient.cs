using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Petshop.Api.Entities.Fiscal;

namespace Petshop.Api.Services.Fiscal;

/// <summary>
/// Cliente HTTP para os web services SEFAZ via SOAP 1.2.
/// Suporta: autorização NFC-e, cancelamento e status de serviço.
/// </summary>
public class SefazHttpClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SefazHttpClient> _logger;

    private static readonly XNamespace NfeNs   = "http://www.portalfiscal.inf.br/nfe";
    private static readonly XNamespace WsdlNs  = "http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4";
    private static readonly XNamespace Soap12  = "http://www.w3.org/2003/05/soap-envelope";

    public SefazHttpClient(HttpClient http, ILogger<SefazHttpClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    // ── Autorização ───────────────────────────────────────────────────────────

    public async Task<SefazAuthResult> AuthorizeAsync(
        string uf,
        SefazEnvironment env,
        string signedNfeXml,
        short serie,
        CancellationToken ct = default)
    {
        var url   = SefazEndpoints.GetAuthUrl(uf, env);
        var ufCode = SefazEndpoints.UfToCode(uf);
        var tpAmb  = env == SefazEnvironment.Producao ? "1" : "2";
        var idLote = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var envelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap12:Envelope xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Header>
    <nfeCabecMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4"">
      <cUF>{ufCode}</cUF>
      <versaoDados>4.00</versaoDados>
    </nfeCabecMsg>
  </soap12:Header>
  <soap12:Body>
    <nfeDadosMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4"">
      <enviNFe versao=""4.00"" xmlns=""http://www.portalfiscal.inf.br/nfe"">
        <idLote>{idLote}</idLote>
        <indSinc>1</indSinc>
        {signedNfeXml}
      </enviNFe>
    </nfeDadosMsg>
  </soap12:Body>
</soap12:Envelope>";

        _logger.LogInformation("[SEFAZ] POST AuthorizeAsync → {Url}", url);

        try
        {
            var content = new StringContent(envelope, Encoding.UTF8);
            // SOAP 1.2: action é obrigatório no Content-Type — sem ele o SEFAZ retorna SOAP Fault
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                "application/soap+xml; charset=utf-8; action=\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4/nfeDadosMsg\"");

            var resp = await _http.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            _logger.LogInformation("[SEFAZ] Auth Response HTTP={Status} Body={Body}",
                (int)resp.StatusCode, body[..Math.Min(800, body.Length)]);

            return ParseAuthResponse(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SEFAZ] Erro na chamada de autorização.");
            return SefazAuthResult.NetworkError(ex.Message);
        }
    }

    private static SefazAuthResult ParseAuthResponse(string soapBody)
    {
        try
        {
            var doc = XDocument.Parse(soapBody);
            var ns  = NfeNs;

            // Pega infProt (dentro de protNFe) — resposta de autorização normal
            var infProt = doc.Descendants(ns + "infProt").FirstOrDefault();
            if (infProt != null)
            {
                var cStat   = infProt.Element(ns + "cStat")?.Value ?? "";
                var xMotivo = infProt.Element(ns + "xMotivo")?.Value ?? "";
                var chave   = infProt.Element(ns + "chNFe")?.Value;
                var nProt   = infProt.Element(ns + "nProt")?.Value ?? "";
                var dhRecbto = infProt.Element(ns + "dhRecbto")?.Value;

                if (cStat == "100" || cStat == "204")
                    return SefazAuthResult.Authorized(chave!, nProt, dhRecbto, soapBody);

                return SefazAuthResult.Rejected(cStat, xMotivo);
            }

            // retEnviNFe: erro de serviço sem infProt (ex: cStat=107 Serviço em Uso)
            var cStatRet   = doc.Descendants(ns + "cStat").FirstOrDefault()?.Value;
            var xMotivoRet = doc.Descendants(ns + "xMotivo").FirstOrDefault()?.Value;
            if (cStatRet != null)
                return SefazAuthResult.Rejected(cStatRet, xMotivoRet ?? "Sem motivo");

            // SOAP Fault — extrai texto do Fault para diagnóstico
            var soap = Soap12;
            var faultCode   = doc.Descendants(soap + "Code").FirstOrDefault()?.Value
                           ?? doc.Descendants("Code").FirstOrDefault()?.Value;
            var faultReason = doc.Descendants(soap + "Text").FirstOrDefault()?.Value
                           ?? doc.Descendants("faultstring").FirstOrDefault()?.Value
                           ?? soapBody[..Math.Min(200, soapBody.Length)];

            return SefazAuthResult.Rejected("SOAP-FAULT", $"[{faultCode}] {faultReason}");
        }
        catch (Exception ex)
        {
            return SefazAuthResult.NetworkError($"Parse error: {ex.Message} | Body: {soapBody[..Math.Min(200, soapBody.Length)]}");
        }
    }

    // ── Status de serviço ────────────────────────────────────────────────────

    public async Task<bool> IsOnlineAsync(string uf, SefazEnvironment env, CancellationToken ct = default)
    {
        var url    = SefazEndpoints.GetStatusUrl(uf, env);
        var ufCode = SefazEndpoints.UfToCode(uf);
        var tpAmb  = env == SefazEnvironment.Producao ? "1" : "2";

        var envelope = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soap12:Envelope xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Header>
    <nfeCabecMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeStatusServico4"">
      <cUF>{ufCode}</cUF>
      <versaoDados>4.00</versaoDados>
    </nfeCabecMsg>
  </soap12:Header>
  <soap12:Body>
    <nfeDadosMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeStatusServico4"">
      <consStatServ versao=""4.00"" xmlns=""http://www.portalfiscal.inf.br/nfe"">
        <tpAmb>{tpAmb}</tpAmb>
        <cUF>{ufCode}</cUF>
        <xServ>STATUS</xServ>
      </consStatServ>
    </nfeDadosMsg>
  </soap12:Body>
</soap12:Envelope>";

        try
        {
            var content = new StringContent(envelope, Encoding.UTF8);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/soap+xml; charset=utf-8");

            var resp = await _http.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            var doc  = XDocument.Parse(body);
            var cStat = doc.Descendants(NfeNs + "cStat").FirstOrDefault()?.Value ?? "999";
            return cStat is "107" or "108"; // 107 = Operação, 108 = Parado temporariamente (aceita NF)
        }
        catch
        {
            return false;
        }
    }
}

// ── Result types ──────────────────────────────────────────────────────────────

public record SefazAuthResult(
    bool    Success,
    string? AccessKey,
    string? Protocol,
    string? AuthDateTimeUtc,
    string? XmlProtocol,
    string? RejectCode,
    string? RejectMessage,
    bool    IsNetworkError)
{
    public static SefazAuthResult Authorized(string key, string prot, string? dt, string xml)
        => new(true, key, prot, dt, xml, null, null, false);

    public static SefazAuthResult Rejected(string code, string msg)
        => new(false, null, null, null, null, code, msg, false);

    public static SefazAuthResult NetworkError(string msg)
        => new(false, null, null, null, null, "NET", msg, true);
}
