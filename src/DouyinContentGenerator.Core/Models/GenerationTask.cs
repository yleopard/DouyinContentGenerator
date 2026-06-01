using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DouyinContentGenerator.Core.Models;

public class GenerationTask
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public Guid ProductId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // pending/processing/completed/failed/cancelled

    public int Progress { get; set; } = 0;

    public string? StatusMessage { get; set; }

    public int ImageCount { get; set; } = 1;

    public int TextVariantsCount { get; set; } = 3;

    public bool UseReferenceImage { get; set; } = false;

    public string? ErrorMessage { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal EstimatedCost { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal ActualCost { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
    public ICollection<TaskImageTemplate> TaskImageTemplates { get; set; } = new List<TaskImageTemplate>();
    public ICollection<GeneratedImage> GeneratedImages { get; set; } = new List<GeneratedImage>();
    public ICollection<GeneratedText> GeneratedTexts { get; set; } = new List<GeneratedText>();
}
