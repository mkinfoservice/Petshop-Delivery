using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Master;
using Petshop.Api.Services.Accounting;

namespace Petshop.Api.Services.Auth;

/// <summary>
/// Recuperação de senha self-service para AdminUser. Token de uso único, expira em
/// 30 minutos, hash SHA-256 persistido (nunca o token bruto). Reaproveita o mailer
/// SMTP genérico já usado pelo disparo contábil (AccountingDispatch:Smtp:*).
/// </summary>
public class PasswordResetService
{
    private const int TokenValidMinutes = 30;

    private readonly AppDbContext _db;
    private readonly AccountingEmailService _email;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(AppDbContext db, AccountingEmailService email, ILogger<PasswordResetService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    /// <summary>
    /// Sempre "sucede" silenciosamente (mesmo se o usuário não existir ou não tiver
    /// e-mail cadastrado) — evita enumeração de usuários. O e-mail só é de fato
    /// enviado quando há uma conta correspondente.
    /// </summary>
    public async Task RequestResetAsync(
        string identifier, Guid companyId, string resetUrlBase, CancellationToken ct = default)
    {
        var trimmed = identifier.Trim();
        var admin = await _db.AdminUsers.FirstOrDefaultAsync(u =>
            u.CompanyId == companyId && u.IsActive &&
            (u.Username == trimmed || u.Email == trimmed), ct);

        if (admin is null || string.IsNullOrWhiteSpace(admin.Email))
        {
            _logger.LogInformation("[PasswordReset] Solicitação para identificador sem conta/e-mail correspondente (empresa {CompanyId}).", companyId);
            return;
        }

        var rawToken = GenerateRawToken();

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            AdminUserId = admin.Id,
            TokenHash = Hash(rawToken),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(TokenValidMinutes),
        });
        await _db.SaveChangesAsync(ct);

        var separator = resetUrlBase.Contains('?') ? "&" : "?";
        var resetLink = $"{resetUrlBase}{separator}token={Uri.EscapeDataString(rawToken)}";

        var body =
            $"Olá {admin.Username},\n\n" +
            "Recebemos uma solicitação para redefinir sua senha no vendApps.\n\n" +
            $"Clique no link abaixo para criar uma nova senha (válido por {TokenValidMinutes} minutos):\n{resetLink}\n\n" +
            "Se você não solicitou isso, pode ignorar este e-mail — sua senha atual continua válida.";

        try
        {
            await _email.SendAsync(
                new AccountingEmailMessage(admin.Email, [], "Redefinição de senha — vendApps", body),
                [],
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PasswordReset] Falha ao enviar e-mail de reset para AdminUser {AdminUserId}.", admin.Id);
        }
    }

    /// <summary>Retorna false se o token for inválido, expirado ou já usado.</summary>
    public async Task<bool> ResetPasswordAsync(string rawToken, string newPassword, CancellationToken ct = default)
    {
        var tokenHash = Hash(rawToken);
        var entry = await _db.PasswordResetTokens
            .Include(t => t.AdminUser)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.UsedAtUtc == null &&
                t.ExpiresAtUtc > DateTime.UtcNow, ct);

        if (entry?.AdminUser is null) return false;

        entry.AdminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        entry.UsedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string Hash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
