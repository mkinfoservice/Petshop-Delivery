using System.ComponentModel.DataAnnotations;

namespace Petshop.Api.Entities.Catalog;

public class CompanyCustomDomain
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    [Required, MaxLength(253)]
    public string Hostname { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Status { get; set; } = CompanyCustomDomainStatus.Pending;

    [Required, MaxLength(80)]
    public string VerificationToken { get; set; } = default!;

    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}

public static class CompanyCustomDomainStatus
{
    public const string Pending = "pending";
    public const string Verified = "verified";
    public const string Active = "active";
    public const string Disabled = "disabled";
}
