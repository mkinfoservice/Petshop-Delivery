using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Petshop.Api.Hubs;

/// <summary>
/// Hub SignalR para envio de eventos de impressão ao painel admin.
/// O cliente se conecta e entra no grupo "company-{companyId}".
/// O servidor emite "PrintOrder" com o payload do pedido.
/// </summary>
[Authorize]
public class PrintHub : Hub
{
    /// <summary>
    /// Chamado pelo cliente admin ao conectar.
    /// Valida que o companyId informado pelo cliente corresponde ao claim do JWT.
    /// </summary>
    public async Task JoinCompany(string companyId)
    {
        var jwtCompanyId = Context.User?.FindFirstValue("companyId");

        if (string.IsNullOrEmpty(jwtCompanyId) ||
            !string.Equals(jwtCompanyId, companyId, StringComparison.OrdinalIgnoreCase))
        {
            throw new HubException("Acesso negado: companyId inválido.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"company-{companyId}");
    }

    public async Task LeaveCompany(string companyId)
    {
        var jwtCompanyId = Context.User?.FindFirstValue("companyId");

        if (string.IsNullOrEmpty(jwtCompanyId) ||
            !string.Equals(jwtCompanyId, companyId, StringComparison.OrdinalIgnoreCase))
        {
            throw new HubException("Acesso negado: companyId inválido.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"company-{companyId}");
    }
}
