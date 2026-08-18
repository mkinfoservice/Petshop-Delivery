using System.ComponentModel.DataAnnotations;

namespace Petshop.Api.Entities.Master;

/// <summary>
/// Token de uso único para recuperação de senha self-service de AdminUser.
/// O token bruto nunca é persistido — só o hash SHA-256 dele.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }

    [Required, MaxLength(64)]
    public string TokenHash { get; set; } = default!;

    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
