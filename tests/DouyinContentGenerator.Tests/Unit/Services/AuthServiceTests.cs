using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using DouyinContentGenerator.API.Services;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.Tests.Unit.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var configMock = new Mock<IConfiguration>();
        var jwtSectionMock = new Mock<IConfigurationSection>();
        jwtSectionMock.Setup(s => s["Secret"]).Returns("this-is-a-test-jwt-secret-key-at-least-32-chars");
        configMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSectionMock.Object);

        _authService = new AuthService(_context, configMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenValidRequest()
    {
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };

        var result = await _authService.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenUsernameExists()
    {
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "first@example.com",
            Password = "password123"
        };

        await _authService.RegisterAsync(request);

        var duplicateRequest = new RegisterRequest
        {
            Username = "existinguser",
            Email = "another@example.com",
            Password = "password123"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _authService.RegisterAsync(duplicateRequest));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
