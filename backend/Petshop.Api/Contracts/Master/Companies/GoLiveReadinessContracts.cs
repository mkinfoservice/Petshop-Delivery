namespace Petshop.Api.Contracts.Master.Companies;

public enum ReadinessStatus
{
    Ok,
    Warning,
    Blocked,
}

public record ReadinessCheckDto(
    string Category,   // "fiscal" | "operacao"
    string Key,
    string Label,
    ReadinessStatus Status,
    string? Detail
);

public record GoLiveReadinessDto(
    Guid CompanyId,
    int ScorePercent,
    bool FiscalReadyForProduction,
    List<ReadinessCheckDto> Checks
);
