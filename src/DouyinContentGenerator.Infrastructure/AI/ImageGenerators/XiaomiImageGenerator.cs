using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.ImageGenerators;

/// <summary>
/// 小米 MiMo 图片生成器 — MiMo 不支持图片生成
/// </summary>
public class XiaomiImageGenerator : IImageGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public string ProviderName => "小米MiMo";

    public XiaomiImageGenerator(string apiKey, string model = "mimo-v2.5-pro")
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

    public Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default)
        => Task.FromResult(new ImageGenerationResult(Success: false, ErrorMessage: "MiMo不支持图片生成，请使用通义万相或CogView"));

    public async Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                model = _model,
                max_tokens = 10,
                messages = new[] { new { role = "user", content = new[] { new { type = "text", text = "Hi" } } } },
                stream = false
            };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("messages", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public decimal GetCostPerImage(bool useReference = false) => 0.15m;
}
