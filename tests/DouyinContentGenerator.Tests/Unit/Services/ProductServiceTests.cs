using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Infrastructure.Data;
using DouyinContentGenerator.Infrastructure.Services;

namespace DouyinContentGenerator.Tests.Unit.Services;

public class ProductServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProductService _productService;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _productService = new ProductService(_context);
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProduct_WhenValidRequest()
    {
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Category = "Test Category",
            Price = 99.99m,
            SellingPoints = new[] { "Feature 1", "Feature 2" }
        };

        var result = await _productService.CreateProductAsync(_testUserId, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        result.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task GetProductsAsync_ShouldReturnOnlyUserProducts()
    {
        var otherUserId = Guid.NewGuid();

        await _productService.CreateProductAsync(_testUserId, new CreateProductRequest
        {
            Name = "User Product",
            Price = 10m
        });

        await _productService.CreateProductAsync(otherUserId, new CreateProductRequest
        {
            Name = "Other User Product",
            Price = 20m
        });

        var results = await _productService.GetProductsAsync(_testUserId);

        results.Should().HaveCount(1);
        results[0].Name.Should().Be("User Product");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
