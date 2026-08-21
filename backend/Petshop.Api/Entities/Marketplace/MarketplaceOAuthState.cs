using System.ComponentModel.DataAnnotations;
using Petshop.Api.Entities.Catalog;

namespace Petshop.Api.Entities.Marketplace;

/// <summary>
/// Estado persistido do fluxo OAuth2 authorization_code (ex: Mercado Livre).
/// Necessário porque o marketplace redireciona o navegador do vendedor de volta
/// para um callback publico, sem contexto de tenant — o "state" e quem resolve
/// para qual empresa aquela autorizacao pertence. Persistido (nao em memoria)
/// para sobreviver a restart/deploy e funcionar com multiplas instancias.
/// </summary>
public class MarketplaceOAuthState
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Valor opaco enviado como "state" na URL de autorizacao e devolvido no callback.</summary>
    [MaxLength(80)]
    public string State { get; set; } = "";

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public MarketplaceType Type { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Curta duracao proposital (minutos) — so cobre o tempo do redirect ida-e-volta.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Marcado quando consumido no callback — evita reuso do mesmo state.</summary>
    public DateTime? ConsumedAtUtc { get; set; }
}
