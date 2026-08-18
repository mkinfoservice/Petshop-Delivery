using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Master;
using Petshop.Api.Services.Master;

namespace Petshop.Api.Controllers;

/// <summary>
/// CRUD dos documentos legais da plataforma (Termos de Uso / Política de Privacidade).
/// Publicar uma versão nova desativa a anterior — histórico fica preservado (IsActive=false),
/// nunca é apagado.
/// </summary>
[ApiController]
[Route("master/legal")]
[Authorize(Roles = "master_admin")]
public class MasterLegalController : ControllerBase
{
    private static readonly string[] ValidTypes = ["terms", "privacy"];

    private readonly AppDbContext _db;
    private readonly MasterAuditService _audit;

    public MasterLegalController(AppDbContext db, MasterAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet("{type}")]
    public async Task<IActionResult> GetActive(string type, CancellationToken ct)
    {
        var normalized = type.Trim().ToLowerInvariant();
        if (!ValidTypes.Contains(normalized))
            return BadRequest(new { error = "Tipo inválido. Use 'terms' ou 'privacy'." });

        var doc = await _db.LegalDocuments
            .AsNoTracking()
            .Where(d => d.DocumentType == normalized && d.IsActive)
            .OrderByDescending(d => d.PublishedAtUtc)
            .FirstOrDefaultAsync(ct);

        return Ok(doc);
    }

    [HttpGet("{type}/history")]
    public async Task<IActionResult> GetHistory(string type, CancellationToken ct)
    {
        var normalized = type.Trim().ToLowerInvariant();
        if (!ValidTypes.Contains(normalized))
            return BadRequest(new { error = "Tipo inválido. Use 'terms' ou 'privacy'." });

        var docs = await _db.LegalDocuments
            .AsNoTracking()
            .Where(d => d.DocumentType == normalized)
            .OrderByDescending(d => d.PublishedAtUtc)
            .ToListAsync(ct);

        return Ok(docs);
    }

    [HttpPost("{type}")]
    public async Task<IActionResult> Publish(
        string type,
        [FromBody] PublishLegalDocumentRequest req,
        CancellationToken ct)
    {
        var normalized = type.Trim().ToLowerInvariant();
        if (!ValidTypes.Contains(normalized))
            return BadRequest(new { error = "Tipo inválido. Use 'terms' ou 'privacy'." });

        if (string.IsNullOrWhiteSpace(req.Version) || string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { error = "Version e Content são obrigatórios." });

        var current = await _db.LegalDocuments
            .Where(d => d.DocumentType == normalized && d.IsActive)
            .ToListAsync(ct);
        foreach (var d in current)
            d.IsActive = false;

        var newDoc = new LegalDocument
        {
            DocumentType = normalized,
            Version = req.Version.Trim(),
            Content = req.Content,
            IsActive = true,
            PublishedAtUtc = DateTime.UtcNow,
        };
        _db.LegalDocuments.Add(newDoc);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(User, GetIp(), "legal.publish", "legal_document",
            newDoc.Id.ToString(), $"{normalized} v{newDoc.Version}",
            new { normalized, newDoc.Version }, ct);

        return Ok(newDoc);
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public record PublishLegalDocumentRequest(string Version, string Content);
