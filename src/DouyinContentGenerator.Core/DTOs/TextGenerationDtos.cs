namespace DouyinContentGenerator.Core.DTOs;

public record TextGenerationRequest(
    Dictionary<string, string> ProductInfo,
    string TemplateType,
    string Tone = "friendly",
    int MaxLength = 300
);

public record TextGenerationResult(
    bool Success,
    List<string>? Texts = null,
    string? ErrorMessage = null,
    decimal Cost = 0
);
