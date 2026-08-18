using System.Security.Cryptography.X509Certificates;
using Petshop.Api.Entities.Fiscal;
using Unimake.Business.DFe.Servicos;
using Unimake.Business.DFe.Servicos.NFe;
using Unimake.Business.DFe.Xml.NFe;

namespace Petshop.Api.Services.Fiscal;

/// <summary>
/// Motor NFC-e baseado em Unimake.DFe.
/// Substitui NfceXmlBuilder + NfceSigningService + SefazHttpClient.
/// Handles: XML building, RSA-SHA1 signing, QR code, SOAP mTLS → SEFAZ.
/// </summary>
public class UnimakeNfceEngine
{
    private readonly ILogger<UnimakeNfceEngine> _logger;

    public UnimakeNfceEngine(ILogger<UnimakeNfceEngine> logger) => _logger = logger;

    public async Task<FiscalEngineResult> IssueAsync(
        FiscalDocumentRequest req,
        byte[] certBytes,
        string? certPassword,
        CancellationToken ct = default)
    {
        await Task.CompletedTask; // lib é síncrona; mantemos assinatura async para compatibilidade

        try
        {
            var cert = new X509Certificate2(
                certBytes,
                certPassword ?? "",
                X509KeyStorageFlags.EphemeralKeySet);

            var enviNFe = BuildEnviNFe(req);
            var config  = BuildConfiguracao(req.Emitter, cert);

            _logger.LogInformation(
                "[Unimake] Autorizando NFC-e #{N}/série {S} — {Env}",
                req.Number, req.Serie, req.Emitter.SefazEnvironment);

            var svc = new Autorizacao(enviNFe, config);
            svc.Executar();

            var infProt = svc.Result?.ProtNFe?.InfProt;
            if (infProt == null)
            {
                _logger.LogWarning("[Unimake] Resposta sem infProt — contingência.");
                return FiscalEngineResult.InContingency("", "Resposta SEFAZ sem infProt");
            }

            var cStat   = infProt.CStat;
            var xMotivo = infProt.XMotivo ?? "";
            var nProt   = infProt.NProt ?? "";
            var chNFe   = infProt.ChNFe ?? "";

            _logger.LogInformation(
                "[Unimake] cStat={CStat} xMotivo={XMotivo} nProt={NProt}",
                cStat, xMotivo, nProt);

            if (cStat is 100 or 204)
            {
                var xmlDoc = svc.NfeProcResult?.GerarXML();
                var xmlStr = xmlDoc?.OuterXml ?? "";
                return FiscalEngineResult.Authorized(chNFe, nProt, xmlStr);
            }

            return FiscalEngineResult.Rejected(cStat.ToString(), xMotivo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Unimake] Exceção ao autorizar NFC-e.");
            return FiscalEngineResult.InContingency("", ex.Message);
        }
    }

    /// <summary>Envia o evento de cancelamento (tpEvento 110111) de uma NFC-e já autorizada.</summary>
    public async Task<FiscalCancelResult> CancelAsync(
        FiscalCancelRequest req,
        byte[] certBytes,
        string? certPassword,
        CancellationToken ct = default)
    {
        await Task.CompletedTask; // lib é síncrona; mantemos assinatura async para compatibilidade

        try
        {
            var cert = new X509Certificate2(
                certBytes,
                certPassword ?? "",
                X509KeyStorageFlags.EphemeralKeySet);

            var tpAmb = req.SefazEnvironment == SefazEnvironment.Producao
                ? TipoAmbiente.Producao : TipoAmbiente.Homologacao;
            var uf   = Enum.Parse<UFBrasil>(req.Uf, ignoreCase: true);
            var cnpj = Digits(req.Cnpj).PadLeft(14, '0');

            const int seq = 1;
            const string tpEventoCode = "110111"; // Cancelamento

            var detEvento = new DetEventoCanc
            {
                DescEvento = "Cancelamento",
                NProt      = req.AuthorizationProtocol,
                XJust      = req.Reason,
                Versao     = "1.00",
            };

            var infEvento = new InfEvento(detEvento)
            {
                Id         = $"ID{tpEventoCode}{req.AccessKey}{seq:D2}",
                CNPJ       = cnpj,
                ChNFe      = req.AccessKey,
                COrgao     = uf,
                DhEvento   = DateTimeOffset.Now,
                NSeqEvento = seq,
                TpAmb      = tpAmb,
                TpEvento   = TipoEventoNFe.Cancelamento,
                VerEvento  = "1.00",
            };

            var envEvento = new EnvEvento
            {
                IdLote = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                Versao = "1.00",
                Evento = new List<Evento> { new() { InfEvento = infEvento, Versao = "1.00" } },
            };

            var config = new Configuracao
            {
                TipoDFe            = TipoDFe.NFCe,
                TipoAmbiente       = tpAmb,
                CertificadoDigital = cert,
            };

            _logger.LogInformation(
                "[Unimake] Cancelando NFC-e chave {ChNFe} — protocolo original {NProt}.",
                req.AccessKey, req.AuthorizationProtocol);

            var svc = new Unimake.Business.DFe.Servicos.NFCe.RecepcaoEvento(envEvento, config);
            svc.Executar();

            var infRet = svc.Result?.RetEvento?.FirstOrDefault()?.InfEvento;
            if (infRet is null)
                return FiscalCancelResult.Failed("999", "Resposta da SEFAZ sem infEvento.");

            _logger.LogInformation(
                "[Unimake] Cancelamento — cStat={CStat} xMotivo={XMotivo} nProt={NProt}",
                infRet.CStat, infRet.XMotivo, infRet.NProt);

            // 135/155 = evento de cancelamento registrado e vinculado à NFC-e.
            if (infRet.CStat is 135 or 155)
                return FiscalCancelResult.Cancelled(infRet.NProt ?? "");

            return FiscalCancelResult.Failed(infRet.CStat.ToString(), infRet.XMotivo ?? "Rejeitado pela SEFAZ.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Unimake] Exceção ao cancelar NFC-e {ChNFe}.", req.AccessKey);
            return FiscalCancelResult.Failed("998", ex.Message);
        }
    }

