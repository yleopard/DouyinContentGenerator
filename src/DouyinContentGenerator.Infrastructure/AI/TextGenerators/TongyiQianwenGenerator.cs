using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.TextGenerators;

public class TongyiQianwenGenerator : ITextGenerator
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly HttpClient _httpClient;

    public string ProviderName => "通义千问";

    public TongyiQianwenGenerator(string apiKey, string model = "qwen-turbo")
    {
        _apiKey = apiKey;
        _model = model;

        _httpClient = new HttpClient
        {
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
                input = new
                {
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                },
                parameters = new
                {
                    temperature = 0.8,
                    max_tokens = request.MaxLength
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation",
                content,
                ct
            );

            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<QwenResponse>(resultJson);

                var texts = ParseTexts(result?.Output?.Text ?? "");
                var cost = CalculateCost(prompt.Length + (result?.Output?.Text?.Length ?? 0));

                return new TextGenerationResult(
                    Success: true,
                    Texts: texts,
                    Cost: cost
                );
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new TextGenerationResult(
                    Success: false,
                    ErrorMessage: $"API Error: {response.StatusCode} - {error}"
                );
            }
        }
        catch (OperationCanceledException)
        {
            return new TextGenerationResult(Success: false, ErrorMessage: "Task cancelled");
        }
        catch (Exception ex)
        {
            return new TextGenerationResult(Success: false, ErrorMessage: ex.Message);
        }
    }

    public async Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default)
    {
        try
        {
            var testRequest = new TextGenerationRequest(
                ProductInfo: new Dictionary<string, string> { ["product_name"] = "测试产品" },
                TemplateType: "pain_point"
            );
            var result = await GenerateAsync(testRequest, ct);
            return result.Success;
        }
        catch
        {
            return false;
        }
    }

    public decimal GetCostPerToken()
    {
        return _model switch
        {
            "qwen-turbo" => 0.000002m,
            "qwen-plus" => 0.000004m,
            _ => 0.000002m
        };
    }

    private string BuildPrompt(TextGenerationRequest request)
    {
        var template = CopywritingTemplates.BuiltInTemplates.GetValueOrDefault(
            request.TemplateType,
            CopywritingTemplates.BuiltInTemplates["pain_point"]
        );

        var prompt = template;
        foreach (var kvp in request.ProductInfo)
        {
            prompt = prompt.Replace($"{{{kvp.Key}}}", kvp.Value);
        }

        return prompt;
    }

    private List<string> ParseTexts(string text)
    {
        return text.Split("---")
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();
    }

    private decimal CalculateCost(int charCount)
    {
        var tokenCount = (int)(charCount / 1.5);
        return tokenCount * GetCostPerToken();
    }
}

internal record QwenResponse(QwenOutput? Output, string? Message);
internal record QwenOutput(string? Text);
