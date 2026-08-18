using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;

namespace Petshop.Api.Controllers;

/// <summary>
/// Expõe a versão ativa dos documentos legais da plataforma (Termos de Uso e
/// Política de Privacidade) para as páginas públicas /termos e /privacidade.
/// </summary>
[ApiController]
[Route("public/legal")]
public class PublicLegalController : ControllerBase
{
    private static readonly string[] ValidTypes = ["terms", "privacy"];

    private readonly AppDbContext _db;

    public PublicLegalController(AppDbContext db) => _db = db;

    [HttpGet("{type}")]
    public async Task<IActionResult> Get(string type, CancellationToken ct)
    {
        var normalized = type.Trim().ToLowerInvariant();
        if (!ValidTypes.Contains(normalized))
            return BadRequest(new { error = "Tipo inválido. Use 'terms' ou 'privacy'." });

        var doc = await _db.LegalDocuments
            .AsNoTracking()
            .Where(d => d.DocumentType == normalized && d.IsActive)
            .OrderByDescending(d => d.PublishedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (doc is null)
            return NotFound(new { error = "Documento ainda não publicado." });

        return Ok(new { doc.DocumentType, doc.Version, doc.Content, doc.PublishedAtUtc });
    }
}
