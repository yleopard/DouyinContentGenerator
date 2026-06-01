# 抖音图文带货AI生成系统 - 后端核心实现计划

> **面向 AI 代理的工作者：** 必需子技能：使用 superpowers:subagent-driven-development（推荐）或 superpowers:executing-plans 逐任务实现此计划。步骤使用复选框（`- [ ]`）语法来跟踪进度。

**目标：** 构建.NET 8 Web API后端核心功能,包括用户认证、产品管理、数据库层和基础架构

**架构：** 采用分层架构(Controller → Service → Repository → EF Core),支持JWT认证、多用户数据隔离、PostgreSQL数据库

**技术栈：** .NET 8, Entity Framework Core, PostgreSQL, JWT, xUnit, Moq

---

## 文件结构

### 后端项目结构

```
DouyinContentGenerator/
├── src/
│   ├── DouyinContentGenerator.API/          # Web API项目
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   └── ProductsController.cs
│   │   ├── Middleware/
│   │   │   └── BudgetGuardMiddleware.cs
│   │   ├── Hubs/
│   │   │   └── GenerationHub.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── DouyinContentGenerator.API.csproj
│   │
│   ├── DouyinContentGenerator.Core/         # 核心业务层
│   │   ├── Interfaces/
│   │   │   ├── IAuthService.cs
│   │   │   ├── IProductService.cs
│   │   │   └── IBudgetReservationService.cs
│   │   ├── Models/
│   │   │   ├── User.cs
│   │   │   ├── Role.cs
│   │   │   ├── Product.cs
│   │   │   └── ProductImage.cs
│   │   ├── Services/
│   │   │   ├── AuthService.cs
│   │   │   ├── ProductService.cs
│   │   │   └── BudgetReservationService.cs
│   │   ├── DTOs/
│   │   │   ├── AuthDtos.cs
│   │   │   └── ProductDtos.cs
│   │   └── DouyinContentGenerator.Core.csproj
│   │
│   └── DouyinContentGenerator.Infrastructure/  # 基础设施层
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   ├── Migrations/
│       │   └── Repositories/
│       ├── Configuration/
│       │   └── DatabaseConfiguration.cs
│       └── DouyinContentGenerator.Infrastructure.csproj
│
├── tests/
│   └── DouyinContentGenerator.Tests/
│       ├── Unit/
│       │   ├── Services/
│       │   └── Controllers/
│       └── Integration/
│           └── ApiTests/
│
├── docker-compose.yml
└── README.md
```

---

## 任务 1：项目初始化与解决方案结构

**文件：**
- 创建：`DouyinContentGenerator/DouyinContentGenerator.sln`
- 创建：`DouyinContentGenerator/src/DouyinContentGenerator.API/DouyinContentGenerator.API.csproj`
- 创建：`DouyinContentGenerator/src/DouyinContentGenerator.Core/DouyinContentGenerator.Core.csproj`
- 创建：`DouyinContentGenerator/src/DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj`
- 创建：`DouyinContentGenerator/tests/DouyinContentGenerator.Tests/DouyinContentGenerator.Tests.csproj`

- [x] **步骤 1：创建解决方案和项目文件**

```bash
cd DouyinContentGenerator
dotnet new sln -n DouyinContentGenerator

# 创建API项目
dotnet new webapi -n DouyinContentGenerator.API -o src/DouyinContentGenerator.API
dotnet sln add src/DouyinContentGenerator.API/DouyinContentGenerator.API.csproj

# 创建Core类库
dotnet new classlib -n DouyinContentGenerator.Core -o src/DouyinContentGenerator.Core
dotnet sln add src/DouyinContentGenerator.Core/DouyinContentGenerator.Core.csproj

# 创建Infrastructure类库
dotnet new classlib -n DouyinContentGenerator.Infrastructure -o src/DouyinContentGenerator.Infrastructure
dotnet sln add src/DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj

# 创建测试项目
dotnet new xunit -n DouyinContentGenerator.Tests -o tests/DouyinContentGenerator.Tests
dotnet sln add tests/DouyinContentGenerator.Tests/DouyinContentGenerator.Tests.csproj
```

- [x] **步骤 2：添加项目引用**

