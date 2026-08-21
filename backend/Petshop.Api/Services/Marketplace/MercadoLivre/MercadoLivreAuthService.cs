using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Petshop.Api.Data;
using Petshop.Api.Entities.Marketplace;

namespace Petshop.Api.Services.Marketplace.MercadoLivre;

/// <summary>
/// OAuth2 authorization_code do Mercado Livre. Diferente do iFood
/// (client_credentials, sem consentimento de usuario), aqui o vendedor
/// precisa autorizar no navegador — access/refresh token sao persistidos
/// criptografados no MarketplaceIntegration (nunca so em memoria).
/// </summary>
public class MercadoLivreAuthService
{
    private const string AuthorizeUrl = "https://auth.mercadolivre.com.br/authorization";
    private const string TokenUrl = "https://api.mercadolibre.com/oauth/token";

    private readonly IHttpClientFactory _http;
    private readonly MercadoLivreOptions _options;
    private readonly MarketplaceCredentialProtectionService _credentials;
    private readonly ILogger<MercadoLivreAuthService> _logger;

    public MercadoLivreAuthService(
        IHttpClientFactory http,
        IOptions<MercadoLivreOptions> options,
        MarketplaceCredentialProtectionService credentials,
        ILogger<MercadoLivreAuthService> logger)
    {
        _http = http;
        _options = options.Value;
        _credentials = credentials;
        _logger = logger;
    }

    public string BuildAuthorizeUrl(string state) =>
        $"{AuthorizeUrl}?response_type=code" +
        $"&client_id={Uri.EscapeDataString(_options.AppId)}" +
        $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
        $"&state={Uri.EscapeDataString(state)}";

    /// <summary>Troca o "code" do callback por access_token + refresh_token.</summary>
    public async Task<MercadoLivreTokenResponse> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", _options.AppId),
            new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _options.RedirectUri),
        });

        return await PostTokenAsync(form, ct);
    }

    private async Task<MercadoLivreTokenResponse> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", _options.AppId),
            new KeyValuePair<string, string>("client_secret", _options.ClientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
        });

        return await PostTokenAsync(form, ct);
    }

    private async Task<MercadoLivreTokenResponse> PostTokenAsync(FormUrlEncodedContent form, CancellationToken ct)
    {
        using var client = _http.CreateClient("mercadolivre");
        var response = await client.PostAsync(TokenUrl, form, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("[MercadoLivre] Falha OAuth. Status={Status} Body={Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Mercado Livre OAuth falhou: {response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<MercadoLivreTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Mercado Livre retornou resposta de token vazia.");
    }

    /// <summary>
    /// Retorna um access token valido para a integracao, renovando via refresh
    /// token e persistindo (criptografado) se estiver perto de expirar.
    /// </summary>
    public async Task<string> GetValidAccessTokenAsync(
        MarketplaceIntegration integration,
        AppDbContext db,
        CancellationToken ct)
    {
        if (integration.AccessTokenEncrypted is not null
            && integration.TokenExpiresAtUtc is { } expiresAt
            && expiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return _credentials.Unprotect(integration.AccessTokenEncrypted)
                ?? throw new InvalidOperationException("Falha ao descriptografar access token.");
        }

        var refreshToken = _credentials.Unprotect(integration.RefreshTokenEncrypted);
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException(
                $"Integracao {integration.Id} sem refresh token — vendedor precisa reconectar.");

        var result = await RefreshAsync(refreshToken, ct);
        await PersistTokensAsync(integration, db, result, ct);
        return result.AccessToken!;
    }

    public async Task PersistTokensAsync(
        MarketplaceIntegration integration,
        AppDbContext db,
        MercadoLivreTokenResponse token,
        CancellationToken ct)
    {
        integration.AccessTokenEncrypted  = _credentials.Protect(token.AccessToken);
        integration.RefreshTokenEncrypted = _credentials.Protect(token.RefreshToken);
        // Margem de 10min para renovar antes do vencimento real.
        integration.TokenExpiresAtUtc     = DateTime.UtcNow.AddSeconds(token.ExpiresIn).AddMinutes(-10);
        integration.LastErrorMessage      = null;

        await db.SaveChangesAsync(ct);
    }
}
