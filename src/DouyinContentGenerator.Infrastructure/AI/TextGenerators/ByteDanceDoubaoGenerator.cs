using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.TextGenerators;

/// <summary>
/// 字节跳动 豆包 文案生成器（OpenAI兼容格式）
/// API: https://ark.cn-beijing.volces.com/api/v3/chat/completions
/// </summary>
public class ByteDanceDoubaoGenerator : ITextGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public string ProviderName => "豆包Doubao";

    public ByteDanceDoubaoGenerator(string apiKey, string model = "doubao-pro-32k")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://ark.cn-beijing.volces.com/api/v3/"),
            Timeout = TimeSpan.FromSeconds(60)
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
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.8,
                max_tokens = request.MaxLength
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("chat/completions", content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<DoubaoResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var text = result?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
                return new TextGenerationResult(Success: true, Texts: ParseTexts(text), Cost: 0.000002m);
            }
            return new TextGenerationResult(Success: false, ErrorMessage: $"豆包API Error: {response.StatusCode} - {Truncate(responseJson, 800)}");
        }
        catch (Exception ex) { return new TextGenerationResult(Success: false, ErrorMessage: ex.Message); }
    }

    public async Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default)
    {
        try
        {
            var r = await GenerateAsync(new TextGenerationRequest(
                ProductInfo: new Dictionary<string, string> { ["product_name"] = "test" }, TemplateType: "pain_point", MaxLength: 20), ct);
            return r.Success;
        }
        catch { return false; }
    }

    public decimal GetCostPerToken() => _model switch
    {
        "doubao-pro-32k" => 0.000002m,
        "doubao-lite-32k" => 0.000001m,
        _ => 0.000002m
    };

    private string BuildPrompt(TextGenerationRequest req)
    {
        var tpl = CopywritingTemplates.BuiltInTemplates.GetValueOrDefault(req.TemplateType, CopywritingTemplates.BuiltInTemplates["pain_point"]);
        var p = tpl;
        foreach (var kv in req.ProductInfo) p = p.Replace($"{{{kv.Key}}}", kv.Value);
        return p;
    }

    private static List<string> ParseTexts(string t) => t.Split("---").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "...";
}

internal record DoubaoResponse(List<DoubaoChoice>? Choices);
internal record DoubaoChoice(DoubaoMessage? Message);
internal record DoubaoMessage(string? Content);