    // ── Build EnviNFe ─────────────────────────────────────────────────────────

    private static EnviNFe BuildEnviNFe(FiscalDocumentRequest req)
    {
        var em      = req.Emitter;
        var tpAmb   = em.SefazEnvironment == SefazEnvironment.Producao
            ? TipoAmbiente.Producao : TipoAmbiente.Homologacao;
        var uf      = Enum.Parse<UFBrasil>(em.Uf, ignoreCase: true);
        var dhEmi   = req.SaleDateTimeUtc.AddHours(-3); // UTC → BRT
        var isSimp  = em.TaxRegime == TaxRegime.SimplesNacional;
        var crt     = isSimp ? CRT.SimplesNacional : CRT.RegimeNormal;

        var cnpj = Digits(em.Cnpj).PadLeft(14, '0');
        var cep  = Digits(em.Cep).PadLeft(8, '0');
        var ie   = SanitizeIe(em.InscricaoEstadual);
        var fone = Digits(em.Telefone);
        var nro  = string.IsNullOrWhiteSpace(em.NumeroEndereco) ? "S/N" : em.NumeroEndereco;
        var bairro = string.IsNullOrWhiteSpace(em.Bairro) ? "N/D" : em.Bairro;

        return new EnviNFe
        {
            IdLote  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            IndSinc = SimNao.Sim,
            NFe     = new List<NFe>
            {
                new()
                {
                    InfNFeField = new InfNFe
                    {
                        Versao = "4.00",
                        Ide = new Ide
                        {
                            CUF     = uf,
                            NatOp   = "VENDA AO CONSUMIDOR",
                            Mod     = ModeloDFe.NFCe,
                            Serie   = req.Serie,
                            NNF     = req.Number,
                            DhEmi   = dhEmi,
                            TpNF    = TipoOperacao.Saida,
                            IdDest  = DestinoOperacao.OperacaoInterna,
                            CMunFG  = em.CodigoMunicipio,
                            TpImp   = FormatoImpressaoDANFE.NFCe,
                            TpEmis  = TipoEmissao.Normal,
                            TpAmb   = tpAmb,
                            FinNFe  = FinalidadeNFe.Normal,
                            IndFinal = SimNao.Sim,
                            IndPres  = IndicadorPresenca.OperacaoPresencial,
                            ProcEmi  = ProcessoEmissao.AplicativoContribuinte,
                            VerProc  = "vendApps 5.0",
                        },
                        Emit = new Emit
                        {
                            CNPJ  = cnpj,
                            XNome = em.RazaoSocial,
                            XFant = em.NomeFantasia,
                            IE    = ie,
                            CRT   = crt,
                            EnderEmit = new EnderEmit
                            {
                                XLgr   = em.Logradouro,
                                Nro    = nro,
                                XCpl   = em.Complemento,
                                XBairro = bairro,
                                CMun   = em.CodigoMunicipio,
                                XMun   = em.NomeMunicipio,
                                UF     = uf,
                                CEP    = cep,
                                Fone   = fone.Length >= 6 ? fone : null,
                            }
                        },
                        Det   = BuildDet(req, isSimp),
                        Total = BuildTotal(req),
                        Transp = new Transp { ModFrete = ModalidadeFrete.SemOcorrenciaTransporte },
                        Pag   = BuildPag(req.Payments),
                        InfAdic = new InfAdic { InfCpl = "Obrigado pela preferencia!" },
                    }
                }
            }
        };
    }

