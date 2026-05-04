using System.ComponentModel.DataAnnotations;

namespace Petshop.Api.Entities.Master;

/// <summary>
/// Configuração global de WhatsApp da plataforma (singleton).
/// Usada por empresas com WhatsappMode = "platform".
/// Gerenciada pelo master admin.
/// </summary>
public class PlatformWhatsappConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(50)]
    public string? WabaId { get; set; }

    [MaxLength(50)]
    public string? PhoneNumberId { get; set; }

    /// <summary>Token criptografado com AES-256.</summary>
    public string? AccessTokenEncrypted { get; set; }

    [MaxLength(10)]
    public string TemplateLanguageCode { get; set; } = "pt_BR";

    /// <summary>JSON array de status que disparam notificação (ex: ["RECEBIDO","EM_PREPARO"]).</summary>
    public string? NotifyOnStatusesJson { get; set; }

    /// <summary>JSON object mapeando status → nome do template Meta (ex: {"RECEBIDO":"pedido_recebido"}).</summary>
    public string? NotificationTemplatesJson { get; set; }

    public bool IsActive { get; set; } = false;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
