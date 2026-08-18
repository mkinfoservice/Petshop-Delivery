namespace Petshop.Api.Contracts.Auth;

public record ForgotPasswordRequest(string Identifier, string Slug);
