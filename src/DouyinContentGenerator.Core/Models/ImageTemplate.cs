using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

public class ImageTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Category { get; set; } // kitchen, living_room, etc.
    
    public string? Description { get; set; }
    
    [Required]
    public string PromptTemplate { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Style { get; set; } = "realistic";
    
    public string? ThumbnailUrl { get; set; }
    
    public bool IsBuiltin { get; set; } = false;
    
    public int UsageCount { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
