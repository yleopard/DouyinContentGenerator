namespace DouyinContentGenerator.Core.Interfaces;

/// <summary>
/// Creates AI generators with user-specific API keys from database settings.
/// </summary>
public interface IUserAIGeneratorFactory
{
    Task<(IImageGenerator Generator, string Provider, string Model)> CreateImageGeneratorAsync(Guid userId);
    Task<(ITextGenerator Generator, string Provider, string Model)> CreateTextGeneratorAsync(Guid userId);
}
