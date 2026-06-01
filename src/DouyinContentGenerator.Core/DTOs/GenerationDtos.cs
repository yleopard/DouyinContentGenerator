namespace DouyinContentGenerator.Core.DTOs;

public class CreateGenerationTaskRequest
{
    public Guid ProductId { get; set; }
    public int ImageCount { get; set; } = 3;
    public int TextVariantsCount { get; set; } = 5;
    public List<Guid> ImageTemplateIds { get; set; } = new();
    public List<Guid> TextTemplateIds { get; set; } = new();
    public bool UseReferenceImage { get; set; } = false;
    public string Style { get; set; } = "realistic";
}

public class BatchGenerationRequest
{
    public List<CreateGenerationTaskRequest> Tasks { get; set; } = new();
}

public class GenerationTaskResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int Progress { get; set; }
    public string? StatusMessage { get; set; }
    public int ImageCount { get; set; }
    public int TextVariantsCount { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GeneratedImageResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid ImageTemplateId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GeneratedTextResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid CopywritingTemplateId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SelectContentRequest
{
    public Guid? SelectedImageId { get; set; }
    public Guid? SelectedTextId { get; set; }
}

public class CostStatisticsResponse
{
    public DateTime Date { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public int CallCount { get; set; }
    public decimal TotalCost { get; set; }
}

public class BudgetStatusResponse
{
    public decimal DailyBudget { get; set; }
    public decimal UsedBudget { get; set; }
    public decimal RemainingBudget { get; set; }
    public int EstimatedRemainingTasks { get; set; }
}
