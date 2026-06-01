using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.DTOs;

public class CreateProductRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Category { get; set; }

    public string? Description { get; set; }

    public string[] SellingPoints { get; set; } = Array.Empty<string>();

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();

    public string? GenerationConfig { get; set; }
}

public class UpdateProductRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public string? Description { get; set; }

    public string[]? SellingPoints { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }

    public string[]? Tags { get; set; }

    public string? GenerationConfig { get; set; }
}

public class ProductResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
    public string[] SellingPoints { get; set; } = Array.Empty<string>();
    public decimal Price { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string? GenerationConfig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProductImageUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = "product";
    public int Order { get; set; } = 0;
}
