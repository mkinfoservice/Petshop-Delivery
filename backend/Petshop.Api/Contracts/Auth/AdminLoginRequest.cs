namespace Petshop.Api.Contracts.Auth;

/// <summary>
/// Slug é opcional por compatibilidade (frontend antigo em cache), mas deve ser
/// enviado sempre que possível: escopa o AdminUser por empresa, permitindo que
/// tenants diferentes tenham usernames iguais (ex: "admin" em cada um).
/// </summary>
public record AdminLoginRequest(string Username, string Password, string? Slug = null);