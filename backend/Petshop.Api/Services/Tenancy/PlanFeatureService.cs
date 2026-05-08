using Microsoft.EntityFrameworkCore;
using Petshop.Api.Data;
using Petshop.Api.Entities.Catalog;

namespace Petshop.Api.Services.Tenancy;

public static class AppFeatureKeys
{
    public const string Agenda = "agenda";
    public const string Commissions = "commissions";
    public const string Tips = "tips";
    public const string DavMenu = "dav_menu";
    public const string FinancialMenu = "financial_menu";
    public const string LoyaltyProgram = "loyalty_program";
    public const string AccountingEmailDispatch = "accounting_email_dispatch";
    /// <summary>Exibe módulos de entrega própria (Rotas, Entregadores). false = entrega somente via marketplace.</summary>
    public const string OwnDelivery = "own_delivery";
    public const string ModernCatalogExperience = "modern_catalog_experience";
    /// <summary>Habilita campos de endereço + geocoding no cadastro de clientes via atendimento telefônico.</summary>
    public const string CustomerAddressGeocoding = "customer_address_geocoding";
}

public sealed record FeatureDefinition(
    string Key,
    string Label,
    string Description,
    string Group,
    string DefaultPlan,
    string RiskLevel,
    bool RequiresExplicitOptIn = false);

public sealed record ResolvedFeatureDefinition(
    string Key,
    bool Enabled,
    bool DefaultEnabled,
    bool? OverrideEnabled,
    string Source,
    FeatureDefinition Definition);

public class PlanFeatureService
{
    private readonly AppDbContext _db;

    public PlanFeatureService(AppDbContext db)
    {
        _db = db;
    }

    private static readonly FeatureDefinition[] FeatureDefinitions =
    [
        new(
            AppFeatureKeys.Agenda,
            "Agenda",
            "Habilita agenda de servicos e fluxo de compromissos.",
            "operation",
            "pro",
            "medium"),
        new(
            AppFeatureKeys.Commissions,
            "Comissoes",
            "Habilita regras e consultas de comissao.",
            "financial",
            "trial",
            "medium"),
        new(
            AppFeatureKeys.Tips,
            "Gorjetas",
            "Habilita controle de gorjetas e rateios.",
            "financial",
            "trial",
            "medium"),
        new(
            AppFeatureKeys.DavMenu,
            "Orcamento / DAV",
            "Exibe e habilita a experiencia de orcamentos/DAV.",
            "sales",
            "always",
            "high"),
        new(
            AppFeatureKeys.FinancialMenu,
            "Financeiro",
            "Exibe o modulo financeiro.",
            "financial",
            "trial",
            "high"),
        new(
            AppFeatureKeys.LoyaltyProgram,
            "Fidelidade",
            "Habilita pontuacao, resgates e comunicacoes de fidelidade.",
            "customer",
            "trial",
            "medium"),
        new(
            AppFeatureKeys.AccountingEmailDispatch,
            "Envio contabil",
            "Habilita fechamento contabil e envio automatico por email.",
            "accounting",
            "pro",
            "high"),
        new(
            AppFeatureKeys.OwnDelivery,
            "Entrega propria",
            "Exibe rotas, entregadores e fluxo de entrega propria.",
            "delivery",
            "always",
            "high"),
        new(
            AppFeatureKeys.ModernCatalogExperience,
            "Catalogo moderno",
            "Habilita a experiencia moderna do catalogo/storefront.",
            "storefront",
            "never",
            "low",
            RequiresExplicitOptIn: true),
        new(
            AppFeatureKeys.CustomerAddressGeocoding,
            "Geocoding no cliente",
            "Habilita endereco e geocoding no cadastro de cliente do atendimento.",
            "customer",
            "never",
            "medium",
            RequiresExplicitOptIn: true),
    ];

    public static readonly string[] SupportedFeatures = FeatureDefinitions
        .Select(f => f.Key)
        .ToArray();

    public static IReadOnlyList<FeatureDefinition> GetDefinitions() => FeatureDefinitions;

    public async Task<Dictionary<string, bool>> ResolveFeaturesAsync(Company company, CancellationToken ct = default)
    {
        var features = BuildPlanDefaults(company.Plan);

        var overrides = await _db.CompanyFeatureOverrides
            .AsNoTracking()
            .Where(f => f.CompanyId == company.Id)
            .ToListAsync(ct);

        foreach (var ov in overrides)
        {
            features[ov.FeatureKey] = ov.IsEnabled;
        }

        return features;
    }

    public async Task<IReadOnlyList<ResolvedFeatureDefinition>> ResolveFeatureDefinitionsAsync(
        Company company,
        CancellationToken ct = default)
    {
        var defaults = BuildPlanDefaults(company.Plan);
        var overrides = await _db.CompanyFeatureOverrides
            .AsNoTracking()
            .Where(f => f.CompanyId == company.Id)
            .ToDictionaryAsync(f => f.FeatureKey, f => f.IsEnabled, StringComparer.OrdinalIgnoreCase, ct);

        return FeatureDefinitions
            .Select(def =>
            {
                var defaultEnabled = defaults.GetValueOrDefault(def.Key);
                var hasOverride = overrides.TryGetValue(def.Key, out var overrideEnabled);
                var enabled = hasOverride ? overrideEnabled : defaultEnabled;
                return new ResolvedFeatureDefinition(
                    def.Key,
                    enabled,
                    defaultEnabled,
                    hasOverride ? overrideEnabled : null,
                    hasOverride ? "override" : "plan",
                    def);
            })
            .ToList();
    }

    public static Dictionary<string, bool> BuildPlanDefaults(string? plan)
    {
        return FeatureDefinitions.ToDictionary(
            def => def.Key,
            def => IsEnabledByDefault(plan, def.DefaultPlan),
            StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsFeatureKeySupported(string key) =>
        SupportedFeatures.Contains((key ?? "").Trim(), StringComparer.OrdinalIgnoreCase);

    public static bool IsPlanAtLeast(string? plan, string minPlan)
    {
        return PlanRank(plan) >= PlanRank(minPlan);
    }

    private static bool IsEnabledByDefault(string? plan, string defaultPlan)
    {
        return defaultPlan.Trim().ToLowerInvariant() switch
        {
            "always" => true,
            "never" => false,
            _ => IsPlanAtLeast(plan, defaultPlan)
        };
    }

    private static int PlanRank(string? plan)
    {
        return (plan ?? "").Trim().ToLowerInvariant() switch
        {
            "trial" => 1,
            "starter" => 2,
            "pro" => 3,
            "enterprise" => 4,
            _ => 1
        };
    }
}
