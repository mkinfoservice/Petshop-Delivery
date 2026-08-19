namespace Petshop.Api.Contracts.Orders;

public sealed record PdvOrderSearchItem(
    Guid Id,
    string PublicId,
    string CustomerName,
    string Phone,
    int TotalCents,
    int ItemCount,
    string Channel,
    string Status,
    DateTime CreatedAtUtc
);

public sealed record PdvOrderSearchResponse(
    IReadOnlyList<PdvOrderSearchItem> Items
);
