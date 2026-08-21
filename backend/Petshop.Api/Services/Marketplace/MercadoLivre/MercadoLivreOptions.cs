namespace Petshop.Api.Services.Marketplace.MercadoLivre;

/// <summary>
/// Credenciais da aplicacao vendApps no Mercado Livre — GLOBAIS (uma aplicacao
/// so, muitos vendedores autorizam ela), diferente do iFood onde cada
/// MarketplaceIntegration guarda seu proprio ClientId/ClientSecret.
///
/// Env vars no Render: MercadoLivre__AppId, MercadoLivre__ClientSecret,
/// MercadoLivre__RedirectUri.
/// </summary>
public class MercadoLivreOptions
{
    public const string SectionName = "MercadoLivre";

    public string AppId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    /// <summary>Precisa bater exatamente com a Redirect URI configurada no painel do ML.</summary>
    public string RedirectUri { get; set; } = "";
}
