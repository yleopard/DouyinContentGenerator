using DouyinContentGenerator.Core.DTOs;

namespace DouyinContentGenerator.Core.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateProductAsync(Guid userId, CreateProductRequest request);
    Task<ProductResponse?> GetProductAsync(Guid userId, Guid productId);
    Task<List<ProductResponse>> GetProductsAsync(Guid userId, int page = 1, int pageSize = 20, string? category = null);
    Task<ProductResponse> UpdateProductAsync(Guid userId, Guid productId, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(Guid userId, Guid productId);
    Task<string> UploadImageAsync(Guid userId, Guid productId, Stream fileStream, string fileName, string type, int order = 0);
    Task<bool> DeleteImageAsync(Guid userId, Guid productId, Guid imageId);
}