    private static List<Det> BuildDet(FiscalDocumentRequest req, bool isSimples)
    {
        return req.Items.Select(item =>
        {
            var barcode = IsValidGtin(item.Barcode) ? item.Barcode : "SEM GTIN";
            var qCom    = item.Quantity;
            var vUnCom  = item.UnitPriceCents / 100m;
            var vProd   = (double)(item.TotalCents / 100m);

            ICMS icms;
            if (isSimples)
            {
                icms = new ICMS
                {
                    ICMSSN102 = new ICMSSN102
                    {
                        Orig  = OrigemMercadoria.Nacional,
                        CSOSN = "400",
                    }
                };
            }
            else
            {
                icms = new ICMS
                {
                    ICMS40 = new ICMS40
                    {
                        Orig = OrigemMercadoria.Nacional,
                        CST  = "40",
                    }
                };
            }

            return new Det
            {
                NItem = item.ItemNumber,
                Prod = new Prod
                {
                    CProd    = item.ProductCode[..Math.Min(60, item.ProductCode.Length)],
                    CEAN     = barcode,
                    XProd    = item.ProductName[..Math.Min(120, item.ProductName.Length)],
                    NCM      = item.Ncm,
                    CFOP     = item.Cfop,
                    UCom     = item.Unit,
                    QCom     = qCom,
                    VUnCom   = vUnCom,
                    VProd    = vProd,
                    CEANTrib = barcode,
                    UTrib    = item.Unit,
                    QTrib    = qCom,
                    VUnTrib  = vUnCom,
                    IndTot   = SimNao.Sim,
                },
                Imposto = new Imposto
                {
                    ICMS   = icms,
                    PIS    = new PIS    { PISNT    = new PISNT    { CST = "07" } },
                    COFINS = new COFINS { COFINSNT = new COFINSNT { CST = "07" } },
                }
            };
        }).ToList();
    }

    private static Total BuildTotal(FiscalDocumentRequest req)
    {
        return new Total
        {
            ICMSTot = new ICMSTot
            {
                VProd  = (double)(req.SubtotalCents / 100m),
                VDesc  = (double)(req.DiscountCents / 100m),
                VNF    = (double)(req.TotalCents    / 100m),
            }
        };
    }

    private static Pag BuildPag(IReadOnlyList<FiscalPaymentData> payments)
    {
        var troco = payments.Sum(p => p.ChangeCents);
        return new Pag
        {
            DetPag  = payments.Select(p => new DetPag
            {
                IndPag = IndicadorPagamento.PagamentoVista,
                TPag   = MapPagamento(p.PaymentMethod),
                VPag   = (double)(p.AmountCents / 100m),
            }).ToList(),
            VTroco = troco > 0 ? (double)(troco / 100m) : 0d,
        };
    }

    private static MeioPagamento MapPagamento(string method) => method.ToUpperInvariant() switch
    {
        "DINHEIRO" or "CASH"          => MeioPagamento.Dinheiro,
        "CARTAO_CREDITO" or "CREDITO" => MeioPagamento.CartaoCredito,
        "CARTAO_DEBITO"  or "DEBITO"  => MeioPagamento.CartaoDebito,
        "PIX"                         => MeioPagamento.PagamentoInstantaneo,
        "CHEQUE" or "CHECK"           => MeioPagamento.Cheque,
        _                             => MeioPagamento.Outros,
    };

    // ── Configuracao ──────────────────────────────────────────────────────────

    private static Configuracao BuildConfiguracao(EmitterData em, X509Certificate2 cert)
    {
        var tpAmb = em.SefazEnvironment == SefazEnvironment.Producao
            ? TipoAmbiente.Producao : TipoAmbiente.Homologacao;

        var cscIdInt = int.TryParse(em.CscId, out var parsed) ? parsed : 1;

        return new Configuracao
        {
            TipoDFe            = TipoDFe.NFCe,
            TipoAmbiente       = tpAmb,
            CertificadoDigital = cert,
            CSC                = em.CscToken ?? "",
            CSCIDToken         = cscIdInt,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Digits(string? s) =>
        new string((s ?? "").Where(c => c >= '0' && c <= '9').ToArray());

    private static string SanitizeIe(string? ie)
    {
        if (string.IsNullOrWhiteSpace(ie)) return "ISENTO";
        var upper = ie.Trim().ToUpperInvariant();
        if (upper is "ISENTO" or "NA") return upper;
        var d = Digits(ie);
        return string.IsNullOrEmpty(d) ? "ISENTO" : d;
    }

    private static bool IsValidGtin(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        foreach (var c in code)
            if (c < '0' || c > '9') return false;
        return code.Length == 8 || (code.Length >= 12 && code.Length <= 14);
    }
}
