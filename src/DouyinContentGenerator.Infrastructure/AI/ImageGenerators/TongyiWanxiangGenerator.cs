using System.Text;
using System.Text.Json;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.Infrastructure.AI.ImageGenerators;

public class TongyiWanxiangGenerator : IImageGenerator
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(120)
    };
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "通义万相";

    public TongyiWanxiangGenerator(string apiKey, string model = "wan2.1-t2i-turbo")
    {
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                model = _model,
                input = new
                {
                    prompt = request.Prompt,
                    reference_image_url = request.ReferenceImageUrl
                },
                parameters = new
                {
                    n = request.BatchSize,
                    size = request.Size,
                    style = request.Style
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                "https://dashscope.aliyuncs.com/api/v1/services/aigc/text2image/image-synthesis")
            {
                Content = content
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {_apiKey}");
            httpRequest.Headers.Add("X-DashScope-Async", "enable");

            var response = await _httpClient.SendAsync(httpRequest, ct);

            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync(ct);
                var result = JsonSerializer.Deserialize<TongyiResponse>(resultJson);

                var urls = result?.Output?.Results?.Select(r => r.Url).ToList() ?? new List<string>();
                var cost = CalculateCost(request.BatchSize, request.ReferenceImageUrl != null);

                return new ImageGenerationResult(
                    Success: true,
                    ImageUrls: urls,
                    Cost: cost
                );
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return new ImageGenerationResult(
                    Success: false,
                    ErrorMessage: $"API Error: {response.StatusCode} - {error}"
                );
            }
        }
        catch (OperationCanceledException)
        {
            return new ImageGenerationResult(Success: false, ErrorMessage: "Task cancelled");
        }
        catch (Exception ex)
        {
            return new ImageGenerationResult(Success: false, ErrorMessage: ex.Message);
        }
    }

    public async Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default)
    {
        try
        {
            var testRequest = new ImageGenerationRequest(Prompt: "test", BatchSize: 1);
            var result = await GenerateAsync(testRequest, ct);
            return result.Success || !(result.ErrorMessage?.Contains("authentication") == true);
        }
        catch
        {
            return false;
        }
    }

    public decimal GetCostPerImage(bool useReference = false)
    {
        var basePrice = _model switch
        {
            "wan2.1-t2i-turbo" => 0.18m,
            "wan2.1-t2i-plus" => 0.37m,
            _ => 0.18m
        };

        return basePrice * (useReference ? 1.2m : 1.0m);
    }

    private decimal CalculateCost(int batchSize, bool useReference)
    {
        return GetCostPerImage(useReference) * batchSize;
    }
}

internal record TongyiResponse(TongyiOutput? Output, string? Message);
internal record TongyiOutput(List<TongyiImageResult>? Results);
internal record TongyiImageResult(string Url);
