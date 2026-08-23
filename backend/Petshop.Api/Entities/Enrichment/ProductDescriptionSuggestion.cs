using System.ComponentModel.DataAnnotations;
using Petshop.Api.Models;

namespace Petshop.Api.Entities.Enrichment;

public enum DescriptionSuggestionStatus { Pending, Approved, Rejected }

/// <summary>
/// Sugestão de descrição gerada por IA para um produto.
/// NUNCA é auto-aplicada (diferente de nome/imagem) — sempre exige revisão manual,
/// já que texto de marketing gerado por LLM pode alucinar características/certificações
/// inexistentes, o que é um risco de propaganda enganosa em um e-commerce ao vivo.
/// </summary>
public class ProductDescriptionSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;

    public Guid BatchId { get; set; }
    public EnrichmentBatch Batch { get; set; } = default!;

    /// <summary>Descrição original do produto no momento da sugestão (imutável, para auditoria).</summary>
    public string? OriginalDescription { get; set; }

    /// <summary>Descrição gerada pela IA.</summary>
    public string SuggestedDescription { get; set; } = default!;

    /// <summary>Nome do modelo LLM usado (ex: "claude-haiku-4-5-20251001"), para auditoria.</summary>
    [MaxLength(100)]
    public string? ModelUsed { get; set; }

    public DescriptionSuggestionStatus Status { get; set; } = DescriptionSuggestionStatus.Pending;

    [MaxLength(100)]
    public string? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