```bash
# API引用Core和Infrastructure
cd src/DouyinContentGenerator.API
dotnet add reference ../DouyinContentGenerator.Core/DouyinContentGenerator.Core.csproj
dotnet add reference ../DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj

# Infrastructure引用Core
cd ../DouyinContentGenerator.Infrastructure
dotnet add reference ../DouyinContentGenerator.Core/DouyinContentGenerator.Core.csproj

# 测试项目引用API和Core
cd ../../tests/DouyinContentGenerator.Tests
dotnet add reference ../../src/DouyinContentGenerator.API/DouyinContentGenerator.API.csproj
dotnet add reference ../../src/DouyinContentGenerator.Core/DouyinContentGenerator.Core.csproj
```

- [x] **步骤 3：安装NuGet包**

```bash
# API项目
cd ../../src/DouyinContentGenerator.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Serilog.AspNetCore
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.PostgreSql
dotnet add package StackExchange.Redis

# Infrastructure项目
cd ../DouyinContentGenerator.Infrastructure
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package BCrypt.Net-Next

# 测试项目
cd ../../tests/DouyinContentGenerator.Tests
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.AspNetCore.Mvc.Testing
```

- [x] **步骤 4：验证项目结构**

```bash
cd ../..
dotnet build
```

预期：BUILD SUCCEEDED

- [x] **步骤 5：Commit**

```bash
git add .
git commit -m "init: create solution structure and projects"
```

---

## 任务 2：数据库模型定义

**文件：**
- 创建：`src/DouyinContentGenerator.Core/Models/User.cs`
- 创建：`src/DouyinContentGenerator.Core/Models/Role.cs`
- 创建：`src/DouyinContentGenerator.Core/Models/Product.cs`
- 创建：`src/DouyinContentGenerator.Core/Models/ProductImage.cs`
- 创建：`src/DouyinContentGenerator.Core/Models/ImageTemplate.cs`
- 创建：`src/DouyinContentGenerator.Core/Models/CopywritingTemplate.cs`

- [x] **步骤 1：创建User模型**

```csharp
// src/DouyinContentGenerator.Core/Models/User.cs
using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

- [x] **步骤 2：创建Role模型**

```csharp
// src/DouyinContentGenerator.Core/Models/Role.cs
using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.Models;

public class Role
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty; // Admin, Operator
    
    // Navigation property
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```

- [x] **步骤 3：创建UserRole关联模型**

```csharp
// src/DouyinContentGenerator.Core/Models/UserRole.cs
namespace DouyinContentGenerator.Core.Models;

public class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    
    // Navigation properties
    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
```

- [x] **步骤 4：创建Product模型**

```csharp
// src/DouyinContentGenerator.Core/Models/Product.cs
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
```

- [x] **步骤 5：创建ProductImage模型**

```csharp
// src/DouyinContentGenerator.Core/Models/ProductImage.cs
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
```

- [x] **步骤 6：创建ImageTemplate模型**

```csharp
// src/DouyinContentGenerator.Core/Models/ImageTemplate.cs
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
```

- [x] **步骤 7：创建CopywritingTemplate模型**

```csharp
// src/DouyinContentGenerator.Core/Models/CopywritingTemplate.cs
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
```

- [x] **步骤 8：编译验证**

```bash
dotnet build src/DouyinContentGenerator.Core/DouyinContentGenerator.Core.csproj
```

预期：BUILD SUCCEEDED

- [x] **步骤 9：Commit**

```bash
git add src/DouyinContentGenerator.Core/Models/
git commit -m "feat: define database models (User, Role, Product, Templates)"
```

---

## 任务 3：DbContext配置与迁移

**文件：**
- 创建：`src/DouyinContentGenerator.Infrastructure/Data/ApplicationDbContext.cs`
- 修改：`src/DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj`

- [x] **步骤 1：安装EF Core工具**

```bash
cd src/DouyinContentGenerator.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.Design
```

- [x] **步骤 2：创建ApplicationDbContext**

```csharp
// src/DouyinContentGenerator.Infrastructure/Data/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.Models;

namespace DouyinContentGenerator.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    // DbSets
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ImageTemplate> ImageTemplates => Set<ImageTemplate>();
    public DbSet<CopywritingTemplate> CopywritingTemplates => Set<CopywritingTemplate>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure composite key for UserRole
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
        
        // Configure relationships
        modelBuilder.Entity<User>()
            .HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId);
        
        modelBuilder.Entity<Role>()
            .HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId);
        
        modelBuilder.Entity<Product>()
            .HasMany(p => p.ProductImages)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId);
        
        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Admin" },
            new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Operator" }
        );
    }
}
```

- [ ] **步骤 3：添加连接字符串配置**

```json
// src/DouyinContentGenerator.API/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=douyin_content;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **步骤 4：在Program.cs中注册DbContext**

