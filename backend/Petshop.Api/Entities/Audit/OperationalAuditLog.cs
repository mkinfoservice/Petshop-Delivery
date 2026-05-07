using System.ComponentModel.DataAnnotations;

namespace Petshop.Api.Entities.Audit;

/// <summary>
/// Immutable tenant-level operational audit log.
/// References are intentionally loose to avoid cascade/delete coupling.
/// </summary>
public class OperationalAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? CompanyId { get; set; }

    [MaxLength(120)]
    public string? CompanySlug { get; set; }

    [MaxLength(100)]
    public string? ActorId { get; set; }

    [Required, MaxLength(120)]
    public string ActorUsername { get; set; } = "unknown";

    [Required, MaxLength(40)]
    public string ActorRole { get; set; } = "unknown";

    [Required, MaxLength(100)]
    public string Action { get; set; } = default!;

    [Required, MaxLength(80)]
    public string TargetType { get; set; } = default!;

    [Required, MaxLength(120)]
    public string TargetId { get; set; } = default!;

    [MaxLength(200)]
    public string? TargetName { get; set; }

    public string? PayloadJson { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(300)]
    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
