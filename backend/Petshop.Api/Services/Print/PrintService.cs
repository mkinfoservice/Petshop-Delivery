using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities;
using Petshop.Api.Hubs;

namespace Petshop.Api.Services.Print;

/// <summary>
/// Registra um job de impressão no banco e emite o evento SignalR para o painel admin.
/// Se o painel estiver offline, o job fica na fila (IsPrinted = false) e é enviado
/// na próxima conexão via PendingJobsEndpoint.
/// </summary>
public class PrintService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<PrintHub> _hub;

    public PrintService(AppDbContext db, IHubContext<PrintHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    /// <summary>
    /// Cria um job de impressão para o pedido informado e dispara via SignalR.
    /// </summary>
    public async Task EnqueueAsync(Order order, CancellationToken ct = default)
    {
        // Carrega os itens se não estiverem em memória
        if (order.Items.Count == 0)
        {
            await _db.Entry(order).Collection(o => o.Items).LoadAsync(ct);
        }

        var branding = await ResolveBrandingAsync(order.CompanyId, ct);

        var payload = new PrintOrderPayload
        {
            OrderId     = order.Id,
            PublicId    = order.PublicId,
            CustomerName = order.CustomerName,
            Phone       = order.Phone,
            Address     = order.Address,
            Complement  = order.Complement,
            Cep         = order.Cep,
            PaymentMethod = order.PaymentMethod,
            TotalCents  = order.TotalCents,
            SubtotalCents = order.SubtotalCents,
            DeliveryCents = order.DeliveryCents,
            CashGivenCents = order.CashGivenCents,
            ChangeCents  = order.ChangeCents,
            IsPhoneOrder = order.IsPhoneOrder,
            CreatedAtUtc = order.CreatedAtUtc,
            Branding = branding,
            Items = order.Items.Select(i => new PrintItemPayload
            {
                Name     = i.ProductNameSnapshot,
                Qty      = i.Qty,
                UnitCents = i.UnitPriceCentsSnapshot,
            }).ToList(),
        };

        var payloadJson = JsonSerializer.Serialize(payload);

        var job = new OrderPrintJob
        {
            CompanyId       = order.CompanyId ?? Guid.Empty,
            OrderId         = order.Id,
            PublicId        = order.PublicId,
            PrintPayloadJson = payloadJson,
            IsPrinted       = false,
        };

        _db.PrintJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Dispara via SignalR (fire-and-forget — não falha o pedido se hub offline)
        try
        {
            await _hub.Clients
                .Group($"company-{order.CompanyId}")
                .SendAsync("PrintOrder", new { jobId = job.Id, payload }, cancellationToken: ct);
        }
        catch (Exception)
        {
            // SignalR offline — job permanece na fila para replay
        }
    }

    private async Task<PrintBrandingPayload> ResolveBrandingAsync(Guid? companyId, CancellationToken ct)
    {
        if (!companyId.HasValue || companyId.Value == Guid.Empty)
            return PrintBrandingPayload.Default;

        var company = await _db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId.Value)
            .Select(c => new { c.Name })
            .FirstOrDefaultAsync(ct);

        var config = await _db.StoreFrontConfigs
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId.Value)
            .Select(c => new
            {
                c.LogoUrl,
                c.StoreName,
                c.StoreSlogan,
                c.PrimaryColor,
                c.SecondaryColor,
                c.AccentColor
            })
            .FirstOrDefaultAsync(ct);

        var storeName = FirstNonEmpty(config?.StoreName, company?.Name) ?? "vendApps";

        return new PrintBrandingPayload
        {
            StoreName = storeName,
            StoreSlogan = FirstNonEmpty(config?.StoreSlogan, null),
            LogoUrl = FirstNonEmpty(config?.LogoUrl, null),
            PrimaryColor = FirstNonEmpty(config?.PrimaryColor) ?? "#111827",
            SecondaryColor = FirstNonEmpty(config?.SecondaryColor) ?? "#111827",
            AccentColor = FirstNonEmpty(config?.AccentColor) ?? "#111827"
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
    }
}

// ── Payloads ──────────────────────────────────────────────────────────────────

public record PrintOrderPayload
{
    public Guid OrderId { get; init; }
    public string PublicId { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public string Phone { get; init; } = "";
    public string Address { get; init; } = "";
    public string? Complement { get; init; }
    public string Cep { get; init; } = "";
    public string PaymentMethod { get; init; } = "PIX";
    public int TotalCents { get; init; }
    public int SubtotalCents { get; init; }
    public int DeliveryCents { get; init; }
    public int? CashGivenCents { get; init; }
    public int? ChangeCents { get; init; }
    public bool IsPhoneOrder { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public PrintBrandingPayload Branding { get; init; } = PrintBrandingPayload.Default;
    public List<PrintItemPayload> Items { get; init; } = new();
}

public record PrintBrandingPayload
{
    public static PrintBrandingPayload Default { get; } = new();

    public string StoreName { get; init; } = "vendApps";
    public string? StoreSlogan { get; init; }
    public string? LogoUrl { get; init; }
    public string PrimaryColor { get; init; } = "#111827";
    public string SecondaryColor { get; init; } = "#111827";
    public string AccentColor { get; init; } = "#111827";
}

public record PrintItemPayload
{
    public string Name { get; init; } = "";
    public int Qty { get; init; }
    public int UnitCents { get; init; }
}