```csharp
// src/DouyinContentGenerator.API/Program.cs
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

- [ ] **步骤 5：创建第一个迁移**

```bash
cd src/DouyinContentGenerator.API
dotnet ef migrations add InitialCreate -s ../DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj -p ../DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj
```

预期：生成迁移文件 `src/DouyinContentGenerator.Infrastructure/Data/Migrations/<timestamp>_InitialCreate.cs`

- [ ] **步骤 6：应用迁移到数据库**

```bash
# 首先启动PostgreSQL (假设使用docker)
docker run --name postgres-test -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=douyin_content -p 5432:5432 -d postgres:16

# 等待几秒后应用迁移
dotnet ef database update -s ../DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj -p ../DouyinContentGenerator.Infrastructure/DouyinContentGenerator.Infrastructure.csproj
```

预期：Done

- [ ] **步骤 7：验证数据库表已创建**

```bash
docker exec -it postgres-test psql -U postgres -d douyin_content -c "\dt"
```

预期：显示Users, Roles, UserRoles, Products, ProductImages, ImageTemplates, CopywritingTemplates表

- [ ] **步骤 8：Commit**

```bash
git add src/DouyinContentGenerator.Infrastructure/Data/
git add src/DouyinContentGenerator.API/appsettings.json
git add src/DouyinContentGenerator.API/Program.cs
git commit -m "feat: configure DbContext and create initial migration"
```

---

## 任务 4：JWT认证服务

**文件：**
- 创建：`src/DouyinContentGenerator.Core/Interfaces/IAuthService.cs`
- 创建：`src/DouyinContentGenerator.Core/Services/AuthService.cs`
- 创建：`src/DouyinContentGenerator.Core/DTOs/AuthDtos.cs`
- 创建：`src/DouyinContentGenerator.API/Controllers/AuthController.cs`

- [ ] **步骤 1：创建Auth DTOs**

```csharp
// src/DouyinContentGenerator.Core/DTOs/AuthDtos.cs
using System.ComponentModel.DataAnnotations;

namespace DouyinContentGenerator.Core.DTOs;

public class RegisterRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}

public class UserInfo
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
```

- [ ] **步骤 2：创建IAuthService接口**

```csharp
// src/DouyinContentGenerator.Core/Interfaces/IAuthService.cs
using DouyinContentGenerator.Core.DTOs;

namespace DouyinContentGenerator.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserInfo?> GetUserInfoAsync(Guid userId);
}
```

- [ ] **步骤 3：实现AuthService**

```csharp
// src/DouyinContentGenerator.Core/Services/AuthService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Core.Models;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.Core.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    
    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }
    
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if username or email already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
        {
            throw new InvalidOperationException("Username already exists");
        }
        
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }
        
        // Create user with hashed password
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };
        
        _context.Users.Add(user);
        
        // Assign default Operator role
        var operatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Operator");
        if (operatorRole != null)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = operatorRole.Id
            });
        }
        
        await _context.SaveChangesAsync();
        
        // Generate token
        var token = GenerateJwtToken(user);
        var roles = await GetUserRolesAsync(user.Id);
        
        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token,
            Roles = roles
        };
    }
    
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password");
        }
        
        if (!user.IsActive)
        {
            throw new InvalidOperationException("Account is deactivated");
        }
        
        var token = GenerateJwtToken(user);
        var roles = await GetUserRolesAsync(user.Id);
        
        return new AuthResponse
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Token = token,
            Roles = roles
        };
    }
    
    public async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return null;
        }
        
        var roles = await GetUserRolesAsync(userId);
        
        return new UserInfo
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = roles
        };
    }
    
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? throw new Exception("JWT Secret not configured"));
        
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }
    
    private async Task<string[]> GetUserRolesAsync(Guid userId)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(_context.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name)
            .ToListAsync();
        
        return roles.ToArray();
    }
}
```

- [ ] **步骤 4：创建AuthController**

```csharp
// src/DouyinContentGenerator.API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;

