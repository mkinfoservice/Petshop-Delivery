using System.ComponentModel.DataAnnotations;
using Petshop.Api.Entities.Catalog;

namespace Petshop.Api.Entities.StoreFront;

/// <summary>
/// Configuração visual da loja online — 1:1 com Company.
/// Criada automaticamente com defaults no primeiro acesso.
/// </summary>
public class StoreFrontConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    // ── Branding ──────────────────────────────────────────────────────────────
    /// <summary>URL ou data URI da logo da loja.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Nome da loja exibido no header.</summary>
    [MaxLength(120)]
    public string? StoreName { get; set; }

    /// <summary>Slogan / subtítulo exibido abaixo do nome.</summary>
    [MaxLength(200)]
    public string? StoreSlogan { get; set; }

    // ── Visual ────────────────────────────────────────────────────────────────
    /// <summary>Cor primária da marca em hexadecimal (ex: "#6366f1").</summary>
    [MaxLength(20)]
    public string PrimaryColor { get; set; } = "#6366f1";

    /// <summary>Cor de fundo da página (ex: "#ffffff").</summary>
    [MaxLength(50)]
    public string BgColor { get; set; } = "#ffffff";

    /// <summary>Cor de fundo secundária — cards, faixas (ex: "#f3f4f6").</summary>
    [MaxLength(50)]
    public string Surface2Color { get; set; } = "#f3f4f6";

    /// <summary>Cor das bordas e divisores (ex: "rgba(0,0,0,0.08)").</summary>
    [MaxLength(50)]
    public string BorderColor { get; set; } = "rgba(0,0,0,0.08)";

    /// <summary>Cor do texto principal (ex: "#111827").</summary>
    [MaxLength(20)]
    public string TextColor { get; set; } = "#111827";

    /// <summary>Cor do texto secundário/muted (ex: "#6b7280").</summary>
    [MaxLength(20)]
    public string TextMutedColor { get; set; } = "#6b7280";

    /// <summary>Cor secundária da marca (ex: navy, usada em headers e profundidade).</summary>
    [MaxLength(20)]
    public string SecondaryColor { get; set; } = "#6366f1";

    /// <summary>Cor de destaque/acento (ex: amarelo dourado, usado em badges e promoções).</summary>
    [MaxLength(20)]
    public string AccentColor { get; set; } = "#f59e0b";

    // ── Banner ────────────────────────────────────────────────────────────────
    /// <summary>Intervalo (segundos) entre slides. 0 = sem auto-rotação.</summary>
    public int BannerIntervalSecs { get; set; } = 5;

    // ── Announcement bar ──────────────────────────────────────────────────────
    /// <summary>JSON array de mensagens exibidas na faixa superior. Ex: ["Frete Grátis acima de R$ 100","Parcele em 12x"]</summary>
    public string AnnouncementsJson { get; set; } = """["Frete Grátis acima de R$ 100"]""";

    public DateTime? UpdatedAtUtc { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public ICollection<BannerSlide> BannerSlides { get; set; } = new List<BannerSlide>();
}
