namespace Petshop.Api.Contracts.Auth;

public record ResetPasswordAuthRequest(string Token, string NewPassword);