namespace DouyinContentGenerator.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
    
    [HttpGet("me")]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Unauthorized();
        }
        
        var userId = Guid.Parse(userIdClaim.Value);
        var userInfo = await _authService.GetUserInfoAsync(userId);
        
        if (userInfo == null)
        {
            return NotFound();
        }
        
        return Ok(userInfo);
    }
}
```

- [ ] **步骤 5：配置JWT认证**

```csharp
// src/DouyinContentGenerator.API/Program.cs - 在builder.Services.AddControllers()之后添加
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? throw new Exception("JWT Secret not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
```

- [ ] **步骤 6：添加JWT配置到appsettings.json**

```json
// src/DouyinContentGenerator.API/appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=douyin_content;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "Secret": "your-super-secret-jwt-key-at-least-32-characters-long"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

- [ ] **步骤 7：注册AuthService**

```csharp
// src/DouyinContentGenerator.API/Program.cs - 在AddDbContext之后添加
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Core.Services;

builder.Services.AddScoped<IAuthService, AuthService>();
```

- [ ] **步骤 8：编写单元测试**

```csharp
// tests/DouyinContentGenerator.Tests/Unit/Services/AuthServiceTests.cs
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.Services;
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
        configMock.Setup(c => c.GetSection("JwtSettings:Secret")).Returns(It.IsAny<IConfigurationSection>());
        
        _authService = new AuthService(_context, configMock.Object);
    }
    
    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenValidRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123"
        };
        
        // Act
        var result = await _authService.RegisterAsync(request);
        
        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
        result.Token.Should().NotBeNullOrEmpty();
    }
    
    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenUsernameExists()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "password123"
        };
        
        await _authService.RegisterAsync(request);
        
        // Act & Assert
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
```

- [ ] **步骤 9：运行测试**

```bash
cd tests/DouyinContentGenerator.Tests
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

预期：2 passed

- [ ] **步骤 10：Commit**

```bash
git add src/DouyinContentGenerator.Core/Interfaces/IAuthService.cs
git add src/DouyinContentGenerator.Core/Services/AuthService.cs
git add src/DouyinContentGenerator.Core/DTOs/AuthDtos.cs
git add src/DouyinContentGenerator.API/Controllers/AuthController.cs
git add tests/DouyinContentGenerator.Tests/Unit/Services/AuthServiceTests.cs
git commit -m "feat: implement JWT authentication service"
```

---

## 任务 5：产品管理服务

**文件：**
- 创建：`src/DouyinContentGenerator.Core/Interfaces/IProductService.cs`
- 创建：`src/DouyinContentGenerator.Core/Services/ProductService.cs`
- 创建：`src/DouyinContentGenerator.Core/DTOs/ProductDtos.cs`
- 创建：`src/DouyinContentGenerator.API/Controllers/ProductsController.cs`

- [ ] **步骤 1：创建Product DTOs**

```csharp
// src/DouyinContentGenerator.Core/DTOs/ProductDtos.cs
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

public class UploadImageRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;
    
    [Required]
    [RegularExpression("^(product|reference)$")]
    public string Type { get; set; } = "product";
    
    public int Order { get; set; } = 0;
}
```

- [ ] **步骤 2：创建IProductService接口**

```csharp
// src/DouyinContentGenerator.Core/Interfaces/IProductService.cs
using DouyinContentGenerator.Core.DTOs;

namespace DouyinContentGenerator.Core.Interfaces;

public interface IProductService
{
    Task<ProductResponse> CreateProductAsync(Guid userId, CreateProductRequest request);
    Task<ProductResponse?> GetProductAsync(Guid userId, Guid productId);
    Task<List<ProductResponse>> GetProductsAsync(Guid userId, int page = 1, int pageSize = 20, string? category = null);
    Task<ProductResponse> UpdateProductAsync(Guid userId, Guid productId, UpdateProductRequest request);
    Task<bool> DeleteProductAsync(Guid userId, Guid productId);
    Task<string> UploadImageAsync(Guid userId, Guid productId, UploadImageRequest request);
    Task<bool> DeleteImageAsync(Guid userId, Guid productId, Guid imageId);
}
```

- [ ] **步骤 3：实现ProductService**

```csharp
// src/DouyinContentGenerator.Core/Services/ProductService.cs
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Core.Models;
using DouyinContentGenerator.Infrastructure.Data;

