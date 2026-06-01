using DouyinContentGenerator.Core.DTOs;

namespace DouyinContentGenerator.Core.Interfaces;

public interface IImageGenerator
{
    string ProviderName { get; }

    Task<ImageGenerationResult> GenerateAsync(ImageGenerationRequest request, CancellationToken ct = default);

    Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default);

    decimal GetCostPerImage(bool useReference = false);
}
