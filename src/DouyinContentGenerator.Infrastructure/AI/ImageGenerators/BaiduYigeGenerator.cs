using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.ImageGenerators;

/// <summary>
/// 百度文心一格 图片生成器
/// API: POST https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/text2image/sd_xl
/// Auth: access_token in query string
/// </summary>
public class BaiduYigeGenerator : IImageGenerator
{
    private readonly string _apiKey;
    private readonly string _secretKey;
    private readonly string _model;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(120) };

    public string ProviderName => "文心一格";

    /// <param name="apiKey">Baidu API Key (client_id)</param>
    /// <param name="secretKey">Baidu Secret Key (client_secret), pass as second API key field or same string</param>
    public BaiduYigeGenerator(string apiKey, string model = "sd_xl", string secretKey = "")
    {
        _apiKey = apiKey;
        _secretKey = string.IsNullOrEmpty(secretKey) ? apiKey : secretKey;
        _model = model;
    }

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var token = await GetAccessToken(ct);
            if (token == null)
                return new ImageGenerationResult(Success: false, ErrorMessage: "百度access_token获取失败，请检查API Key/Secret Key");

            var payload = new { prompt = request.Prompt, size = NormalizeSize(request.Size), num = 1 };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"https://aip.baidubce.com/rpc/2.0/ai_custom/v1/wenxinworkshop/text2image/{_model}?access_token={token}";

            var response = await _httpClient.PostAsync(url, content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<BaiduImageResponse>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var urls = result?.Data?.Select(d => d.Url).Where(u => !string.IsNullOrEmpty(u)).ToList() ?? new List<string>();
                if (urls.Count > 0)
                    return new ImageGenerationResult(Success: true, ImageUrls: urls!, Cost: 0.20m);
                return new ImageGenerationResult(Success: false, ErrorMessage: $"百度API返回但无图片: {Truncate(responseJson, 800)}");
            }
            return new ImageGenerationResult(Success: false, ErrorMessage: $"百度API Error: {response.StatusCode} - {Truncate(responseJson, 800)}");
        }
        catch (Exception ex) { return new ImageGenerationResult(Success: false, ErrorMessage: ex.Message); }
    }

    public async Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default)
    {
        try
        {
            var result = await GenerateAsync(new ImageGenerationRequest(Prompt: "test", BatchSize: 1), ct);
            return result.Success;
        }
        catch { return false; }
    }

    public decimal GetCostPerImage(bool useReference = false) => 0.20m;

    private async Task<string?> GetAccessToken(CancellationToken ct)
    {
        try
        {
            var url = $"https://aip.baidubce.com/oauth/2.0/token?grant_type=client_credentials&client_id={_apiKey}&client_secret={_secretKey}";
            var resp = await _httpClient.PostAsync(url, null, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<BaiduTokenResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.AccessToken;
        }
        catch { return null; }
    }

    private static string NormalizeSize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "1024x1024";
        return s.Replace('*', 'x');
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "...";
}

internal record BaiduImageResponse(List<BaiduImageData>? Data);
internal record BaiduImageData(string? Url);
internal record BaiduTokenResponse(string? AccessToken);
