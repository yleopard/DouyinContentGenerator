using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.TextGenerators;

/// <summary>
/// 百度文心一言 文案生成器
/// Auth: access_token via OAuth
/// </summary>
public class BaiduYiyanGenerator : ITextGenerator
{
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly string _model;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };

    public string ProviderName => "文心一言";

    public BaiduYiyanGenerator(string apiKey, string model = "ernie-4.0-turbo", string secretKey = "")
    {
        _apiKey = apiKey;
        _secretKey = string.IsNullOrEmpty(secretKey) ? apiKey : secretKey;
        _model = model;
    }

    public async Task<TextGenerationResult> GenerateAsync(TextGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var token = await GetAccessToken(ct);
            if (token == null) return new TextGenerationResult(Success: false, ErrorMessage: "百度access_token获取失败");

            var prompt = BuildPrompt(request);
            var payload = new { messages = new[] { new { role = "user", content = prompt } } };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/chat/{_model}?access_token={token}";

            var response = await _httpClient.PostAsync(url, content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<BaiduChatResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var text = result?.Result ?? "";
                var texts = ParseTexts(text);
                return new TextGenerationResult(Success: true, Texts: texts, Cost: 0.000003m);
            }
            return new TextGenerationResult(Success: false, ErrorMessage: $"文心API Error: {response.StatusCode}");
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

    public decimal GetCostPerToken() => 0.000003m;

    private async Task<string?> GetAccessToken(CancellationToken ct)
    {
        try
        {
            var url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={_apiKey}&client_secret={_secretKey}";
            var resp = await _httpClient.PostAsync(url, null, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<BaiduTokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.AccessToken;
        }
        catch { return null; }
    }

    private string BuildPrompt(TextGenerationRequest req)
    {
        var tpl = CopywritingTemplates.BuiltInTemplates.GetValueOrDefault(req.TemplateType, CopywritingTemplates.BuiltInTemplates["pain_point"]);
        var p = tpl;
        foreach (var kv in req.ProductInfo) p = p.Replace($"{{{kv.Key}}}", kv.Value);
        return p;
    }

    private static List<string> ParseTexts(string t) => t.Split("---").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
}

internal record BaiduChatResponse(string? Result);
internal record BaiduTokenResponse(string? AccessToken);
