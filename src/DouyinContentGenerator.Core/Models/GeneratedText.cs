using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DouyinContentGenerator.Core.Models;

public class GeneratedText
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskId { get; set; }

    public Guid CopywritingTemplateId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,4)")]
    public decimal Cost { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "success"; // success/failed

    public bool IsSelected { get; set; } = false;

    public string? ErrorMessage { get; set; }

    [MaxLength(200)]
    public string? IdempotencyKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public GenerationTask Task { get; set; } = null!;
    public CopywritingTemplate CopywritingTemplate { get; set; } = null!;
}
