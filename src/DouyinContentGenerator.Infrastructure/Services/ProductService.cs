using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Core.Models;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductResponse> CreateProductAsync(Guid userId, CreateProductRequest request)
    {
        var product = new Product
        {
            UserId = userId,
            Name = request.Name,
            Category = request.Category,
            Description = request.Description,
            SellingPoints = request.SellingPoints,
            Price = request.Price,
            Tags = request.Tags,
            GenerationConfig = request.GenerationConfig
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<ProductResponse?> GetProductAsync(Guid userId, Guid productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);

        return product != null ? MapToResponse(product) : null;
    }

    public async Task<List<ProductResponse>> GetProductsAsync(Guid userId, int page = 1, int pageSize = 20, string? category = null)
    {
        var query = _context.Products.Where(p => p.UserId == userId);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(p => p.Category == category);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return products.Select(MapToResponse).ToList();
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid userId, Guid productId, UpdateProductRequest request)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId)
            ?? throw new InvalidOperationException("Product not found");

        if (request.Name != null) product.Name = request.Name;
        if (request.Category != null) product.Category = request.Category;
        if (request.Description != null) product.Description = request.Description;
        if (request.SellingPoints != null) product.SellingPoints = request.SellingPoints;
        if (request.Price.HasValue) product.Price = request.Price.Value;
        if (request.Tags != null) product.Tags = request.Tags;
        if (request.GenerationConfig != null) product.GenerationConfig = request.GenerationConfig;

        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<bool> DeleteProductAsync(Guid userId, Guid productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);

        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<string> UploadImageAsync(Guid userId, Guid productId, Stream fileStream, string fileName, string type, int order = 0)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId)
            ?? throw new InvalidOperationException("Product not found");

        var imageUrl = $"https://placeholder.local/{fileName}";

        var productImage = new ProductImage
        {
            ProductId = productId,
            Url = imageUrl,
            Type = type,
            Order = order
        };

        _context.ProductImages.Add(productImage);
        await _context.SaveChangesAsync();

        return imageUrl;
    }

    public async Task<bool> DeleteImageAsync(Guid userId, Guid productId, Guid imageId)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(pi => pi.Id == imageId && pi.ProductId == productId);

        if (image == null) return false;

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);

        if (product == null) return false;

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();

        return true;
    }

    private static ProductResponse MapToResponse(Product product)
    {
        return new ProductResponse
        {
            Id = product.Id,
            UserId = product.UserId,
            Name = product.Name,
            Category = product.Category,
            Description = product.Description,
            SellingPoints = product.SellingPoints,
            Price = product.Price,
            Tags = product.Tags,
            GenerationConfig = product.GenerationConfig,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };
    }
}
