using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.TextGenerators;

/// <summary>
/// 智谱AI GLM 文案生成器（OpenAI兼容格式）
/// API: https://open.bigmodel.cn/api/paas/v4/chat/completions
/// </summary>
public class GlmTextGenerator : ITextGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public string ProviderName => "智谱GLM";

    public GlmTextGenerator(string apiKey, string model = "glm-4-flash")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://open.bigmodel.cn/api/paas/v4/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DouyinContentGenerator/1.0");
    }

    public async Task<TextGenerationResult> GenerateAsync(TextGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var prompt = BuildPrompt(request);

            var payload = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                temperature = 0.8,
                max_tokens = request.MaxLength
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("chat/completions", content, ct);

            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<GlmChatResponse>(resultJson);
                var text = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
                var texts = ParseTexts(text);
                var cost = CalculateCost(result?.Usage?.TotalTokens ?? prompt.Length);
                return new TextGenerationResult(Success: true, Texts: texts, Cost: cost);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new TextGenerationResult(Success: false, ErrorMessage: $"API Error: {response.StatusCode} - {error}");
            }
        }
        catch (OperationCanceledException) { return new TextGenerationResult(Success: false, ErrorMessage: "Task cancelled"); }
        catch (Exception ex) { return new TextGenerationResult(Success: false, ErrorMessage: ex.Message); }
    }

    public async Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default)
    {
        try
        {
            var testReq = new TextGenerationRequest(
                ProductInfo: new Dictionary<string, string> { ["product_name"] = "test" },
                TemplateType: "pain_point", MaxLength: 50);
            var result = await GenerateAsync(testReq, ct);
            return result.Success;
        }
        catch { return false; }
    }

    public decimal GetCostPerToken() => _model switch
    {
        "glm-4-flash" => 0.000001m,
        "glm-4-plus" => 0.000014m,
        "glm-4-air" => 0.000001m,
        _ => 0.000001m
    };

    private string BuildPrompt(TextGenerationRequest request)
    {
        var template = CopywritingTemplates.BuiltInTemplates.GetValueOrDefault(
            request.TemplateType, CopywritingTemplates.BuiltInTemplates["pain_point"]);
        var prompt = template;
        foreach (var kvp in request.ProductInfo)
            prompt = prompt.Replace($"{{{kvp.Key}}}", kvp.Value);
        return prompt;
    }

    private static List<string> ParseTexts(string text)
        => text.Split("---").Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

    private decimal CalculateCost(int tokenCount) => tokenCount * GetCostPerToken();
}

internal record GlmChatResponse(List<GlmChoice>? Choices, GlmUsage? Usage);
internal record GlmChoice(GlmMessage? Message);
internal record GlmMessage(string? Content);
internal record GlmUsage(int TotalTokens);
