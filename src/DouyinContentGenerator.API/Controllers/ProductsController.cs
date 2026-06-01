using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.Parse(userIdClaim?.Value ?? throw new UnauthorizedAccessException());
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var userId = GetCurrentUserId();
        var product = await _productService.CreateProductAsync(userId, request);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductResponse>> GetProduct(Guid id)
    {
        var userId = GetCurrentUserId();
        var product = await _productService.GetProductAsync(userId, id);

        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductResponse>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? category = null)
    {
        var userId = GetCurrentUserId();
        var products = await _productService.GetProductsAsync(userId, page, pageSize, category);
        return Ok(products);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var product = await _productService.UpdateProductAsync(userId, id, request);
            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        var userId = GetCurrentUserId();
        var deleted = await _productService.DeleteProductAsync(userId, id);

        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPost("{id}/images")]
    public async Task<ActionResult> UploadImage(Guid id, IFormFile file, [FromForm] string type = "product", [FromForm] int order = 0)
    {
        try
        {
            var userId = GetCurrentUserId();
            using var stream = file.OpenReadStream();
            var imageUrl = await _productService.UploadImageAsync(userId, id, stream, file.FileName, type, order);
            return Ok(new { url = imageUrl });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{productId}/images/{imageId}")]
    public async Task<ActionResult> DeleteImage(Guid productId, Guid imageId)
    {
        var userId = GetCurrentUserId();
        var deleted = await _productService.DeleteImageAsync(userId, productId, imageId);

        if (!deleted) return NotFound();
        return NoContent();
    }
}
