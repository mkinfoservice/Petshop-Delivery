using Microsoft.AspNetCore.DataProtection;

namespace Petshop.Api.Services.Marketplace;

/// <summary>
/// Protege segredos de integracao de marketplace em repouso (client secret,
/// access token, refresh token) via ASP.NET Core Data Protection.
///
/// Substitui o padrao anterior (iFood) que gravava ClientSecretEncrypted em
/// texto puro apesar do nome do campo. Mesmo mecanismo do CpfProtectionService.
/// </summary>
public class MarketplaceCredentialProtectionService
{
    private readonly IDataProtector _protector;

    public MarketplaceCredentialProtectionService(IDataProtectionProvider dpProvider)
    {
        _protector = dpProvider.CreateProtector("Marketplace.Credential.v1");
    }

    /// <summary>Criptografa. Retorna null se o valor for null/vazio.</summary>
    public string? Protect(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return _protector.Protect(value);
    }

    /// <summary>
    /// Descriptografa. Dado legado gravado em texto puro (antes desta mudanca)
    /// e retornado como esta, sem lancar erro — evita quebrar integracoes
    /// existentes ate a proxima gravacao, que ja salva criptografado.
    /// </summary>
    public string? Unprotect(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return null;
        if (!IsProtected(stored)) return stored;
        try { return _protector.Unprotect(stored); }
        catch { return null; }
    }

    /// <summary>True se o valor parece ja criptografado pelo Data Protection API.</summary>
    public static bool IsProtected(string? value) =>
        value != null && value.StartsWith("CfDJ8", StringComparison.Ordinal);
}
