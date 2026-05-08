namespace Petshop.Api.Contracts.Admin.Audit;

public record OperationalAuditListResponse(
    int Page,
    int PageSize,
    int Total,
    IReadOnlyList<OperationalAuditListItem> Items);

public record OperationalAuditListItem(
    Guid Id,
    string Action,
    string TargetType,
    string TargetId,
    string? TargetName,
    string ActorUsername,
    string ActorRole,
    string? CorrelationId,
    DateTime CreatedAtUtc,
    bool HasPayload);

public record OperationalAuditDetailResponse(
    Guid Id,
    Guid? CompanyId,
    string? CompanySlug,
    string? ActorId,
    string ActorUsername,
    string ActorRole,
    string Action,
    string TargetType,
    string TargetId,
    string? TargetName,
    string? PayloadJson,
    string? CorrelationId,
    DateTime CreatedAtUtc);
