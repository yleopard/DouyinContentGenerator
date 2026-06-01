using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.ImageGenerators;

/// <summary>
/// 智谱AI CogView / GLM-Image 图片生成器
/// API: POST https://open.bigmodel.cn/api/paas/v4/images/generations
/// Docs: https://docs.bigmodel.cn/api-reference/模型-api/图像生成
/// </summary>
public class GlmImageGenerator : IImageGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public string ProviderName => "智谱CogView";

    // Supported sizes per model type
    private static readonly Dictionary<string, string[]> _glmImageSizes = new()
    {
        ["glm-image"] = new[] { "1280x1280", "1568x1056", "1056x1568", "1472x1088", "1088x1472", "1728x960", "960x1728" },
        ["cogview-4-250304"] = new[] { "1024x1024", "768x1344", "864x1152", "1344x768", "1152x864", "1440x720", "720x1440" },
        ["cogview-4"] = new[] { "1024x1024", "768x1344", "864x1152", "1344x768", "1152x864", "1440x720", "720x1440" },
        ["cogview-3-flash"] = new[] { "1024x1024", "768x1344", "864x1152", "1344x768", "1152x864", "1440x720", "720x1440" },
    };

    public GlmImageGenerator(string apiKey, string model = "glm-image")
    {
        _apiKey = apiKey;
        _model = model;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://open.bigmodel.cn/api/paas/v4/"),
            Timeout = TimeSpan.FromSeconds(180)
        };
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DouyinContentGenerator/1.0");
    }

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var size = NormalizeSize(request.Size, _model);
            var quality = _model == "glm-image" ? "hd" : "standard";

            var payload = new Dictionary<string, object>
            {
                ["model"] = _model,
                ["prompt"] = request.Prompt,
                ["size"] = size,
                ["quality"] = quality,
                ["watermark_enabled"] = false // 关闭水印用于电商场景
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("images/generations", content, ct);

            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<CogViewResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var urls = result?.Data?.Select(d => d.Url).Where(u => !string.IsNullOrEmpty(u)).ToList()
                    ?? new List<string>();
                
                if (urls.Count == 0)
                    return new ImageGenerationResult(Success: false, ErrorMessage: $"API返回成功但未生成图片: {Truncate(responseJson, 800)}");
                
                var cost = GetCostPerImage(false);
                return new ImageGenerationResult(Success: true, ImageUrls: urls, Cost: cost);
            }
            else
            {
                return new ImageGenerationResult(Success: false,
                    ErrorMessage: $"API Error: {response.StatusCode} - {Truncate(responseJson, 800)}");
            }
        }
        catch (OperationCanceledException) { return new ImageGenerationResult(Success: false, ErrorMessage: "Task cancelled"); }
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

    public decimal GetCostPerImage(bool useReference = false) => _model switch
    {
        "glm-image" => 0.15m,
        "cogview-4-250304" => 0.25m,
        "cogview-4" => 0.30m,
        "cogview-3-flash" => 0.10m,
        _ => 0.15m
    };

    /// <summary>Normalize size to model-supported format</summary>
    internal static string NormalizeSize(string? size, string model)
    {
        if (string.IsNullOrWhiteSpace(size)) return GetDefaultSize(model);
        size = size.Replace('*', 'x').Replace('X', 'x');

        // Check if it matches a supported size
        var supported = _glmImageSizes.GetValueOrDefault(model, _glmImageSizes["cogview-3-flash"]);
        if (supported.Contains(size)) return size;

        return GetDefaultSize(model);
    }

    private static string GetDefaultSize(string model)
        => model == "glm-image" ? "1280x1280" : "1024x1024";

    private static string Truncate(string s, int maxLen)
        => s.Length <= maxLen ? s : s[..maxLen] + "...";
}

internal record CogViewResponse(long Created, List<CogViewData>? Data);
internal record CogViewData(string? Url);