namespace DouyinContentGenerator.Core.Services;

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
        {
            query = query.Where(p => p.Category == category);
        }
        
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
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);
        
        if (product == null)
        {
            throw new InvalidOperationException("Product not found");
        }
        
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
        
        if (product == null)
        {
            return false;
        }
        
        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<string> UploadImageAsync(Guid userId, Guid productId, UploadImageRequest request)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);
        
        if (product == null)
        {
            throw new InvalidOperationException("Product not found");
        }
        
        // TODO: Upload to Supabase Storage and get URL
        // For now, just return a placeholder
        var imageUrl = $"https://placeholder.com/{request.File.FileName}";
        
        var productImage = new ProductImage
        {
            ProductId = productId,
            Url = imageUrl,
            Type = request.Type,
            Order = request.Order
        };
        
        _context.ProductImages.Add(productImage);
        await _context.SaveChangesAsync();
        
        return imageUrl;
    }
    
    public async Task<bool> DeleteImageAsync(Guid userId, Guid productId, Guid imageId)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(pi => pi.Id == imageId && pi.ProductId == productId);
        
        if (image == null)
        {
            return false;
        }
        
        // Verify product belongs to user
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId && p.UserId == userId);
        
        if (product == null)
        {
            return false;
        }
        
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
```

- [ ] **步骤 4：创建ProductsController**

```csharp
// src/DouyinContentGenerator.API/Controllers/ProductsController.cs
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
        
        if (product == null)
        {
            return NotFound();
        }
        
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
        
        if (!deleted)
        {
            return NotFound();
        }
        
        return NoContent();
    }
    
    [HttpPost("{id}/images")]
    public async Task<ActionResult<string>> UploadImage(Guid id, [FromForm] UploadImageRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var imageUrl = await _productService.UploadImageAsync(userId, id, request);
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
        
        if (!deleted)
        {
            return NotFound();
        }
        
        return NoContent();
    }
}
```

- [ ] **步骤 5：注册ProductService**

```csharp
// src/DouyinContentGenerator.API/Program.cs
using DouyinContentGenerator.Core.Interfaces;
using DouyinContentGenerator.Core.Services;

builder.Services.AddScoped<IProductService, ProductService>();
```

- [ ] **步骤 6：编写单元测试**

```csharp
// tests/DouyinContentGenerator.Tests/Unit/Services/ProductServiceTests.cs
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using DouyinContentGenerator.Core.Services;
using DouyinContentGenerator.Core.DTOs;
using DouyinContentGenerator.Infrastructure.Data;

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
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Category = "Test Category",
            Price = 99.99m,
            SellingPoints = new[] { "Feature 1", "Feature 2" }
        };
        
        // Act
        var result = await _productService.CreateProductAsync(_testUserId, request);
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Product");
        result.Price.Should().Be(99.99m);
        result.UserId.Should().Be(_testUserId);
    }
    
    [Fact]
    public async Task GetProductsAsync_ShouldReturnOnlyUserProducts()
    {
        // Arrange
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
        
        // Act
        var results = await _productService.GetProductsAsync(_testUserId);
        
        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("User Product");
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}
```

- [ ] **步骤 7：运行测试**

```bash
cd tests/DouyinContentGenerator.Tests
dotnet test --filter "FullyQualifiedName~ProductServiceTests"
```

预期：2 passed

- [ ] **步骤 8：Commit**

```bash
git add src/DouyinContentGenerator.Core/Interfaces/IProductService.cs
git add src/DouyinContentGenerator.Core/Services/ProductService.cs
git add src/DouyinContentGenerator.Core/DTOs/ProductDtos.cs
git add src/DouyinContentGenerator.API/Controllers/ProductsController.cs
git add tests/DouyinContentGenerator.Tests/Unit/Services/ProductServiceTests.cs
git commit -m "feat: implement product management service with CRUD operations"
```

---

## 后续任务预告

已完成后端核心基础架构,接下来的计划包括:

- **计划2:** AI服务插件化架构 (IImageGenerator, ITextGenerator, 通义万相/通义千问实现)
- **计划3:** 后台任务系统 (Hangfire主/子Job, SignalR实时推送)
- **计划4:** 预算控制与成本管理 (Redis预算预留,成本监控)
- **计划5:** 前端React应用 (产品管理,生成配置,内容预览)

---

**计划已完成并保存到 `docs/superpowers/plans/2026-05-29-douyin-content-generator-backend-core.md`。两种执行方式：**

**1. 子代理驱动（推荐）** - 每个任务调度一个新的子代理，任务间进行审查，快速迭代

**2. 内联执行** - 在当前会话中使用 executing-plans 执行任务，批量执行并设有检查点供审查

**选哪种方式？**
