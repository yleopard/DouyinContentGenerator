using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

/// <summary>
/// Per-user AI provider settings. Each user has exactly one record.
/// All provider configs stored as JSON in ConfigJson.
/// </summary>
public class UserAISettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// JSON object: { "imageProvider", "imageApiKey", "imageModel", "textProvider", "textApiKey", "textModel", "dailyBudget", "alertThreshold" }
    /// </summary>
    [Required]
    public string ConfigJson { get; set; } = "{}";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
