using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

public class ProductImage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid ProductId { get; set; }
    
    [Required]
    public string Url { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "product"; // product or reference
    
    public int Order { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Product Product { get; set; } = null!;
}
