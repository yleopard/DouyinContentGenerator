namespace DouyinContentGenerator.Core.DTOs;

public record ImageGenerationRequest(
    string Prompt,
    string Style = "realistic",
    string Size = "1024x1024",
    int BatchSize = 1,
    string? ReferenceImageUrl = null
);

public record ImageGenerationResult(
    bool Success,
    List<string>? ImageUrls = null,
    string? ErrorMessage = null,
    decimal Cost = 0
);
