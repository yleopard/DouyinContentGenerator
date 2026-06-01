using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

public class AiProviderConfig
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string ProviderType { get; set; } = string.Empty; // image_generation / text_generation

    [Required]
    [MaxLength(100)]
    public string ProviderName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = false;

    public string? ConfigData { get; set; } // JSONB - encrypted

    public string? UsageStats { get; set; } // JSONB

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
