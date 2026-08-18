using Petshop.Api.Entities.Fiscal;

namespace Petshop.Api.Services.Fiscal;

/// <summary>Dados necessários para enviar o evento de cancelamento (tpEvento 110111) à SEFAZ.</summary>
public record FiscalCancelRequest(
    string AccessKey,
    string AuthorizationProtocol,
    string Reason,
    string Cnpj,
    string Uf,
    SefazEnvironment SefazEnvironment);

/// <summary>Resultado do evento de cancelamento — separado de FiscalEngineResult porque o
/// protocolo retornado aqui é do evento, não de uma autorização de emissão.</summary>
public class FiscalCancelResult
{
    public bool Success { get; init; }
    public string? Protocol { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static FiscalCancelResult Cancelled(string protocol) => new()
    {
        Success = true,
        Protocol = protocol,
    };

    public static FiscalCancelResult Failed(string code, string message) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
    };
}
