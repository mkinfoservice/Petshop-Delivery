using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Petshop.Api.Services.Enrichment;

public record DescriptionGenerationInput(
    string  Name,
    string? Brand,
    string? CategoryName,
    string? RecommendedPet,
    string? PetFoodType,
    string? ExistingDescription
);

/// <summary>
/// Gera descrições de produto via Anthropic Messages API (Claude Haiku).
/// Requer a env var Anthropic__ApiKey configurada — sem ela, o serviço fica inoperante
/// e a orquestração pula a geração (ver EnableDescriptionGeneration em EnrichmentConfig).
/// O prompt exige texto estritamente factual: nunca inventar propriedades, certificações
/// ou benefícios de saúde não informados, para evitar risco de propaganda enganosa.
/// </summary>
public sealed class ProductDescriptionGeneratorService
{
    private const string Model = "claude-haiku-4-5-20251001";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<ProductDescriptionGeneratorService> _logger;

    public ProductDescriptionGeneratorService(
        HttpClient http,
        IConfiguration config,
        ILogger<ProductDescriptionGeneratorService> logger)
    {
        _http    = http;
        _config  = config;
        _logger  = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_config["Anthropic:ApiKey"]);

    public async Task<string?> GenerateAsync(DescriptionGenerationInput input, CancellationToken ct)
    {
        var apiKey = _config["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Anthropic__ApiKey não configurada — geração de descrição pulada.");
            return null;
        }

        var facts = new StringBuilder();
        facts.AppendLine($"Nome do produto: {input.Name}");
        if (!string.IsNullOrWhiteSpace(input.Brand))          facts.AppendLine($"Marca: {input.Brand}");
        if (!string.IsNullOrWhiteSpace(input.CategoryName))   facts.AppendLine($"Categoria: {input.CategoryName}");
        if (!string.IsNullOrWhiteSpace(input.RecommendedPet)) facts.AppendLine($"Indicado para: {input.RecommendedPet}");
        if (!string.IsNullOrWhiteSpace(input.PetFoodType))    facts.AppendLine($"Tipo: {input.PetFoodType}");
        if (!string.IsNullOrWhiteSpace(input.ExistingDescription))
            facts.AppendLine($"Descrição atual (pode estar incompleta ou vazia): {input.ExistingDescription}");

        var systemPrompt =
            "Você escreve descrições curtas de produto para um catálogo de petshop/varejo, em português do Brasil. " +
            "Regras estritas: (1) use APENAS as informações fornecidas — nunca invente marca, peso, sabor, " +
            "composição nutricional, indicação de idade/porte, certificação, registro (MAPA/ANVISA) ou benefício " +
            "de saúde que não tenha sido informado; (2) não faça promessas terapêuticas ou médicas; " +
            "(3) escreva 2 a 4 frases, tom comercial objetivo, sem emojis, sem markdown; " +
            "(4) responda APENAS com o texto final da descrição, sem preâmbulo.";

        var requestBody = new
        {
            model      = Model,
            max_tokens = 300,
            system     = systemPrompt,
            messages   = new[]
            {
                new { role = "user", content = facts.ToString() }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning(
                "Falha ao gerar descrição via Anthropic ({Status}): {Body}",
                response.StatusCode, body);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<AnthropicMessageResponse>(cancellationToken: ct);
        var text = payload?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
        return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    private sealed class AnthropicMessageResponse
    {
        [JsonPropertyName("content")]
        public List<AnthropicContentBlock>? Content { get; set; }
    }

    private sealed class AnthropicContentBlock
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
