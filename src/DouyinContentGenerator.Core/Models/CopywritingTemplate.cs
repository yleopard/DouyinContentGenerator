using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

public class CopywritingTemplate
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string TemplateType { get; set; } = string.Empty; // pain_point, value, etc.
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public string[] Variables { get; set; } = Array.Empty<string>();
    
    public bool IsBuiltin { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
