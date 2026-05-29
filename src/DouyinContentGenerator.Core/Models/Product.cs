using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DouyinContentGenerator.Core.Models;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? Category { get; set; }
    
    public string? Description { get; set; }
    
    public string[] SellingPoints { get; set; } = Array.Empty<string>();
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    public string[] Tags { get; set; } = Array.Empty<string>();
    
    public string? GenerationConfig { get; set; } // JSONB
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
