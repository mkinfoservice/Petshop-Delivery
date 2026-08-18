namespace Petshop.Api.Contracts.Auth;

/// <summary>
/// Slug é obrigatório: frontend (Vercel) e backend (Render) estão em domínios
/// distintos, então o Host header da request sempre chega como o domínio do
/// backend — não dá pra resolver o tenant a partir dele.
/// </summary>
public record DelivererLoginRequest(string Phone, string Pin, string? Slug);
