using System.ComponentModel.DataAnnotations;

namespace Petshop.Api.Entities.Master;

/// <summary>
/// Documento legal versionado da plataforma vendApps (Termos de Uso / Política de
/// Privacidade). Publicar uma nova versão desativa a anterior automaticamente —
/// só uma versão ativa por DocumentType.
/// </summary>
public class LegalDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>"terms" | "privacy"</summary>
    [Required, MaxLength(20)]
    public string DocumentType { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Version { get; set; } = "1.0";

    [Required]
    public string Content { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public DateTime PublishedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
