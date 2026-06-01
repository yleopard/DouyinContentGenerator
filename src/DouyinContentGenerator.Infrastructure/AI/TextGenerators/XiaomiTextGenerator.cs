using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.TextGenerators;

/// <summary>
/// 小米 MiMo 文案生成器（Anthropic兼容格式）
/// API: https://api.xiaomimimo.com/anthropic/v1/messages
/// Auth: api-key header
/// </summary>
public class XiaomiTextGenerator : ITextGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public string ProviderName => "小米MiMo";

    public XiaomiTextGenerator(string apiKey, string model = "mimo-v2.5-pro")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.xiaomimimo.com/anthropic/v1/"),
            Timeout = TimeSpan.FromSeconds(120)
        };
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
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
                max_tokens = request.MaxLength,
                system = "You are a professional e-commerce copywriter. Write compelling product descriptions in Chinese.",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new[]
                        {
                            new { type = "text", text = prompt }
                        }
                    }
                },
                temperature = 0.8,
                top_p = 0.95,
                stream = false
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("messages", content, ct);

            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<MiMoResponse>(responseJson, new JsonSerializerOptions
                { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                var text = result?.GetContentText() ?? "";

                if (string.IsNullOrWhiteSpace(text))
                    return new TextGenerationResult(Success: false,
                        ErrorMessage: $"MiMo返回成功但无内容，原始响应: {Truncate(responseJson, 800)}");

                var texts = ParseTexts(text);
                var tokens = (result?.Usage?.InputTokens ?? 0) + (result?.Usage?.OutputTokens ?? 0);
                var cost = CalculateCost(tokens > 0 ? tokens : prompt.Length);
                return new TextGenerationResult(Success: true, Texts: texts, Cost: cost);
            }
            else
            {
                return new TextGenerationResult(Success: false, ErrorMessage: $"API Error: {response.StatusCode} - {responseJson}");
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
        "mimo-v2.5-pro" => 0.000002m,
        "mimo-v2.5-flash" => 0.000001m,
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

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}

// MiMo Anthropic-compatible response models
internal record MiMoResponse(string Id, string Model, List<MiMoContentBlock>? Content, MiMoUsageInfo? Usage);
internal record MiMoContentBlock(string Type, string? Text);
internal record MiMoUsageInfo(int InputTokens, int OutputTokens);

internal static class MiMoResponseExtensions
{
    public static string GetContentText(this MiMoResponse response)
        => string.Join("\n", response.Content?.Where(c => c.Type == "text").Select(c => c.Text ?? "") ?? Array.Empty<string>());
}
