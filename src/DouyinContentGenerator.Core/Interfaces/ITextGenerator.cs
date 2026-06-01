using DouyinContentGenerator.Core.DTOs;

namespace DouyinContentGenerator.Core.Interfaces;

public interface ITextGenerator
{
    string ProviderName { get; }

    Task<TextGenerationResult> GenerateAsync(TextGenerationRequest request, CancellationToken ct = default);

    Task<bool> ValidateConfigAsync(Dictionary<string, string> config, CancellationToken ct = default);

    decimal GetCostPerToken();
}
