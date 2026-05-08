using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.StoreFront;

namespace Petshop.Api.Services.Branding;

public sealed class TenantBrandingService
{
    private static readonly Regex HexColorRegex = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;

    public TenantBrandingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TenantBrandingSnapshot> ResolveAsync(Guid companyId, CancellationToken ct)
    {
        var companyName = await _db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);

        var config = await _db.StoreFrontConfigs
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .FirstOrDefaultAsync(ct);

        return TenantBrandingSnapshot.From(companyName, config);
    }

    internal static string NormalizeHex(string? value, string fallback)
    {
        var color = value?.Trim();
        return !string.IsNullOrWhiteSpace(color) && HexColorRegex.IsMatch(color)
            ? color
            : fallback;
    }
}

public sealed record TenantBrandingSnapshot(
    string StoreName,
    string? StoreSlogan,
    string? LogoUrl,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string TextColor,
    string TextMutedColor,
    string BorderColor)
{
    public static TenantBrandingSnapshot Default { get; } = new(
        "vendApps",
        null,
        null,
        "#111827",
        "#1E3A8A",
        "#FFA726",
        "#111827",
        "#6b7280",
        "#e5e7eb");

    public static TenantBrandingSnapshot From(string? companyName, StoreFrontConfig? config)
    {
        var storeName = FirstNonEmpty(config?.StoreName, companyName, Default.StoreName);

        return new TenantBrandingSnapshot(
            storeName,
            string.IsNullOrWhiteSpace(config?.StoreSlogan) ? null : config.StoreSlogan.Trim(),
            string.IsNullOrWhiteSpace(config?.LogoUrl) ? null : config.LogoUrl.Trim(),
            TenantBrandingService.NormalizeHex(config?.PrimaryColor, Default.PrimaryColor),
            TenantBrandingService.NormalizeHex(config?.SecondaryColor, Default.SecondaryColor),
            TenantBrandingService.NormalizeHex(config?.AccentColor, Default.AccentColor),
            TenantBrandingService.NormalizeHex(config?.TextColor, Default.TextColor),
            TenantBrandingService.NormalizeHex(config?.TextMutedColor, Default.TextMutedColor),
            TenantBrandingService.NormalizeHex(config?.BorderColor, Default.BorderColor));
    }

    public static TenantBrandingSnapshot Fallback(string? storeName) =>
        Default with { StoreName = FirstNonEmpty(storeName, Default.StoreName) };

    public byte[]? TryDecodeDataUriLogo()
    {
        if (string.IsNullOrWhiteSpace(LogoUrl))
            return null;

        var value = LogoUrl.Trim();
        if (!value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return null;

        var commaIndex = value.IndexOf(',');
        if (commaIndex < 0 || !value[..commaIndex].Contains(";base64", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            return Convert.FromBase64String(value[(commaIndex + 1)..]);
        }
        catch
        {
            return null;
        }
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values
            .Select(v => v?.Trim())
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
        ?? Default.StoreName;
}
