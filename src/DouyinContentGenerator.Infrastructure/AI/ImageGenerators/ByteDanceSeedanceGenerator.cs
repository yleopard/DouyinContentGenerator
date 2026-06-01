using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.ImageGenerators;

/// <summary>
/// 字节跳动 豆包 Seedream 图片生成器
/// API: POST https://ark.cn-beijing.volces.com/api/v3/images/generate
/// Model: doubao-seedream-4-0-250828
/// Docs: https://github.com/xujfcn/seedream-guide
/// </summary>
public class ByteDanceSeedanceGenerator : IImageGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public string ProviderName => "即梦Seedance";

    public ByteDanceSeedanceGenerator(string apiKey, string model = "doubao-seedream-4-0-250828")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://ark.cn-beijing.volces.com/api/v3/"),
            Timeout = TimeSpan.FromSeconds(180)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DouyinContentGenerator/1.0");
    }

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var size = NormalizeSize(request.Size);
            var payload = new
            {
                model = _model,
                prompt = request.Prompt,
                size,
                response_format = "url",
                watermark = false
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("images/generations", content, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                // Flexible parsing: try multiple known response formats
                var urls = new List<string>();
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Format 1: { data: [{ url: "..." }] }
                if (root.TryGetProperty("data", out var dataArr) && dataArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArr.EnumerateArray())
                    {
                        if (item.TryGetProperty("url", out var urlEl) || item.TryGetProperty("image_url", out urlEl))
                            if (urlEl.ValueKind == JsonValueKind.String) urls.Add(urlEl.GetString()!);
                    }
                }
                // Format 2: { image_url: "..." }
                if (urls.Count == 0 && (root.TryGetProperty("url", out var directUrl) || root.TryGetProperty("image_url", out directUrl)))
                    if (directUrl.ValueKind == JsonValueKind.String) urls.Add(directUrl.GetString()!);

                if (urls.Count > 0)
                    return new ImageGenerationResult(Success: true, ImageUrls: urls, Cost: _model.Contains("4-0") ? 0.25m : 0.15m);

                return new ImageGenerationResult(Success: false,
                    ErrorMessage: $"Seedance返回但无图片: {Truncate(responseJson, 800)}");
            }
            return new ImageGenerationResult(Success: false,
                ErrorMessage: $"Seedance API Error: {response.StatusCode} - {Truncate(responseJson, 800)}");
        }
        catch (Exception ex) { return new ImageGenerationResult(Success: false, ErrorMessage: ex.Message); }
    }

    public async Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default)
    {
        try
        {
            var r = await GenerateAsync(new ImageGenerationRequest(Prompt: "一只简单的红色水杯", BatchSize: 1), ct);
            return r.Success;
        }
        catch { return false; }
    }

    public decimal GetCostPerImage(bool useReference = false) => 0.25m;

    private static string NormalizeSize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "1K";
        s = s.Replace('*', 'x');
        return s switch
        {
            "1024x1024" => "1K",
            "2048x2048" => "2K",
            "4096x4096" => "4K",
            _ => s
        };
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "...";
}

internal record SeedanceResponse(List<SeedanceData>? Data);
internal record SeedanceData(string? Url, string? RevisedPrompt);
