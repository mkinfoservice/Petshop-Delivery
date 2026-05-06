using System.ComponentModel.DataAnnotations;

namespace Petshop.Api.Contracts.Admin.StoreFront;

// ── Responses ────────────────────────────────────────────────────────────────

public record BannerSlideResponse(
    Guid   Id,
    string? ImageUrl,
    string? Title,
    string? Subtitle,
    string? CtaText,
    string  CtaType,
    string? CtaTarget,
    bool    CtaNewTab,
    int     SortOrder,
    bool    IsActive);

public record StoreFrontConfigResponse(
    Guid   Id,
    string PrimaryColor,
    int    BannerIntervalSecs,
    string? LogoUrl,
    string? StoreName,
    string? StoreSlogan,
    IReadOnlyList<string> Announcements,
    IReadOnlyList<BannerSlideResponse> Slides,
    Guid   CompanyId      = default,
    string BgColor        = "#ffffff",
    string Surface2Color  = "#f3f4f6",
    string BorderColor    = "rgba(0,0,0,0.08)",
    string TextColor      = "#111827",
    string TextMutedColor = "#6b7280",
    string SecondaryColor = "#6366f1",
    string AccentColor    = "#f59e0b");

// ── Requests — config geral ───────────────────────────────────────────────────

public record UpdateStoreFrontConfigRequest(
    [MaxLength(20)]  string? PrimaryColor,
    int?             BannerIntervalSecs,
    string?          LogoUrl,
    [MaxLength(120)] string? StoreName,
    [MaxLength(200)] string? StoreSlogan,
    IReadOnlyList<string>? Announcements,
    [MaxLength(50)]  string? BgColor,
    [MaxLength(50)]  string? Surface2Color,
    [MaxLength(50)]  string? BorderColor,
    [MaxLength(20)]  string? TextColor,
    [MaxLength(20)]  string? TextMutedColor,
    [MaxLength(20)]  string? SecondaryColor,
    [MaxLength(20)]  string? AccentColor);

// ── Requests — slides ─────────────────────────────────────────────────────────

public record UpsertBannerSlideRequest(
    string?          ImageUrl,   // URL ou data URI base64 — sem limite
    [MaxLength(120)] string? Title,
    [MaxLength(200)] string? Subtitle,
    [MaxLength(60)]  string? CtaText,
    [MaxLength(20)]  string? CtaType,    // none | category | product | external
    [MaxLength(500)] string? CtaTarget,
    bool?   CtaNewTab,
    int?    SortOrder,
    bool?   IsActive);

public record ReorderSlidesRequest(IReadOnlyList<Guid> OrderedIds);
