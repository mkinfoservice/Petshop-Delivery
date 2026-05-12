namespace Petshop.Api.Messaging.Contracts;

/// <summary>
/// Publicado quando um pedido avança para PRONTO_PARA_ENTREGA e ainda não possui coordenadas.
/// O consumer busca as coordenadas via ViaCEP + provedor de geocoding e salva no pedido.
/// </summary>
public record GeocodingRequestedEvent
{
    public Guid     OrderId       { get; init; }
    public Guid     CompanyId     { get; init; }
    public string   PublicId      { get; init; } = "";
    public string?  Address       { get; init; }
    public string?  Cep           { get; init; }
    public string?  CorrelationId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
